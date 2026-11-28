import { requestJson } from "./http-client.js";

const messages = {
    400: "Revisa los campos y comprueba que la zona cabe dentro del plano.",
    404: "No se ha encontrado el almacén. Vuelve a elegirlo desde el inicio.",
    409: "Ese nombre ya existe o la zona se solapa con otra. Revisa el plano."
};

function isWarehouse(value) {
    return value && Number.isSafeInteger(value.id) && value.id > 0
        && typeof value.name === "string" && value.name.trim().length > 0
        && Number.isInteger(value.rows) && value.rows >= 1 && value.rows <= 50
        && Number.isInteger(value.columns) && value.columns >= 1 && value.columns <= 50
        && Number.isInteger(value.robotCount) && value.robotCount >= 0;
}

export async function getWarehouses() {
    const values = await requestJson("/api/warehouses", {}, messages);
    if (!Array.isArray(values) || !values.every(isWarehouse)) throw new Error("Lista de almacenes inválida.");
    return values;
}

export async function getWarehouse(id) {
    const value = await requestJson(`/api/warehouses/${id}`, {}, messages);
    if (!isWarehouse(value) || value.id !== id) throw new Error("El servidor devolvió otro almacén.");
    return value;
}

export async function createWarehouse(data) {
    const value = await requestJson("/api/warehouses", {
        method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(data)
    }, messages);
    if (!isWarehouse(value)) throw new Error("No se recibió un almacén válido.");
    return value;
}

function isZone(value) {
    return value && Number.isSafeInteger(value.id) && value.id > 0
        && typeof value.name === "string" && value.name.trim().length > 0
        && ["shelf", "dock", "charging"].includes(value.kind)
        && [value.startRow, value.startColumn, value.rows, value.columns]
            .every(number => Number.isInteger(number) && number >= 1 && number <= 50);
}

export async function getZones(id) {
    const values = await requestJson(`/api/warehouses/${id}/zones`, {}, messages);
    if (!Array.isArray(values) || !values.every(isZone)) throw new Error("Lista de zonas inválida.");
    return values;
}

export async function createZone(id, data) {
    const value = await requestJson(`/api/warehouses/${id}/zones`, {
        method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(data)
    }, messages);
    if (!isZone(value)) throw new Error("No se recibió una zona válida.");
    return value;
}
