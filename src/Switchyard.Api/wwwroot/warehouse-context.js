import { getWarehouse } from "./warehouse-api.js";

export async function loadWarehouseContext() {
    const id = Number(new URLSearchParams(location.search).get("warehouse"));
    if (!Number.isSafeInteger(id) || id <= 0) {
        location.replace("/");
        return null;
    }
    try {
        const warehouse = await getWarehouse(id);
        document.querySelector("#warehouse-name").textContent = warehouse.name;
        for (const link of document.querySelectorAll("[data-warehouse-link]")) {
            const url = new URL(link.href);
            url.searchParams.set("warehouse", id);
            link.href = url;
        }
        document.querySelector(".intro").append(` · ${warehouse.name}`);
        return warehouse;
    } catch (error) {
        const message = document.querySelector("#context-error");
        message.textContent = `${error.message} Recarga la página para reintentar o vuelve a Almacenes.`;
        message.hidden = false;
        document.querySelector("#loading-state").hidden = true;
        document.querySelector("#connection-message").textContent = "No se pudo cargar el almacén";
        return null;
    }
}
