import { requestJson } from "./http-client.js";

const robotsUrl = "/api/robots";
const errorMessages = {
    400: "Revisa el identificador: usa de 1 a 32 caracteres A–Z, 0–9, guion o guion bajo, empezando por una letra o un número.",
    409: "Ese identificador ya está registrado. Elige otro o consulta la lista."
};

function isRobot(value) {
    return value !== null && typeof value === "object"
        && typeof value.id === "string" && /^[A-Z0-9][A-Z0-9_-]{0,31}$/.test(value.id)
        && typeof value.registeredAt === "string" && Number.isFinite(Date.parse(value.registeredAt));
}

export async function getRobots(warehouseId = 1) {
    const robots = await requestJson(`${robotsUrl}?warehouseId=${warehouseId}`, {}, errorMessages);
    if (!Array.isArray(robots) || !robots.every(robot => isRobot(robot) && robot.warehouseId === warehouseId)) {
        throw new Error("La respuesta del servidor no contiene una lista de robots válida.");
    }
    return robots;
}

export async function registerRobot(id, warehouseId = 1) {
    const robot = await requestJson(robotsUrl, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ id, warehouseId })
    }, errorMessages);
    if (!isRobot(robot) || robot.warehouseId !== warehouseId || robot.id !== id) {
        throw new Error("El servidor no devolvió el registro esperado.");
    }
    return robot;
}
