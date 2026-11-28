const { test, before, after } = require('node:test');
const assert = require('node:assert/strict');
const { spawn } = require('node:child_process');
const { mkdtemp, rm } = require('node:fs/promises');
const { tmpdir } = require('node:os');
const path = require('node:path');
const net = require('node:net');
const { chromium } = require('playwright');

let browser, server, serverExit, directory, baseUrl;
const root = path.resolve(__dirname, '../..');
let sequence = 0;

before(async () => {
    directory = await mkdtemp(path.join(tmpdir(), 'switchyard-editor-'));
    const listener = net.createServer();
    await new Promise(resolve => listener.listen(0, '127.0.0.1', resolve));
    const port = listener.address().port;
    await new Promise(resolve => listener.close(resolve));
    baseUrl = `http://127.0.0.1:${port}`;
    server = spawn('dotnet', [path.join(root, 'src/Switchyard.Api/bin/Release/net10.0/Switchyard.Api.dll')], {
        cwd: path.join(root, 'src/Switchyard.Api'),
        env: { ...process.env, ASPNETCORE_URLS: baseUrl, ASPNETCORE_ENVIRONMENT: 'Production', Database__Path: path.join(directory, 'test.db') },
        stdio: ['ignore', 'pipe', 'pipe']
    });
    let output = '';
    server.stdout.on('data', data => { output += data; });
    server.stderr.on('data', data => { output += data; });
    serverExit = new Promise(resolve => server.on('exit', resolve));
    for (let attempt = 0; attempt < 100; attempt++) {
        try { if ((await fetch(`${baseUrl}/api/warehouses`)).ok) break; } catch { /* Wait for this test server. */ }
        if (attempt === 99 || server.exitCode !== null) throw new Error(`API did not start. Build Release first.\n${output}`);
        await new Promise(resolve => setTimeout(resolve, 100));
    }
    browser = await chromium.launch();
});

after(async () => {
    await browser?.close();
    if (server && server.exitCode === null) { server.kill('SIGTERM'); await serverExit; }
    if (directory) await rm(directory, { recursive: true, force: true });
});

async function post(route, data) {
    const response = await fetch(baseUrl + route, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(data) });
    assert.equal(response.status, 201, await response.clone().text());
    return response.json();
}

async function fixture(t, { mobile = false, rows = 10, columns = 12 } = {}) {
    const warehouse = await post('/api/warehouses', { name: `Editor ${++sequence}`, rows, columns });
    const context = await browser.newContext(mobile
        ? { viewport: { width: 390, height: 844 }, isMobile: true, hasTouch: true }
        : { viewport: { width: 1440, height: 1100 } });
    t.after(() => context.close());
    const page = await context.newPage();
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    t.after(() => assert.deepEqual(errors, []));
    await page.goto(`${baseUrl}/warehouse.html?warehouse=${warehouse.id}`);
    await page.waitForFunction(() => !document.querySelector('#draw-mode').disabled);
    return { page, context, warehouse, zones: async () => (await fetch(`${baseUrl}/api/warehouses/${warehouse.id}/zones`)).json() };
}

async function coordinates(page, warehouse, row, column) {
    const bounds = await page.locator('#warehouse-map').boundingBox();
    return { x: bounds.x + (column - .5) / warehouse.columns * bounds.width, y: bounds.y + (row - .5) / warehouse.rows * bounds.height };
}

async function draw(page, warehouse, from, to) {
    await page.locator('#map-scroll').scrollIntoViewIfNeeded();
    const start = await coordinates(page, warehouse, ...from);
    const end = await coordinates(page, warehouse, ...to);
    await page.mouse.move(start.x, start.y);
    await page.mouse.down();
    await page.mouse.move(end.x, end.y, { steps: 8 });
    await page.mouse.up();
}

async function save(page, name, kind = 'shelf') {
    await page.locator('#name').fill(name);
    await page.locator('#kind').selectOption(kind);
    await page.getByRole('button', { name: 'Guardar zona', exact: true }).click();
    await page.locator('#form-message[data-state="success"]').waitFor();
    await page.locator('.zone-entry').filter({ hasText: name }).waitFor();
}

