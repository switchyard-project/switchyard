// Runs before styles load to avoid painting the wrong theme during navigation.
(() => {
    const storageKey = "switchyard.theme";
    const systemTheme = matchMedia("(prefers-color-scheme: dark)");
    let preference = readPreference();

    function readPreference() {
        try {
            const value = localStorage.getItem(storageKey);
            return value === "light" || value === "dark" ? value : null;
        } catch {
            return null; // The selector still works if browser storage is unavailable.
        }
    }

    function applyTheme() {
        const theme = preference ?? (systemTheme.matches ? "dark" : "light");
        document.documentElement.dataset.theme = theme;
        const button = document.querySelector("#theme-toggle");
        if (button) {
            button.textContent = theme === "dark" ? "Modo claro" : "Modo oscuro";
            button.setAttribute("aria-label", theme === "dark" ? "Activar modo claro" : "Activar modo oscuro");
        }
    }

    applyTheme();
    systemTheme.addEventListener("change", () => { if (preference === null) applyTheme(); });
    window.addEventListener("storage", event => {
        if (event.key === storageKey || event.key === null) {
            preference = readPreference();
            applyTheme();
        }
    });
    document.addEventListener("DOMContentLoaded", () => {
        const button = document.querySelector("#theme-toggle");
        if (!button) return;
        button.hidden = false;
        applyTheme();
        button.addEventListener("click", () => {
            preference = document.documentElement.dataset.theme === "dark" ? "light" : "dark";
            try { localStorage.setItem(storageKey, preference); } catch { /* Keep this page usable without storage. */ }
            applyTheme();
        });
    });
})();
