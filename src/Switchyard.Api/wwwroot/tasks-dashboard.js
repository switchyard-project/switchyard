import { getRobots } from "./robot-api.js";
import { mountAssignmentForm } from "./task-assignment.js";
import { loadWarehouseContext } from "./warehouse-context.js";
import { getTasks, createTask } from "./task-api.js";
import { mountCollectionPanel } from "./collection-panel.js";

const taskList = document.querySelector("#task-list");
const taskTemplate = document.querySelector("#task-template");
const statusLabels = { pending: "Pendiente", assigned: "Asignada", completed: "Completada" };

function renderTasks(tasks) {
    const rows = document.createDocumentFragment();
    for (const task of tasks) {
        const row = taskTemplate.content.cloneNode(true);
        row.querySelector("h3").textContent = `Tarea #${task.id}`;
        const status = row.querySelector(".task-status");
        status.textContent = statusLabels[task.status];
        status.dataset.state = task.status;
        row.querySelector(".task-pickup").textContent = task.pickup;
        row.querySelector(".task-destination").textContent = task.destination;
        row.querySelector(".task-robot").textContent = task.robotId ?? "Sin asignar";
        rows.append(row);
    }
    taskList.replaceChildren(rows);
    assignment.update(tasks, robots);
    document.querySelector("#task-count").textContent = tasks.length;
}

const warehouse = await loadWarehouseContext();
let robots = [];
let assignment;
if (warehouse) {
    const panel = mountCollectionPanel({
        form: document.querySelector("#task-form"),
        load: async () => {
            const [tasks, latestRobots] = await Promise.all([getTasks(warehouse.id), getRobots(warehouse.id)]);
            robots = latestRobots;
            return tasks;
        },
        render: renderTasks,
        create: data => createTask(data.get("pickup"), data.get("destination"), warehouse.id),
        createdMessage: task => `Tarea #${task.id} creada. Ya está pendiente de asignación.`,
        uncertaintyMessage: "Si no recibiste confirmación, actualiza y comprueba la lista antes de crear otra: podría haberse guardado."
    });
    assignment = mountAssignmentForm(warehouse.id, panel.refresh);
}
