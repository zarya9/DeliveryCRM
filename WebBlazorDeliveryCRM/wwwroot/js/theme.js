window.dmsTheme = {
    storageKey: "dms-theme",
    settingsKey: "dms-ui-settings",

    get: function () {
        var t = localStorage.getItem(this.storageKey);
        if (t === "light" || t === "dark") return t;
        return window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches
            ? "dark"
            : "light";
    },

    apply: function (theme) {
        var normalized = theme === "dark" ? "dark" : "light";
        document.documentElement.setAttribute("data-theme", normalized);
        localStorage.setItem(this.storageKey, normalized);
        if (window.leafletMap && typeof window.leafletMap.setTheme === "function") {
            window.leafletMap.setTheme(normalized);
        }
        return normalized;
    },

    getSettings: function () {
        try {
            var raw = localStorage.getItem(this.settingsKey);
            if (!raw) {
                return { accent: "", background: "plain", radius: "12" };
            }
            var parsed = JSON.parse(raw);
            return {
                accent: parsed.accent || "",
                background: parsed.background || "plain",
                radius: parsed.radius || "12"
            };
        } catch {
            return { accent: "", background: "plain", radius: "12" };
        }
    },

    applySettings: function (settings) {
        var s = settings || {};
        var accent = (s.accent || "").trim();
        var background = (s.background || "plain").trim();
        var radius = (s.radius || "12").toString().trim();

        if (accent) {
            document.documentElement.style.setProperty("--dms-primary", accent);
            document.documentElement.style.setProperty("--dms-primary-hover", accent);
        } else {
            document.documentElement.style.removeProperty("--dms-primary");
            document.documentElement.style.removeProperty("--dms-primary-hover");
        }

        document.documentElement.style.setProperty("--dms-radius-lg", radius + "px");

        var body = document.body;
        body.classList.remove("bg-plain", "bg-grid", "bg-dots", "bg-gradient");
        if (background === "grid" || background === "dots" || background === "gradient") {
            body.classList.add("bg-" + background);
        } else {
            body.classList.add("bg-plain");
            background = "plain";
        }

        var normalized = { accent: accent, background: background, radius: radius };
        localStorage.setItem(this.settingsKey, JSON.stringify(normalized));
        return normalized;
    },

    init: function () {
        this.apply(this.get());
        this.applySettings(this.getSettings());
        return this.get();
    },

    toggle: function () {
        var next = this.get() === "dark" ? "light" : "dark";
        return this.apply(next);
    },

    resetSettings: function () {
        var defaults = { accent: "", background: "plain", radius: "12" };
        return this.applySettings(defaults);
    }
};

window.dmsTheme.init();
