import { getZones, createZone } from "./warehouse-api.js";
import { loadWarehouseContext } from "./warehouse-context.js";
import { mountCollectionPanel } from "./collection-panel.js";
import { mountWarehouseMap, zoneTypes } from "./warehouse-map.js";
import { placementError } from "./warehouse-geometry.js";

const warehouse = await loadWarehouseContext();
const form = document.querySelector("#zone-form");
const coordinateNames = ["startRow", "startColumn", "rows", "columns"];

function readRectangle() {
    return Object.fromEntries(coordinateNames.map(name => [name, Number(form.elements[name].value)]));
}

function showZoneDetails(zone, reveal) {
    const details = document.querySelector("#zone-details");
    details.hidden = !zone;
    if (!zone) return;
    details.dataset.kind = zone.kind;
    document.querySelector("#detail-name").textContent = zone.name;
    document.querySelector("#detail-kind").textContent = zoneTypes[zone.kind].label;
    document.querySelector("#detail-symbol").textContent = zoneTypes[zone.kind].symbol;
    document.querySelector("#detail-position").textContent = `Fila ${zone.startRow}, columna ${zone.startColumn}`;
    document.querySelector("#detail-size").textContent = `${zone.columns} m de ancho × ${zone.rows} m de largo`;
    document.querySelector("#detail-area").textContent = `${zone.rows * zone.columns} m²`;
    if (reveal && matchMedia("(max-width: 1000px)").matches) details.scrollIntoView({ block: "nearest" });
}

if (warehouse) {
    let zones = [];
    let hasLoaded = false;
    document.querySelector("#dimensions").textContent = `${warehouse.name} · ${warehouse.columns} m de ancho × ${warehouse.rows} m de largo`;
    for (const name of ["rows", "startRow"]) form.elements[name].max = warehouse.rows;
    for (const name of ["columns", "startColumn"]) form.elements[name].max = warehouse.columns;

    const editor = mountWarehouseMap({
        warehouse,
        onDraft: rectangle => {
            for (const name of coordinateNames) form.elements[name].value = rectangle[name];
            // Drawing prepares a draft; only submitting the form persists it.
            document.querySelector("#form-message").textContent = "Medidas tomadas del plano. Revisa el tipo y el nombre antes de guardar.";
        },
        onClear: () => { for (const name of coordinateNames) form.elements[name].value = ""; },
        onSelect: showZoneDetails
    });
    for (const name of coordinateNames) {
        form.elements[name].addEventListener("input", () => editor.showDraft(readRectangle()));
    }
    document.querySelector("#close-details").addEventListener("click", () => {
        editor.selectZone(null);
        document.querySelector("#draw-mode").focus();
    });

    mountCollectionPanel({
        form,
        load: () => getZones(warehouse.id),
        render: records => {
            zones = records;
            hasLoaded = true;
            editor.updateZones(records);
            document.querySelector("#zone-count").textContent = `${records.length} zonas`;
        },
        create: data => {
            const rectangle = readRectangle();
            const error = placementError(rectangle, warehouse, zones);
            if (error) throw new Error(error);
            return createZone(warehouse.id, { name: data.get("name"), kind: data.get("kind"), ...rectangle });
        },
        onCreated: zone => {
            editor.showDraft(null);
            editor.selectZone(zone);
        },
        onSubmittingChange: submitting => editor.setDisabled(submitting || !hasLoaded),
        createdMessage: zone => `${zone.name} guardada en el almacén. Ya puedes dibujar la siguiente.`,
        uncertaintyMessage: "Actualiza el plano antes de repetir; si no llegó la respuesta, la zona podría haberse guardado."
    });
}
