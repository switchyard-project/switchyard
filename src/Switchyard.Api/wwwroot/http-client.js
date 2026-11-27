const timeoutMilliseconds = 10000;

export async function requestJson(url, options = {}, errorMessages = {}) {
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), timeoutMilliseconds);

    try {
        const response = await fetch(url, { ...options, cache: "no-store", signal: controller.signal });
        if (!response.ok) {
            throw new Error(errorMessages[response.status]
                ?? `El servidor respondió con un error (HTTP ${response.status}). Inténtalo de nuevo más tarde.`);
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
