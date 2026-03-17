window.deliveryCrmNotifications = {
    requestPermission: function () {
        if (!("Notification" in window)) return Promise.resolve("unsupported");
        if (Notification.permission === "granted") return Promise.resolve("granted");
        if (Notification.permission === "denied") return Promise.resolve("denied");
        return Notification.requestPermission().then(function (p) { return p; });
    },
    isTabVisible: function () {
        return typeof document !== "undefined" && document.visibilityState === "visible";
    },
    showPush: function (title, body) {
        if (!("Notification" in window)) return;
        if (Notification.permission !== "granted") return;
        try {
            var n = new Notification(title || "Delivery CRM", { body: body || "", icon: "/favicon.png" });
            n.onclick = function () { window.focus(); n.close(); };
            setTimeout(function () { n.close(); }, 5000);
        } catch (e) { }
    }
};

window.appTheme = {
    set: function (theme) {
        if (!theme) return;
        theme = theme.toLowerCase();
        if (theme !== "light" && theme !== "dark") theme = "light";
        document.documentElement.setAttribute("data-theme", theme);
    }
};
