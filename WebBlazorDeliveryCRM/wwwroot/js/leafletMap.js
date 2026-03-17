window.leafletMap = {
    map: null,
    markers: [],
    routeLayer: null,
    baseLayers: {},
    currentTheme: "light",
    routeStart: null,
    routeEnd: null,
    routeMarkers: [],

    init: function (elementId, centerLat, centerLon, zoom, theme) {
        if (this.map) {
            this.dispose();
        }
        var el = document.getElementById(elementId);
        if (!el) return;
        centerLat = centerLat || 55.7558;
        centerLon = centerLon || 37.6173;
        zoom = zoom || 10;
        if (!theme) {
            var t = document.documentElement.getAttribute("data-theme");
            theme = t || "light";
        }
        this.currentTheme = theme || "light";

        // создаём карту без стандартной атрибуции, чтобы убрать флаг-эмодзи
        this.map = L.map(elementId, {
            attributionControl: false
        }).setView([centerLat, centerLon], zoom);

        // OSM/Carto tile layers для разных тем
        this.baseLayers = {
            light: L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
            }),
            dark: L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
                attribution: '&copy; <a href=\"https://carto.com/attributions\">CARTO</a>, &copy; OpenStreetMap'
            }),
            contrast: L.tileLayer('https://{s}.tile.openstreetmap.fr/hot/{z}/{x}/{y}.png', {
                attribution: '&copy; OpenStreetMap, Humanitarian style'
            })
        };

        var layer = this.baseLayers[this.currentTheme] || this.baseLayers.light;
        layer.addTo(this.map);

        // собственный, нейтральный контрол атрибуции без флага
        var attr = L.control.attribution({ position: "bottomright" }).addTo(this.map);
        attr.setPrefix(""); // убираем "Leaflet" с ссылкой
        attr.addAttribution("Leaflet | \u00a9 OpenStreetMap");

        // обработчик кликов по карте для демонстрации A*
        var self = this;
        this.map.on("click", function (e) {
            self.handleClickForRoute(e.latlng);
        });
        this.markers = [];
        this.routeLayer = null;
    },

    setTheme: function (theme) {
        if (!this.map || !this.baseLayers) return;
        if (!theme) {
            var t = document.documentElement.getAttribute("data-theme");
            theme = t || "light";
        }
        if (this.baseLayers[this.currentTheme]) {
            this.map.removeLayer(this.baseLayers[this.currentTheme]);
        }
        this.currentTheme = theme || "light";
        var layer = this.baseLayers[this.currentTheme] || this.baseLayers.light;
        layer.addTo(this.map);
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
        // маркеры маршрута не трогаем здесь
    },

    handleClickForRoute: function (latlng) {
        if (!this.map) return;

        // первый клик — старт, второй — финиш, третий сбрасывает и начинает заново
        if (!this.routeStart) {
            this.clearRoute();
            this.routeStart = latlng;
            this.routeEnd = null;
            this.addRouteMarker(latlng, "Старт");
            return;
        }

        if (!this.routeEnd) {
            this.routeEnd = latlng;
            this.addRouteMarker(latlng, "Финиш");
            this.drawRouteAStar(this.routeStart.lat, this.routeStart.lng, this.routeEnd.lat, this.routeEnd.lng);
            return;
        }

        // если уже есть старт и финиш — начинаем выбор заново
        this.clearRoute();
        this.routeStart = latlng;
        this.addRouteMarker(latlng, "Старт");
    },

    addRouteMarker: function (latlng, label) {
        if (!this.map) return;
        var m = L.marker([latlng.lat, latlng.lng]).addTo(this.map).bindPopup(label);
        this.routeMarkers.push(m);
    },

    clearRoute: function () {
        if (this.routeLayer && this.map) {
            this.map.removeLayer(this.routeLayer);
            this.routeLayer = null;
        }
        for (var i = 0; i < this.routeMarkers.length; i++) {
            this.map.removeLayer(this.routeMarkers[i]);
        }
        this.routeMarkers = [];
        this.routeStart = null;
        this.routeEnd = null;
    },

    // Простой A* по сетке в пределах текущих границ карты.
    drawRouteAStar: function (startLat, startLon, endLat, endLon) {
        if (!this.map) return;

        var bounds = this.map.getBounds();
        var rows = 40;
        var cols = 40;

        function latToRow(lat) {
            return Math.round((bounds.getNorth() - lat) / (bounds.getNorth() - bounds.getSouth()) * (rows - 1));
        }
        function lonToCol(lon) {
            return Math.round((lon - bounds.getWest()) / (bounds.getEast() - bounds.getWest()) * (cols - 1));
        }
        function rowToLat(row) {
            return bounds.getNorth() - (row / (rows - 1)) * (bounds.getNorth() - bounds.getSouth());
        }
        function colToLon(col) {
            return bounds.getWest() + (col / (cols - 1)) * (bounds.getEast() - bounds.getWest());
        }

        var start = { r: latToRow(startLat), c: lonToCol(startLon) };
        var goal = { r: latToRow(endLat), c: lonToCol(endLon) };

        function h(a, b) {
            var dr = a.r - b.r;
            var dc = a.c - b.c;
            return Math.sqrt(dr * dr + dc * dc);
        }

        function key(n) { return n.r + ':' + n.c; }

        var open = {};
        var openArr = [];
        var cameFrom = {};
        var gScore = {};

        function addOpen(n, g, f) {
            var k = key(n);
            gScore[k] = g;
            open[k] = { node: n, f: f };
            openArr.push(open[k]);
        }

        addOpen(start, 0, h(start, goal));

        var dirs = [
            { dr: 1, dc: 0 }, { dr: -1, dc: 0 },
            { dr: 0, dc: 1 }, { dr: 0, dc: -1 },
            { dr: 1, dc: 1 }, { dr: 1, dc: -1 },
            { dr: -1, dc: 1 }, { dr: -1, dc: -1 }
        ];

        var closed = {};
        var found = false;

        while (openArr.length > 0) {
            openArr.sort(function (a, b) { return a.f - b.f; });
            var currentWrap = openArr.shift();
            var current = currentWrap.node;
            var ck = key(current);
            if (!open[ck]) continue;
            delete open[ck];

            if (current.r === goal.r && current.c === goal.c) {
                found = true;
                break;
            }

            closed[ck] = true;

            for (var i = 0; i < dirs.length; i++) {
                var nr = current.r + dirs[i].dr;
                var nc = current.c + dirs[i].dc;
                if (nr < 0 || nr >= rows || nc < 0 || nc >= cols) continue;
                var neighbor = { r: nr, c: nc };
                var nk = key(neighbor);
                if (closed[nk]) continue;

                var tentativeG = gScore[ck] + ((i < 4) ? 1 : Math.SQRT2);
                if (!(nk in gScore) || tentativeG < gScore[nk]) {
                    cameFrom[nk] = current;
                    var f = tentativeG + h(neighbor, goal);
                    addOpen(neighbor, tentativeG, f);
                }
            }
        }

        if (!found) {
            return;
        }

        var path = [];
        var cur = goal;
        while (cur) {
            path.push([rowToLat(cur.r), colToLon(cur.c)]);
            var ck2 = key(cur);
            cur = cameFrom[ck2];
        }
        path.reverse();

        if (this.routeLayer) {
            this.map.removeLayer(this.routeLayer);
        }
        this.routeLayer = L.polyline(path, {
            color: '#2563eb',
            weight: 5,
            opacity: 0.85
        }).addTo(this.map);
        this.map.fitBounds(this.routeLayer.getBounds().pad(0.15));
    },

    dispose: function () {
        this.clearMarkers();
        if (this.routeLayer && this.map) {
            this.map.removeLayer(this.routeLayer);
            this.routeLayer = null;
        }
        for (var i = 0; i < this.routeMarkers.length; i++) {
            this.map.removeLayer(this.routeMarkers[i]);
        }
        this.routeMarkers = [];
        this.routeStart = null;
        this.routeEnd = null;
        if (this.map) {
            this.map.remove();
            this.map = null;
        }
        this.baseLayers = {};
    }
};
