window.courierGeo = {
    watchId: null,
    dotnetHelper: null,
    throttleMs: 3500,
    /** Минимальный сдвиг в метрах между отправками при частых обновлениях GPS */
    minMoveMeters: 15,
    lastSentAt: 0,
    lastSentLat: null,
    lastSentLon: null,

    haversineMeters: function (lat1, lon1, lat2, lon2) {
        var R = 6371000;
        var toRad = function (d) { return (d * Math.PI) / 180; };
        var dLat = toRad(lat2 - lat1);
        var dLon = toRad(lon2 - lon1);
        var a =
            Math.sin(dLat / 2) * Math.sin(dLat / 2) +
            Math.cos(toRad(lat1)) * Math.cos(toRad(lat2)) * Math.sin(dLon / 2) * Math.sin(dLon / 2);
        var c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
        return R * c;
    },

    tryGetPosition: function () {
        return new Promise(function (resolve, reject) {
            if (!navigator.geolocation) {
                reject(new Error("Браузер не поддерживает геолокацию."));
                return;
            }
            var rej = function (e) {
                var code = e && typeof e.code === "number" ? e.code : 0;
                var msg = e && e.message ? e.message : "";
                reject(new Error("GEO_" + code + ":" + msg));
            };
            /** Сначала GPS/высокая точность; при коде 2 (нет фикса) — сеть/IP/Wi‑Fi через Windows (важно для ПК без GPS). */
            var optsLow = { enableHighAccuracy: false, maximumAge: 120000, timeout: 45000 };
            var optsHigh = { enableHighAccuracy: true, maximumAge: 15000, timeout: 20000 };
            navigator.geolocation.getCurrentPosition(
                function (p) {
                    resolve([p.coords.latitude, p.coords.longitude]);
                },
                function (eHigh) {
                    var code = eHigh && typeof eHigh.code === "number" ? eHigh.code : -1;
                    if (code !== 2) {
                        rej(eHigh);
                        return;
                    }
                    navigator.geolocation.getCurrentPosition(
                        function (p) {
                            resolve([p.coords.latitude, p.coords.longitude]);
                        },
                        function (eLow) {
                            rej(eLow);
                        },
                        optsLow
                    );
                },
                optsHigh
            );
        });
    },

    /** Непрерывное отслеживание + вызов .NET через DotNetObjectReference при изменении позиции */
    startWatch: function (dotnetHelper) {
        this.stopWatch();
        this.dotnetHelper = dotnetHelper;
        if (!navigator.geolocation) return;
        var self = this;
        self.lastSentAt = 0;
        self.lastSentLat = null;
        self.lastSentLon = null;
        self._geoUseHighAccuracy = true;
        var pushSuccess = function (p) {
            self.maybePush(p.coords.latitude, p.coords.longitude);
        };
        var onWatchErr = function (e) {
            var code = e && typeof e.code === "number" ? e.code : 0;
            var msg = e && e.message ? e.message : "";
            if (code === 2 && self._geoUseHighAccuracy && self.dotnetHelper) {
                self._geoUseHighAccuracy = false;
                try {
                    if (self.watchId != null) navigator.geolocation.clearWatch(self.watchId);
                } catch (_) { }
                var lowOpts = { enableHighAccuracy: false, maximumAge: 60000, timeout: 45000 };
                self.watchId = navigator.geolocation.watchPosition(pushSuccess, onWatchErr, lowOpts);
                return;
            }
            if (self.dotnetHelper) {
                self.dotnetHelper
                    .invokeMethodAsync("OnGeoWatchError", "GEO_" + code + ":" + msg)
                    .catch(function () { });
            }
        };
        try {
            this.watchId = navigator.geolocation.watchPosition(pushSuccess, onWatchErr, {
                enableHighAccuracy: true,
                maximumAge: 2000,
                timeout: 25000,
            });
        } catch (err) {
            if (dotnetHelper) {
                dotnetHelper.invokeMethodAsync("OnGeoWatchError", String(err)).catch(function () { });
            }
        }
    },

    stopWatch: function () {
        if (this.watchId != null && navigator.geolocation) {
            try {
                navigator.geolocation.clearWatch(this.watchId);
            } catch (_) { }
        }
        this.watchId = null;
        this.dotnetHelper = null;
        this.lastSentAt = 0;
        this.lastSentLat = null;
        this.lastSentLon = null;
    },

    maybePush: function (lat, lon) {
        var h = this.dotnetHelper;
        if (!h) return;
        var now = Date.now();
        var minInterval = this.throttleMs;
        if (this.lastSentLat != null && this.lastSentLon != null) {
            var moved = this.haversineMeters(this.lastSentLat, this.lastSentLon, lat, lon);
            if (moved < this.minMoveMeters && now - this.lastSentAt < minInterval * 2) return;
        }
        if (now - this.lastSentAt < minInterval) return;
        this.lastSentAt = now;
        this.lastSentLat = lat;
        this.lastSentLon = lon;
        h.invokeMethodAsync("OnGeoPosition", lat, lon).catch(function () { });
    }
};