test('draws in either direction, saves only on confirmation and keeps metre measurements after reload', async t => {
    const { page, warehouse, zones } = await fixture(t);
    await draw(page, warehouse, [2, 2], [4, 5]);
    assert.equal(await page.locator('#columns').inputValue(), '4');
    assert.equal(await page.locator('#rows').inputValue(), '3');
    assert.equal((await zones()).length, 0);
    await save(page, 'Estantería A');
    await draw(page, warehouse, [8, 10], [6, 8]);
    await save(page, 'Bahía sur', 'dock');
    const records = await zones();
    assert.deepEqual(records.map(({ startRow, startColumn, rows, columns }) => ({ startRow, startColumn, rows, columns })), [
        { startRow: 2, startColumn: 2, rows: 3, columns: 4 }, { startRow: 6, startColumn: 8, rows: 3, columns: 3 }
    ]);
    await page.reload();
    await page.locator('.map-zone.dock').click();
    assert.match(await page.locator('#detail-size').textContent(), /3 m de ancho × 3 m de largo/);
    assert.equal(await page.locator('#detail-area').textContent(), '9 m²');
});

test('rejects overlapping drafts, handles manual input and cancels without saving', async t => {
    const { page, warehouse, zones } = await fixture(t);
    await draw(page, warehouse, [2, 2], [3, 3]);
    await save(page, 'Ocupada');
    await draw(page, warehouse, [1, 1], [3, 3]);
    assert.equal(await page.locator('.map-preview').getAttribute('data-invalid'), 'true');
    await page.locator('#name').fill('No guardar');
    await page.getByRole('button', { name: 'Guardar zona', exact: true }).click();
    await page.locator('#form-message[data-state="error"]').waitFor();
    assert.equal((await zones()).length, 1);
    await page.getByRole('button', { name: 'Cancelar borrador' }).click();
    assert.equal(await page.locator('.map-preview').isVisible(), false);
    assert.equal(await page.locator('#rows').inputValue(), '');
    for (const [id, value] of Object.entries({ startRow: '5', startColumn: '6', rows: '2', columns: '3' })) await page.locator(`#${id}`).fill(value);
    await save(page, 'Carga manual', 'charging');
    assert.equal((await zones())[1].kind, 'charging');
    await page.locator('#rows').fill('50');
    assert.match(await page.locator('#drawing-message').textContent(), /sale del almacén/);
    assert.equal(await page.locator('.map-preview').isVisible(), false);
});

test('selects with the keyboard and preserves focus, selection and a draft across polling', async t => {
    const { page, warehouse } = await fixture(t);
    await draw(page, warehouse, [2, 2], [3, 3]);
    await save(page, 'Seleccionada');
    await draw(page, warehouse, [6, 7], [7, 9]);
    const zone = page.locator('.map-zone');
    await zone.focus();
    await page.keyboard.press('Enter');
    await page.waitForResponse(response => response.url().endsWith(`/api/warehouses/${warehouse.id}/zones`) && response.request().method() === 'GET');
    assert.equal(await zone.evaluate(element => document.activeElement === element), true);
    assert.equal(await zone.getAttribute('aria-pressed'), 'true');
    assert.equal(await page.locator('#columns').inputValue(), '3');
    assert.equal(await page.locator('.map-preview').isVisible(), true);
    assert.equal(await page.locator('#detail-name').textContent(), 'Seleccionada');
    await page.keyboard.press('Escape');
    assert.equal(await page.locator('.map-preview').isVisible(), false);
});

test('restores a previous draft after pointer cancellation and preserves it when the network fails', async t => {
    const { page, warehouse, zones } = await fixture(t);
    await draw(page, warehouse, [2, 2], [3, 3]);
    const start = await coordinates(page, warehouse, 6, 6);
    await page.mouse.move(start.x, start.y); await page.mouse.down();
    await page.keyboard.press('Escape'); await page.mouse.up();
    assert.equal(await page.locator('#startRow').inputValue(), '2');
    await page.route('**/api/warehouses/*/zones', route => route.abort());
    await page.locator('#refresh-button').click();
    await page.locator('#list-error:not([hidden])').waitFor();
    assert.equal(await page.locator('.map-preview').isVisible(), true);
    await page.locator('#name').fill('Sin respuesta');
    await page.getByRole('button', { name: 'Guardar zona', exact: true }).click();
    await page.locator('#form-message[data-state="error"]').waitFor();
    assert.equal(await page.locator('#name').inputValue(), 'Sin respuesta');
    assert.equal((await zones()).length, 0);
});

