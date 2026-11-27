import { getRobots, registerRobot } from "./robot-api.js";
import { mountCollectionPanel } from "./collection-panel.js";

const robotList = document.querySelector("#robot-list");
const robotTemplate = document.querySelector("#robot-template");
const dateFormat = new Intl.DateTimeFormat("es", { dateStyle: "medium", timeStyle: "short" });

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
}


mountCollectionPanel({
    form: document.querySelector("#register-form"),
    load: getRobots,
    render: renderRobots,
    create: data => registerRobot(data.get("id")),
    createdMessage: robot => `${robot.id} registrado. Su alta ya está guardada en el servidor.`,
    uncertaintyMessage: "Si la respuesta no llegó, actualiza la lista antes de repetir el alta."
});
