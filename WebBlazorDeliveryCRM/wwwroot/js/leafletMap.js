window.leafletMap = {
    map: null,
    markers: [],

    init: function (elementId, centerLat, centerLon, zoom) {
        if (this.map) {
            this.dispose();
        }
        var el = document.getElementById(elementId);
        if (!el) return;
        centerLat = centerLat || 55.7558;
        centerLon = centerLon || 37.6173;
        zoom = zoom || 10;
        this.map = L.map(elementId).setView([centerLat, centerLon], zoom);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
        }).addTo(this.map);
        this.markers = [];
    },

    setMarkers: function (items) {
        if (!this.map || !items || !items.length) return;
        this.clearMarkers();
        for (var i = 0; i < items.length; i++) {
            var lat = parseFloat(items[i].lat);
            var lon = parseFloat(items[i].lon);
            if (isNaN(lat) || isNaN(lon)) continue;
            var title = items[i].title || ('Точка ' + (i + 1));
            var marker = L.marker([lat, lon]).addTo(this.map).bindPopup(title);
            this.markers.push(marker);
        }
        if (this.markers.length > 1) {
            var group = new L.featureGroup(this.markers);
            this.map.fitBounds(group.getBounds().pad(0.1));
        }
    },

    clearMarkers: function () {
        for (var i = 0; i < this.markers.length; i++) {
            this.map.removeLayer(this.markers[i]);
        }
        this.markers = [];
    },

    dispose: function () {
        this.clearMarkers();
        if (this.map) {
            this.map.remove();
            this.map = null;
        }
    }
};