test('draws with touch, inspects a zone and keeps an exploration mode on mobile', async t => {
    const { page, context, warehouse, zones } = await fixture(t, { mobile: true });
    assert.equal(await page.locator('#explore-mode').getAttribute('aria-pressed'), 'true');
    await page.locator('#draw-mode').tap();
    await page.locator('#map-scroll').scrollIntoViewIfNeeded();
    const start = await coordinates(page, warehouse, 1, 1);
    const end = await coordinates(page, warehouse, 3, 3);
    const client = await context.newCDPSession(page);
    await client.send('Input.dispatchTouchEvent', { type: 'touchStart', touchPoints: [start] });
    await client.send('Input.dispatchTouchEvent', { type: 'touchMove', touchPoints: [end] });
    await client.send('Input.dispatchTouchEvent', { type: 'touchEnd', touchPoints: [] });
    assert.equal(await page.locator('#rows').inputValue(), '3');
    await save(page, 'Zona táctil', 'dock');
    assert.equal((await zones())[0].columns, 3);
    await page.locator('.zone-entry').tap();
    assert.equal(await page.locator('#detail-kind').textContent(), 'Bahía');
    await page.locator('#explore-mode').tap();
    assert.equal(await page.locator('#warehouse-map').evaluate(element => getComputedStyle(element).touchAction), 'auto');
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
    await page.addScriptTag({ path: require.resolve('axe-core/axe.min.js') });
    const violations = await page.evaluate(async () => (await axe.run(document, { runOnly: { type: 'tag', values: ['wcag2a', 'wcag2aa', 'wcag21aa'] } })).violations.map(v => ({ id: v.id, targets: v.nodes.map(n => n.target) })));
    assert.deepEqual(violations, []);
});

test('maps coordinates correctly after scrolling a 50 by 50 metre warehouse', async t => {
    const { page, warehouse, zones } = await fixture(t, { rows: 50, columns: 50 });
    await page.locator('#map-scroll').evaluate(element => { element.scrollTop = element.scrollHeight; element.scrollLeft = element.scrollWidth; });
    await draw(page, warehouse, [48, 48], [50, 50]);
    await save(page, 'Esquina');
    const zone = (await zones())[0];
    assert.equal(zone.startRow, 48); assert.equal(zone.startColumn, 48);
    assert.equal(zone.rows, 3); assert.equal(zone.columns, 3);
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
});

test('keeps a rejected draft when another client saves an overlapping zone first', async t => {
    const { page, warehouse, zones } = await fixture(t);
    await draw(page, warehouse, [2, 2], [4, 4]);
    await post(`/api/warehouses/${warehouse.id}/zones`, { name: 'Otro cliente', kind: 'shelf', startRow: 3, startColumn: 3, rows: 1, columns: 1 });
    await page.locator('#name').fill('Mi borrador');
    await page.getByRole('button', { name: 'Guardar zona', exact: true }).click();
    await page.locator('#form-message[data-state="error"]').waitFor();
    assert.equal(await page.locator('#name').inputValue(), 'Mi borrador');
    assert.equal(await page.locator('#columns').inputValue(), '3');
    assert.equal((await zones()).length, 1);
    await page.locator('#refresh-button').click();
    await page.locator('.zone-entry').filter({ hasText: 'Otro cliente' }).waitFor();
    assert.equal(await page.locator('.map-preview').getAttribute('data-invalid'), 'true');
});

