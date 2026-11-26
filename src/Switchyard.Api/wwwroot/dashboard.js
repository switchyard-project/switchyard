import { getRobots, registerRobot } from "./robot-api.js";

const form = document.querySelector("#register-form");
const robotId = document.querySelector("#robot-id");
const registerButton = document.querySelector("#register-button");
const refreshButton = document.querySelector("#refresh-button");
const formMessage = document.querySelector("#form-message");
const listError = document.querySelector("#list-error");
const connectionMessage = document.querySelector("#connection-message");
const robotList = document.querySelector("#robot-list");
const robotTemplate = document.querySelector("#robot-template");
const dateFormat = new Intl.DateTimeFormat("es", { dateStyle: "medium", timeStyle: "short" });
const timeFormat = new Intl.DateTimeFormat("es", { timeStyle: "medium" });
let isLoading = false;
let isSubmitting = false;
let hasLoaded = false;
let currentRefresh = null;

function updateControls() {
    refreshButton.disabled = isLoading || isSubmitting;
    registerButton.disabled = isSubmitting;
    robotId.disabled = isSubmitting;
    registerButton.textContent = isSubmitting ? "Registrando…" : "Registrar robot";
    refreshButton.textContent = isLoading ? "Actualizando…" : "Actualizar";
}

function renderRobots(robots) {
    const rows = document.createDocumentFragment();
    for (const robot of robots) {
        const row = robotTemplate.content.cloneNode(true);
        row.querySelector("h3").textContent = robot.id;
        const time = row.querySelector("time");
        time.dateTime = robot.registeredAt;
        time.textContent = dateFormat.format(new Date(robot.registeredAt));
        rows.append(row);
    }
    robotList.replaceChildren(rows);
    document.querySelector("#robot-count").textContent = robots.length;
    document.querySelector("#empty-state").hidden = robots.length !== 0;
}

function refreshRobots() {
    if (isSubmitting) return Promise.resolve();
    if (currentRefresh) return currentRefresh;
    currentRefresh = loadRobots().finally(() => { currentRefresh = null; });
    return currentRefresh;
}

async function loadRobots() {
    isLoading = true;
    updateControls();

    try {
        const robots = await getRobots();
        renderRobots(robots);
        hasLoaded = true;
        listError.hidden = true;
        if (connectionMessage.textContent !== "Consulta al servidor correcta") {
            connectionMessage.textContent = "Consulta al servidor correcta";
        }
        connectionMessage.dataset.state = "success";
        document.querySelector("#last-updated").textContent = `Última consulta: ${timeFormat.format(new Date())}`;
    } catch (error) {
        document.querySelector("#empty-state").hidden = true;
        listError.textContent = `${error.message} ${hasLoaded ? "Se conservan los últimos datos recibidos; pueden estar desactualizados." : "Todavía no se ha podido cargar la lista."}`;
        listError.hidden = false;
        connectionMessage.textContent = hasLoaded ? "Datos pendientes de actualizar" : "Sin datos del servidor";
        connectionMessage.dataset.state = "error";
    } finally {
        document.querySelector("#loading-state").hidden = true;
        isLoading = false;
        updateControls();
    }
}

async function submitRegistration(event) {
    event.preventDefault();
    if (isSubmitting) return;
    isSubmitting = true;
    formMessage.textContent = "";
    updateControls();
    let registered = false;

    try {
        const robot = await registerRobot(robotId.value);
        formMessage.textContent = `${robot.id} registrado. Su alta ya está guardada en el servidor.`;
        formMessage.dataset.state = "success";
        form.reset();
        registered = true;
    } catch (error) {
        formMessage.textContent = `${error.message} Si la respuesta no llegó, actualiza la lista antes de repetir el alta.`;
        formMessage.dataset.state = "error";
    } finally {
        isSubmitting = false;
        updateControls();
        robotId.focus();
    }

    // Una lectura anterior al alta no debe sobrescribir la lista recién actualizada.
    if (registered) {
        await currentRefresh;
        await refreshRobots();
    }
}

form.addEventListener("submit", submitRegistration);
refreshButton.addEventListener("click", refreshRobots);
document.addEventListener("visibilitychange", () => {
    if (!document.hidden) refreshRobots();
});
setInterval(() => {
    if (!document.hidden) refreshRobots();
}, 5000);

updateControls();
refreshRobots();
