(() => {
    const storageKey = "eduassess-theme";
    const themes = ["light", "dark"];

    function applyTheme(theme) {
        document.documentElement.dataset.theme = theme;
        localStorage.setItem(storageKey, theme);
    }

    const savedTheme = localStorage.getItem(storageKey);
    const initialTheme = themes.includes(savedTheme)
        ? savedTheme
        : window.matchMedia("(prefers-color-scheme: dark)").matches
            ? "dark"
            : "light";

    applyTheme(initialTheme);

    window.eduAssessTheme = {
        isDark: () => document.documentElement.dataset.theme === "dark",

        toggle: () => {
            const nextTheme =
                document.documentElement.dataset.theme === "dark" ? "light" : "dark";

            applyTheme(nextTheme);
            return nextTheme === "dark";
        }
    };
})();