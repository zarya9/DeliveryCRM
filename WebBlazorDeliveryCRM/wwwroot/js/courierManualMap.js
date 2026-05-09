/** Отдельная карта Leaflet для ручной позиции курьера (не трогаем singleton leafletMap). */
window.courierManualMap = {
    map: null,
    marker: null,
    baseLayers: {},
    currentTheme: "light",
    dotnetHelper: null,

    init: function (elementId, lat, lon, zoom, dotnetHelper) {
        this.dispose();
        if (typeof L === "undefined") return;
        var el = document.getElementById(elementId);
        if (!el) return;
        this.dotnetHelper = dotnetHelper;
        var theme = document.documentElement.getAttribute("data-theme") || "light";
        this.currentTheme = theme === "dark" ? "dark" : "light";
        zoom = zoom || 15;
        lat = lat || 55.7558;
        lon = lon || 37.6173;

        this.map = L.map(elementId, { attributionControl: false }).setView([lat, lon], zoom);
        var osm = '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener">OSM</a>';
        this.baseLayers = {
            light: L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
                maxZoom: 19,
                attribution: osm,
            }),
            dark: L.tileLayer("https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png", {
                maxZoom: 19,
                attribution: osm,
            }),
        };
        var layer = this.baseLayers[this.currentTheme] || this.baseLayers.light;
        layer.addTo(this.map);

        var attr = L.control.attribution({ position: "bottomright" }).addTo(this.map);
        attr.setPrefix("");
        attr.addAttribution(osm);

        var iconHtml =
            '<div style="width:24px;height:24px;background:var(--dms-primary, #2563eb);border:2px solid #fff;border-radius:50%;box-shadow:0 2px 10px rgba(0,0,0,.4)"></div>';
        var icon = L.divIcon({ className: "courier-manual-marker-pin", html: iconHtml, iconSize: [24, 24], iconAnchor: [12, 12] });

        var self = this;
        this.marker = L.marker([lat, lon], { draggable: true, icon: icon, riseOnHover: true }).addTo(this.map);

        function notify(latLng) {
            if (self.dotnetHelper && latLng)
                self.dotnetHelper.invokeMethodAsync("OnMapMarkerMoved", latLng.lat, latLng.lng).catch(function () {});
        }

        this.marker.on("dragend", function (e) {
            notify(e.target.getLatLng());
        });

        this.map.on("click", function (e) {
            self.marker.setLatLng(e.latlng);
            self.map.panTo(e.latlng);
            notify(e.latlng);
        });
    },

    setMarker: function (lat, lon, shouldPan) {
        if (!this.marker || !this.map) return;
        var lt = parseFloat(lat);
        var ln = parseFloat(lon);
        if (isNaN(lt) || isNaN(ln)) return;
        this.marker.setLatLng([lt, ln]);
        if (shouldPan) this.map.panTo([lt, ln]);
    },

    setTheme: function (theme) {
        if (!this.map || !this.baseLayers) return;
        theme = theme === "dark" ? "dark" : "light";
        var prev = this.baseLayers[this.currentTheme];
        if (prev) this.map.removeLayer(prev);
        this.currentTheme = theme;
        var next = this.baseLayers[this.currentTheme];
        if (next) next.addTo(this.map);
    },

    dispose: function () {
        this.dotnetHelper = null;
        if (this.marker) {
            try {
                this.map.removeLayer(this.marker);
            } catch (_) {}
        }
        this.marker = null;
        if (this.map) {
            try {
                this.map.remove();
            } catch (_) {}
        }
        this.map = null;
        this.baseLayers = {};
    },
};
