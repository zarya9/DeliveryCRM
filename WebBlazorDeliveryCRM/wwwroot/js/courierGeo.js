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
                    reject(e && e.message ? new Error(e.message) : new Error("Нет доступа к геолокации."));
                },
                { enableHighAccuracy: true, maximumAge: 15000, timeout: 20000 }
            );
        });
    }
};
