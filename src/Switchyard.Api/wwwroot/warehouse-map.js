import { cellAtPoint, rectangleBetween, placementError } from "./warehouse-geometry.js";

export const zoneTypes = {
    shelf: { label: "Estantería", symbol: "▤" },
    dock: { label: "Bahía", symbol: "▣" },
    charging: { label: "Punto de carga", symbol: "ϟ" }
};

export function mountWarehouseMap({ warehouse, onDraft, onClear, onSelect }) {
    const map = document.querySelector("#warehouse-map");
    const list = document.querySelector("#zone-list");
    const preview = document.createElement("div");
    const message = document.querySelector("#drawing-message");
    const modeButton = document.querySelector("#draw-mode");
    const cancelButton = document.querySelector("#cancel-draft");
    const nodes = new Map();
    let zones = [];
    let draft = null;
    let gesture = null;
    let selectedId = null;
    let disabled = true;
    let drawing = !matchMedia("(pointer: coarse)").matches;

    map.style.setProperty("--columns", warehouse.columns);
    map.style.setProperty("--rows", warehouse.rows);
    // A minimum size keeps 50×50 plans usable through scrolling, not microscopic cells.
    map.style.minWidth = `${warehouse.columns * 32}px`;
    const cells = document.createDocumentFragment();
    for (let row = 1; row <= warehouse.rows; row++) {
        for (let column = 1; column <= warehouse.columns; column++) {
            const cell = document.createElement("span");
            cell.className = "map-cell";
            cell.style.gridRow = row;
            cell.style.gridColumn = column;
            cell.setAttribute("aria-hidden", "true");
            cells.append(cell);
        }
    }
    preview.className = "map-preview";
    preview.hidden = true;
    preview.setAttribute("aria-hidden", "true");
    map.append(cells, preview);

    function place(element, rectangle) {
        element.style.gridRow = `${rectangle.startRow} / span ${rectangle.rows}`;
        element.style.gridColumn = `${rectangle.startColumn} / span ${rectangle.columns}`;
    }

    function showDraft(rectangle) {
        draft = rectangle;
        const error = rectangle ? placementError(rectangle, warehouse, zones) : null;
        const canPlace = rectangle && !error;
        // Out-of-bounds grid tracks must never enlarge the drawing surface.
        preview.hidden = !rectangle || rectangle.startRow < 1 || rectangle.startColumn < 1
            || rectangle.startRow + rectangle.rows - 1 > warehouse.rows
            || rectangle.startColumn + rectangle.columns - 1 > warehouse.columns
            || ![rectangle.startRow, rectangle.startColumn, rectangle.rows, rectangle.columns].every(Number.isInteger)
            || rectangle.rows < 1 || rectangle.columns < 1;
        if (!preview.hidden) place(preview, rectangle);
        preview.dataset.invalid = String(Boolean(error));
        preview.textContent = rectangle ? `${rectangle.columns} × ${rectangle.rows} m` : "";
        message.textContent = error ?? (rectangle
            ? `${rectangle.columns} m de ancho × ${rectangle.rows} m de largo · ${rectangle.rows * rectangle.columns} m². Ponle nombre y guarda la zona.`
            : "Dibuja un rectángulo en un espacio libre o introduce las medidas en el formulario.");
        message.dataset.state = error ? "error" : "ready";
        cancelButton.disabled = disabled || !rectangle;
        return Boolean(canPlace);
    }

    function selectZone(zone, reveal = false) {
        selectedId = zone?.id ?? null;
        for (const [id, pair] of nodes) {
            for (const node of pair) node.setAttribute("aria-pressed", String(id === selectedId));
        }
        onSelect(zone, reveal);
    }

    function zoneButton(zone, inMap) {
        const button = document.createElement("button");
        button.type = "button";
        button.className = inMap ? `map-zone ${zone.kind}` : `zone-entry ${zone.kind}`;
        button.dataset.zoneId = zone.id;
        button.setAttribute("aria-pressed", "false");
        button.setAttribute("aria-label", `${zone.name}, ${zoneTypes[zone.kind].label}, ${zone.columns} por ${zone.rows} metros. Ver características`);
        const symbol = document.createElement("span");
        symbol.className = "zone-symbol";
        symbol.setAttribute("aria-hidden", "true");
        symbol.textContent = zoneTypes[zone.kind].symbol;
        const caption = document.createElement("span");
        caption.className = "zone-caption";
        const name = document.createElement("strong");
        name.textContent = zone.name;
        const kind = document.createElement("span");
        kind.textContent = zoneTypes[zone.kind].label;
        caption.append(name, kind);
        button.append(symbol, caption);
        if (inMap) {
            place(button, zone);
            button.dataset.compact = String(zone.columns < 2 || zone.rows < 2);
        }
        button.addEventListener("click", () => selectZone(zone, true));
        return button;
    }

    function updateZones(records) {
        zones = records;
        // Retain buttons so polling does not discard focus, a pressed pointer or selection.
        for (const [id, pair] of nodes) {
            if (!records.some(zone => zone.id === id)) {
                pair.forEach(node => node.remove());
                nodes.delete(id);
            }
        }
        for (const zone of records) {
            if (nodes.has(zone.id)) continue;
            const pair = [zoneButton(zone, true), zoneButton(zone, false)];
            map.append(pair[0]);
            list.append(pair[1]);
            nodes.set(zone.id, pair);
        }
        for (const [id, pair] of nodes) {
            pair.forEach(node => node.setAttribute("aria-pressed", String(id === selectedId)));
        }
        showDraft(draft);
    }

    function cancelGesture() {
        if (!gesture) return;
        const { previous, pointerId } = gesture;
        gesture = null;
        if (map.hasPointerCapture(pointerId)) map.releasePointerCapture(pointerId);
        showDraft(previous);
    }

    function setMode() {
        cancelGesture();
        map.dataset.drawing = String(drawing);
        modeButton.setAttribute("aria-pressed", String(drawing));
        modeButton.textContent = drawing ? "Dibujar · activo" : "Dibujar zona";
        document.querySelector("#explore-mode").setAttribute("aria-pressed", String(!drawing));
        document.querySelector("#map-instructions").textContent = drawing
            ? "Arrastra sobre las celdas libres. Pulsa una zona para ver sus características. Escape cancela el trazo."
            : "Pulsa una zona para conocerla. Puedes desplazar el plano; activa Dibujar para añadir otra.";
    }

    modeButton.addEventListener("click", () => { drawing = true; setMode(); });
    document.querySelector("#explore-mode").addEventListener("click", () => { drawing = false; setMode(); });
    cancelButton.addEventListener("click", () => { cancelGesture(); showDraft(null); onClear(); });
    document.addEventListener("keydown", event => {
        if (!disabled && event.key === "Escape") {
            if (gesture) cancelGesture();
            else if (draft) { showDraft(null); onClear(); }
        }
    });
    const point = event => cellAtPoint(event.clientX, event.clientY, map.getBoundingClientRect(), warehouse);
    map.addEventListener("pointerdown", event => {
        if (disabled || !drawing || !event.isPrimary || event.button !== 0 || event.target.closest(".map-zone")) return;
        event.preventDefault();
        gesture = { start: point(event), previous: draft, pointerId: event.pointerId };
        map.setPointerCapture(event.pointerId);
        showDraft(rectangleBetween(gesture.start, gesture.start));
    });
    map.addEventListener("pointermove", event => {
        if (gesture?.pointerId === event.pointerId) showDraft(rectangleBetween(gesture.start, point(event)));
    });
    map.addEventListener("pointerup", event => {
        if (gesture?.pointerId !== event.pointerId) return;
        const rectangle = rectangleBetween(gesture.start, point(event));
        gesture = null;
        map.releasePointerCapture(event.pointerId);
        showDraft(rectangle);
        onDraft(rectangle);
    });
    map.addEventListener("pointercancel", cancelGesture);
    map.addEventListener("lostpointercapture", cancelGesture);
    // Scrolling during a captured gesture changes the coordinate frame: cancel it safely.
    document.querySelector("#map-scroll").addEventListener("scroll", cancelGesture);
    window.addEventListener("blur", cancelGesture);
    setMode();
    showDraft(null);

    return {
        updateZones, showDraft, selectZone,
        setDisabled(value) {
            disabled = value;
            if (value) cancelGesture();
            modeButton.disabled = value;
            document.querySelector("#explore-mode").disabled = value;
            cancelButton.disabled = value || !draft;
        }
    };
}
