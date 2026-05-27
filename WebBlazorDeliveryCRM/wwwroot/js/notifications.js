window.deliveryCrmNotificationsInbox = {
    _observer: null,
    _visibleTimers: new Map(),

    observe: function (dotNetRef, listSelector) {
        this.disconnect();
        var list = document.querySelector(listSelector);
        if (!list || !dotNetRef) return;

        var self = this;
        this._observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                var el = entry.target;
                var id = parseInt(el.getAttribute("data-notification-id"), 10);
                if (!id || id <= 0) return;

                if (entry.isIntersecting && entry.intersectionRatio >= 0.45) {
                    if (self._visibleTimers.has(id)) return;
                    var timer = window.setTimeout(function () {
                        self._visibleTimers.delete(id);
                        dotNetRef.invokeMethodAsync("OnNotificationVisible", id).catch(function () { });
                    }, 400);
                    self._visibleTimers.set(id, timer);
                } else {
                    var existing = self._visibleTimers.get(id);
                    if (existing) {
                        window.clearTimeout(existing);
                        self._visibleTimers.delete(id);
                    }
                }
            });
        }, { threshold: [0, 0.45, 0.6, 1] });

        list.querySelectorAll("[data-notification-id]").forEach(function (el) {
            self._observer.observe(el);
        });
    },

    disconnect: function () {
        if (this._observer) {
            this._observer.disconnect();
            this._observer = null;
        }
        this._visibleTimers.forEach(function (t) { window.clearTimeout(t); });
        this._visibleTimers.clear();
    }
};

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
