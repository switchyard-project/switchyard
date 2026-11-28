export function mountCollectionPanel({ form, load, render, create, createdMessage, uncertaintyMessage,
    onCreated = () => {}, onSubmittingChange = () => {} }) {
    const inputs = [...form.querySelectorAll("input, select")];
    const submitButton = form.querySelector("button[type=submit]");
    const submitLabel = submitButton.textContent;
    const refreshButton = document.querySelector("#refresh-button");
    const formMessage = document.querySelector("#form-message");
    const listError = document.querySelector("#list-error");
    const connectionMessage = document.querySelector("#connection-message");
    const emptyState = document.querySelector("#empty-state");
    const timeFormat = new Intl.DateTimeFormat("es", { timeStyle: "medium" });
    let isLoading = false;
    let isSubmitting = false;
    let hasLoaded = false;
    let currentRefresh = null;

    function updateControls() {
        onSubmittingChange(isSubmitting);
        refreshButton.disabled = isLoading || isSubmitting;
        submitButton.disabled = isSubmitting;
        inputs.forEach(input => { input.disabled = isSubmitting; });
        submitButton.textContent = isSubmitting ? "Guardando…" : submitLabel;
        refreshButton.textContent = isLoading ? "Actualizando…" : "Actualizar";
    }

    function refresh() {
        if (isSubmitting) return Promise.resolve();
        if (currentRefresh) return currentRefresh;
        currentRefresh = loadRecords().finally(() => { currentRefresh = null; });
        return currentRefresh;
    }

    async function loadRecords() {
        isLoading = true;
        updateControls();
        try {
            const records = await load();
            render(records);
            hasLoaded = true;
            emptyState.hidden = records.length !== 0;
            listError.hidden = true;
            if (connectionMessage.textContent !== "Consulta al servidor correcta") {
                connectionMessage.textContent = "Consulta al servidor correcta";
            }
            connectionMessage.dataset.state = "success";
            document.querySelector("#last-updated").textContent = `Última consulta: ${timeFormat.format(new Date())}`;
        } catch (error) {
            emptyState.hidden = true;
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

    async function submit(event) {
        event.preventDefault();
        if (isSubmitting) return;
        const data = new FormData(form);
        isSubmitting = true;
        formMessage.textContent = "";
        updateControls();
        let created = false;

        try {
            const record = await create(data);
            formMessage.textContent = createdMessage(record);
            formMessage.dataset.state = "success";
            form.reset();
            onCreated(record);
            created = true;
        } catch (error) {
            formMessage.textContent = `${error.message} ${uncertaintyMessage}`;
            formMessage.dataset.state = "error";
        } finally {
            isSubmitting = false;
            updateControls();
            inputs[0].focus();
        }

        // Esperar la lectura anterior evita que oculte el registro recién creado.
        if (created) {
            await currentRefresh;
            await refresh();
        }
    }

    form.addEventListener("submit", submit);
    refreshButton.addEventListener("click", refresh);
    document.addEventListener("visibilitychange", () => {
        if (!document.hidden) refresh();
    });
    setInterval(() => {
        if (!document.hidden) refresh();
    }, 5000);

    updateControls();
    refresh();
    return { refresh };
}