test('follows the system, preserves the editor when toggled and remembers the choice across pages and tabs', async t => {
    const { page, context, warehouse } = await fixture(t);
    await page.emulateMedia({ colorScheme: 'dark' });
    await page.waitForFunction(() => document.documentElement.dataset.theme === 'dark');
    await draw(page, warehouse, [2, 2], [3, 3]);
    await save(page, 'Tema');
    await draw(page, warehouse, [6, 6], [7, 8]);
    await page.getByRole('button', { name: 'Activar modo claro', exact: true }).click();
    assert.equal(await page.locator('#columns').inputValue(), '3');
    assert.equal(await page.locator('#detail-name').textContent(), 'Tema');
    assert.equal(await page.locator('.map-preview').isVisible(), true);
    assert.equal(await page.evaluate(() => localStorage.getItem('switchyard.theme')), 'light');
    await page.reload();
    assert.equal(await page.locator('html').getAttribute('data-theme'), 'light');
    for (const route of ['/', `/robots.html?warehouse=${warehouse.id}`, `/tasks.html?warehouse=${warehouse.id}`]) {
        await page.goto(baseUrl + route);
        assert.equal(await page.locator('html').getAttribute('data-theme'), 'light');
    }
    const second = await context.newPage();
    await second.goto(baseUrl);
    await second.getByRole('button', { name: 'Activar modo oscuro', exact: true }).click();
    await page.waitForFunction(() => document.documentElement.dataset.theme === 'dark');
    await page.reload();
    assert.equal(await page.locator('html').getAttribute('data-theme'), 'dark');
});

test('keeps all four pages coherent and accessible in both themes, including task states and mobile widths', async t => {
    const { page, warehouse } = await fixture(t);
    await draw(page, warehouse, [2, 2], [3, 3]);
    await save(page, 'Estantería');
    const robotId = `THEME-${warehouse.id}`;
    await post('/api/robots', { id: robotId, warehouseId: warehouse.id });
    const task = await post('/api/tasks', { pickup: 'A', destination: 'B', warehouseId: warehouse.id });
    for (const route of [`/api/robots/${robotId}/tasks/claim`, `/api/robots/${robotId}/tasks/${task.id}/completion`]) {
        const response = await fetch(baseUrl + route, { method: route.endsWith('claim') ? 'POST' : 'PUT' });
        assert.equal(response.status, 200);
    }
    await post('/api/tasks', { pickup: 'C', destination: 'D', warehouseId: warehouse.id });
    await fetch(`${baseUrl}/api/robots/${robotId}/tasks/claim`, { method: 'POST' });
    await post('/api/tasks', { pickup: 'E', destination: 'F', warehouseId: warehouse.id });
    for (const theme of ['dark', 'light']) {
        await page.evaluate(value => localStorage.setItem('switchyard.theme', value), theme);
        for (const route of ['/', `/warehouse.html?warehouse=${warehouse.id}`, `/robots.html?warehouse=${warehouse.id}`, `/tasks.html?warehouse=${warehouse.id}`]) {
            await page.goto(baseUrl + route);
            await page.waitForFunction(() => document.querySelector('#loading-state').hidden);
            if (route.includes('warehouse.html')) await page.locator('.map-zone').click();
            assert.equal(await page.locator('html').getAttribute('data-theme'), theme);
            const surfaces = await page.evaluate(() => [...document.querySelectorAll('.topbar, .register-panel, .registry-panel, .plan-panel, .zone-form-panel, .zone-details')].map(element => getComputedStyle(element).backgroundColor));
            assert.equal(new Set(surfaces).size, 1, `${theme} ${route}: inconsistent surfaces`);
            await page.addScriptTag({ path: require.resolve('axe-core/axe.min.js') });
            const violations = await page.evaluate(async () => (await axe.run(document, { runOnly: { type: 'tag', values: ['wcag2a', 'wcag2aa', 'wcag21aa'] } })).violations.map(v => ({ id: v.id, targets: v.nodes.map(n => n.target) })));
            assert.deepEqual(violations, [], `${theme} ${route}`);
            await page.setViewportSize({ width: 390, height: 844 });
            assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), false);
            assert.equal(await page.locator('#theme-toggle').isVisible(), true);
            await page.setViewportSize({ width: 1440, height: 1100 });
        }
    }
});

test('the theme selector still works when local storage is blocked', async t => {
    const { page } = await fixture(t);
    await page.addInitScript(() => Object.defineProperty(window, 'localStorage', { get() { throw new DOMException('Blocked', 'SecurityError'); } }));
    await page.emulateMedia({ colorScheme: 'dark' });
    await page.reload();
    assert.equal(await page.locator('html').getAttribute('data-theme'), 'dark');
    await page.getByRole('button', { name: 'Activar modo claro', exact: true }).click();
    assert.equal(await page.locator('html').getAttribute('data-theme'), 'light');
});
