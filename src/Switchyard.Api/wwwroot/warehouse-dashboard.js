import { getZones, createZone } from "./warehouse-api.js";
import { loadWarehouseContext } from "./warehouse-context.js";
import { mountCollectionPanel } from "./collection-panel.js";

const labels = { shelf: "Estantería", dock: "Bahía", charging: "Punto de carga" };
const warehouse = await loadWarehouseContext();

function renderZones(zones) {
    const map = document.querySelector("#warehouse-map");
    map.style.gridTemplateColumns = `repeat(${warehouse.columns}, 1fr)`;
    map.style.gridTemplateRows = `repeat(${warehouse.rows}, 1fr)`;
    map.style.aspectRatio = `${warehouse.columns} / ${warehouse.rows}`;
    const cells = Array.from({ length: warehouse.rows * warehouse.columns }, (_, index) => {
        const cell = document.createElement("span");
        cell.className = "map-cell";
        cell.style.gridRow = Math.floor(index / warehouse.columns) + 1;
        cell.style.gridColumn = index % warehouse.columns + 1;
        cell.setAttribute("aria-hidden", "true");
        return cell;
    });
    const descriptions = [];
    zones.forEach((zone, index) => {
        const block = document.createElement("span");
        block.className = `map-zone ${zone.kind}`;
        block.style.gridRow = `${zone.startRow} / span ${zone.rows}`;
        block.style.gridColumn = `${zone.startColumn} / span ${zone.columns}`;
        block.textContent = index + 1;
        block.title = `${zone.name} · ${labels[zone.kind]}`;
        block.setAttribute("aria-hidden", "true");
        cells.push(block);
        const description = document.createElement("p");
        description.className = "zone-description";
        description.textContent = `${index + 1}. ${zone.name} · ${labels[zone.kind]} — fila ${zone.startRow}, columna ${zone.startColumn}; ${zone.rows} × ${zone.columns} celdas.`;
        descriptions.push(description);
    });
    map.replaceChildren(...cells);
    document.querySelector("#zone-list").replaceChildren(...descriptions);
}

if (warehouse) {
    document.querySelector("#dimensions").textContent = `${warehouse.name} · ${warehouse.rows} filas × ${warehouse.columns} columnas. Coordenadas desde 1.`;
    for (const name of ["rows", "startRow"]) document.getElementById(name).max = warehouse.rows;
    for (const name of ["columns", "startColumn"]) document.getElementById(name).max = warehouse.columns;
    mountCollectionPanel({
        form: document.querySelector("#zone-form"), load: () => getZones(warehouse.id), render: renderZones,
        create: data => createZone(warehouse.id, {
            name: data.get("name"), kind: data.get("kind"), startRow: Number(data.get("startRow")),
            startColumn: Number(data.get("startColumn")), rows: Number(data.get("rows")), columns: Number(data.get("columns"))
        }),
        createdMessage: zone => `${zone.name} añadida al plano.`,
        uncertaintyMessage: "Actualiza el plano antes de repetir; la zona podría haberse guardado."
    });
}
