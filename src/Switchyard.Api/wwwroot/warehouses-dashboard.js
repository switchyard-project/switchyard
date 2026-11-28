import { getWarehouses, createWarehouse } from "./warehouse-api.js";
import { mountCollectionPanel } from "./collection-panel.js";

function renderWarehouses(warehouses) {
    const cards = warehouses.map(warehouse => {
        const card = document.createElement("a");
        card.className = "warehouse-card";
        card.href = `/warehouse.html?warehouse=${warehouse.id}`;
        const title = document.createElement("h3");
        title.textContent = warehouse.name;
        const detail = document.createElement("p");
        detail.textContent = `${warehouse.columns} m de ancho × ${warehouse.rows} m de largo · ${warehouse.robotCount} robots registrados`;
        const action = document.createElement("span");
        action.textContent = "Abrir almacén";
        card.append(title, detail, action);
        return card;
    });
    document.querySelector("#warehouse-list").replaceChildren(...cards);
}

mountCollectionPanel({
    form: document.querySelector("#warehouse-form"), load: getWarehouses, render: renderWarehouses,
    create: data => createWarehouse({ name: data.get("name"), rows: Number(data.get("rows")), columns: Number(data.get("columns")) }),
    createdMessage: warehouse => `${warehouse.name} creado. Ábrelo para añadir sus zonas.`,
    uncertaintyMessage: "Actualiza la lista antes de repetir; el almacén podría haberse guardado."
});
