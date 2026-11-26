const robotsUrl = "/api/robots";
const timeoutMilliseconds = 10000;

async function requestJson(options = {}) {
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), timeoutMilliseconds);

    try {
        const response = await fetch(robotsUrl, { ...options, cache: "no-store", signal: controller.signal });
        if (response.status === 409) {
            throw new Error("Ese identificador ya está registrado. Elige otro o consulta la lista.");
        }
        if (response.status === 400) {
            throw new Error("Revisa el identificador: usa de 1 a 32 caracteres A–Z, 0–9, guion o guion bajo, empezando por una letra o un número.");
        }
        if (!response.ok) {
            throw new Error(`El servidor respondió con un error (HTTP ${response.status}). Inténtalo de nuevo más tarde.`);
        }
        return await response.json();
    } catch (error) {
        if (error.name === "AbortError") {
            throw new Error("El servidor no respondió en 10 segundos.");
        }
        if (error instanceof TypeError) {
            throw new Error("No se pudo contactar con el servidor. Comprueba la conexión.");
        }
        if (error instanceof SyntaxError) {
            throw new Error("La respuesta del servidor no se puede interpretar.");
        }
        throw error;
    } finally {
        clearTimeout(timeout);
    }
}

function isRobot(value) {
    return value !== null && typeof value === "object"
        && typeof value.id === "string" && /^[A-Z0-9][A-Z0-9_-]{0,31}$/.test(value.id)
        && typeof value.registeredAt === "string" && Number.isFinite(Date.parse(value.registeredAt));
}

export async function getRobots() {
    const robots = await requestJson();
    if (!Array.isArray(robots) || !robots.every(isRobot)) {
        throw new Error("La respuesta del servidor no contiene una lista de robots válida.");
    }
    return robots;
}

export async function registerRobot(id) {
    const robot = await requestJson({
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ id })
    });
    if (!isRobot(robot) || robot.id !== id) {
        throw new Error("El servidor no devolvió el registro esperado.");
    }
    return robot;
}
