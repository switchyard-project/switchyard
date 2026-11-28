import { assignTask } from "./task-api.js";

export function mountAssignmentForm(warehouseId, refresh) {
    const form = document.querySelector("#assignment-form");
    const taskSelect = form.querySelector("#assignment-task");
    const robotSelect = form.querySelector("#assignment-robot");
    const button = form.querySelector("button");
    const message = document.querySelector("#assignment-message");
    let isSubmitting = false;
    let latestTasks = [];
    let latestRobots = [];

    function replaceOptions(select, options, placeholder) {
        // Keep both the selection and the focused native control stable during polling.
        const signature = JSON.stringify(options);
        if (select.dataset.options === signature) return;
        const selected = select.value;
        select.replaceChildren(new Option(placeholder, ""), ...options.map(([value, label]) => new Option(label, value)));
        if (options.some(([value]) => value === selected)) select.value = selected;
        select.dataset.options = signature;
    }

    function update(tasks, robots) {
        latestTasks = tasks;
        latestRobots = robots;
        if (isSubmitting) return;
        const pending = tasks.filter(task => task.status === "pending");
        const busy = new Set(tasks.filter(task => task.status === "assigned").map(task => task.robotId));
        const available = robots.filter(robot => !busy.has(robot.id));
        replaceOptions(taskSelect, pending.map(task => [String(task.id), `#${task.id} · ${task.pickup} → ${task.destination}`]),
            pending.length ? "Elige una tarea" : "No hay tareas pendientes");
        replaceOptions(robotSelect, available.map(robot => [robot.id, robot.id]),
            available.length ? "Elige un robot" : "No hay robots disponibles");
        taskSelect.disabled = pending.length === 0;
        robotSelect.disabled = available.length === 0;
        button.disabled = pending.length === 0 || available.length === 0;
    }

    form.addEventListener("submit", async event => {
        event.preventDefault();
        if (isSubmitting || !form.reportValidity()) return;
        const taskId = Number(taskSelect.value);
        const robotId = robotSelect.value;
        isSubmitting = true;
        taskSelect.disabled = robotSelect.disabled = button.disabled = true;
        message.textContent = "Guardando asignación…";
        try {
            await assignTask(taskId, robotId, warehouseId);
            message.textContent = `Tarea #${taskId} asignada a ${robotId}. Ya puede recogerla desde su consola.`;
        } catch (error) {
            message.textContent = `${error.message} Si no llegó la respuesta, consulta el estado antes de reintentar: puede haberse guardado.`;
        } finally {
            // Drain any older GET, then request a fresh snapshot after the write.
            await refresh();
            await refresh();
            isSubmitting = false;
            update(latestTasks, latestRobots);
        }
    });
    return { update };
}
