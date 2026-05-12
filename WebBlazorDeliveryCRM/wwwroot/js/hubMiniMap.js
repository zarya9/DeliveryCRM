/**
 * Мини-карты складов: отдельные экземпляры Leaflet (не singleton leafletMap).
 * Без панели атрибуции OSM — юридически на странице остаётся короткая строка © OSM под списком.
 */
window.hubMiniMap = (function () {
    var maps = {};

    return {
        init: function (elementId, lat, lon) {
            if (typeof L === "undefined") return;
            this.dispose(elementId);
            var el = document.getElementById(elementId);
            if (!el) return;
            var latN = parseFloat(lat);
            var lonN = parseFloat(lon);
            if (isNaN(latN) || isNaN(lonN)) return;

            var map = L.map(el, {
                attributionControl: false,
                zoomControl: false,
                scrollWheelZoom: false,
                dragging: false,
                doubleClickZoom: false,
                boxZoom: false,
                keyboard: false
            }).setView([latN, lonN], 16);

            L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
                maxZoom: 19,
                attribution: ""
            }).addTo(map);

            var hubSvg =
                '<svg viewBox="0 0 24 24" width="20" height="20" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true"><path d="M3.50002 10V15C3.50002 17.8284 3.50002 19.2426 4.37869 20.1213C5.25737 21 6.67159 21 9.50002 21H14.5C17.3284 21 18.7427 21 19.6213 20.1213C20.5 19.2426 20.5 17.8284 20.5 15V10M17 7.50184C17 8.88255 15.8807 9.99997 14.5 9.99997C13.1193 9.99997 12 8.88068 12 7.49997C12 8.88068 10.8807 9.99997 9.50002 9.99997C8.1193 9.99997 7.00002 8.88068 7.00002 7.49997C7.00002 8.88068 5.82655 9.99997 4.37901 9.99997C3.59984 9.99997 2.90008 9.67567 2.42 9.16087C1.59462 8.2758 2.12561 6.97403 2.81448 5.98842L3.20202 5.45851C4.08386 4.2527 4.52478 3.6498 5.16493 3.32494C5.80508 3.00008 6.55201 3.00018 8.04587 3.00038L15.9551 3.00143C17.4485 3.00163 18.1952 3.00173 18.8351 3.32658C19.475 3.65143 19.9158 4.25414 20.7974 5.45957L21.1855 5.99029C21.8744 6.97589 22.4054 8.27766 21.58 9.16273C21.0999 9.67754 20.4002 10.0018 19.621 10.0018C18.1734 10.0018 17 8.88255 17 7.50184Z" stroke="#0f766e" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/></svg>';
            var hubWrap =
                '<div class="lm-pin-wrap" style="width:30px;height:30px;display:flex;align-items:center;justify-content:center;background:#fff;border-radius:8px;box-shadow:0 1px 4px rgba(0,0,0,.25);border:1px solid rgba(0,0,0,.08)">' +
                hubSvg +
                "</div>";
            var hubIcon = L.divIcon({
                html: hubWrap,
                className: "lm-div-marker",
                iconSize: [30, 30],
                iconAnchor: [15, 15]
            });
            L.marker([latN, lonN], { icon: hubIcon }).addTo(map);
            maps[elementId] = map;

            setTimeout(function () {
                try {
                    map.invalidateSize();
                } catch (_) { }
            }, 80);
        },

        dispose: function (elementId) {
            var m = maps[elementId];
            if (!m) return;
            try {
                m.remove();
            } catch (_) { }
            delete maps[elementId];
        }
    };
})();
