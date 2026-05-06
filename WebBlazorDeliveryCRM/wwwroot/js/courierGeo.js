window.courierGeo = {
    tryGetPosition: function () {
        return new Promise(function (resolve, reject) {
            if (!navigator.geolocation) {
                reject(new Error("Браузер не поддерживает геолокацию."));
                return;
            }
            navigator.geolocation.getCurrentPosition(
                function (p) {
                    resolve([p.coords.latitude, p.coords.longitude]);
                },
                function (e) {
                    var code = e && typeof e.code === "number" ? e.code : 0;
                    var msg = e && e.message ? e.message : "";
                    reject(new Error("GEO_" + code + ":" + msg));
                },
                { enableHighAccuracy: true, maximumAge: 15000, timeout: 20000 }
            );
        });
    }
};
