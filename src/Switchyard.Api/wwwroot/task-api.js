import { requestJson } from "./http-client.js";

const tasksUrl = "/api/tasks";

function isTask(value) {
    if (value === null || typeof value !== "object"
        || !Number.isSafeInteger(value.id) || value.id <= 0
        || typeof value.pickup !== "string" || value.pickup.trim().length === 0 || value.pickup.length > 60
        || typeof value.destination !== "string" || value.destination.trim().length === 0 || value.destination.length > 60) {
        return false;
    }
    if (value.status === "pending") return value.robotId === null;
    return (value.status === "assigned" || value.status === "completed")
        && typeof value.robotId === "string" && /^[A-Z0-9][A-Z0-9_-]{0,31}$/.test(value.robotId);
}

export async function getTasks(warehouseId = 1) {
    const tasks = await requestJson(`${tasksUrl}?warehouseId=${warehouseId}`);
    if (!Array.isArray(tasks) || !tasks.every(task => isTask(task) && task.warehouseId === warehouseId)) {
        throw new Error("La respuesta del servidor no contiene una lista de tareas válida.");
    }
    return tasks;
}

export async function createTask(pickup, destination, warehouseId = 1) {
    const task = await requestJson(tasksUrl, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ pickup, destination, warehouseId })
    }, { 400: "Revisa la recogida y el destino: cada campo necesita entre 1 y 60 caracteres y no puede contener solo espacios." });

    if (!isTask(task) || task.warehouseId !== warehouseId || task.status !== "pending"
        || task.pickup !== pickup.trim() || task.destination !== destination.trim()) {
        throw new Error("El servidor no devolvió la tarea esperada.");
    }
    return task;
}
