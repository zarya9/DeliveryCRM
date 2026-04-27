/**
 * Leaflet + OSM тайлы + OSRM через Leaflet Routing Machine (панель шагов, язык — см. routeLanguage).
 * Как в https://ru.stackoverflow.com/questions/1597647/ — language + Formatter для локализации инструкций.
 */
window.leafletMap = {
    map: null,
    markers: [],
    routeLayer: null,
    routingControl: null,
    baseLayers: {},
    currentTheme: "light",
    routeStart: null,
    routeEnd: null,
    routeMarkers: [],
    heatLayer: null,
    circleLayers: [],
    /** Базовый URL OSRM без завершающего слэша */
    osrmBaseUrl: "",
    /** Язык пошаговых инструкций (код для LRM: ru, en, de, fr, …) */
    routeLanguage: "ru",
    /** Построение маршрута по кликам (только страница логиста) */
    enableRouting: false,
    /** AbortController для текущего OSRM запроса */
    routeAbortController: null,
    /** Счётчик запроса маршрута для отсечения устаревших ответов */
    routeRequestId: 0,

    init: function (elementId, centerLat, centerLon, zoom, theme, osrmBaseUrl, enableRouting, routeLanguage) {
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
        this.osrmBaseUrl = (osrmBaseUrl && String(osrmBaseUrl).trim()) || "";
        this.enableRouting = enableRouting !== false;
        this.routeLanguage = (routeLanguage && String(routeLanguage).trim()) || "ru";

        this.map = L.map(elementId, {
            attributionControl: false
        }).setView([centerLat, centerLon], zoom);

        this.baseLayers = {
            light: L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
                attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
            }),
            dark: L.tileLayer("https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png", {
                attribution: '&copy; <a href="https://carto.com/attributions">CARTO</a>, &copy; OpenStreetMap'
            }),
            contrast: L.tileLayer("https://{s}.tile.openstreetmap.fr/hot/{z}/{x}/{y}.png", {
                attribution: "&copy; OpenStreetMap, Humanitarian style"
            })
        };

        var layer = this.baseLayers[this.currentTheme] || this.baseLayers.light;
        layer.addTo(this.map);

        var attr = L.control.attribution({ position: "bottomright" }).addTo(this.map);
        attr.setPrefix("");
        attr.addAttribution(
            "Leaflet | \u00a9 OpenStreetMap | OSRM | \u041c\u0430\u0440\u0448\u0440\u0443\u0442: Leaflet Routing Machine"
        );

        this.markers = [];
        this.routeLayer = null;
        this.routingControl = null;
        this.heatLayer = null;
        this.circleLayers = [];

        var self = this;
        if (this.enableRouting && typeof L.Routing !== "undefined" && L.Routing.osrmv1 && L.Routing.control) {
            var base = this.osrmBaseUrl || "https://router.project-osrm.org";
            base = base.replace(/\/$/, "");
            var serviceUrl = base + "/route/v1";
            var router = L.Routing.osrmv1({
                serviceUrl: serviceUrl,
                profile: "driving",
                useHints: false,
                /** Полная геометрия по дорогам; иначе LRM/OSRM по умолчанию может дать «ломаную»/упрощённую линию. */
                requestParameters: {
                    overview: "full",
                    geometries: "geojson"
                }
            });
            var lang = this.routeLanguage;
            var fmt = new L.Routing.Formatter({ language: lang, units: "metric" });
            this.routingControl = L.Routing.control({
                waypoints: [],
                router: router,
                language: lang,
                formatter: fmt,
                routeWhileDragging: false,
                addWaypoints: false,
                /**
                 * Видимую линию рисуем только через drawRouteOsrm (тот же OSRM, overview=full).
                 * Линия самого LRM при ошибке/лимите часто превращается в прямые отрезки между точками.
                 */
                lineOptions: {
                    styles: [{ color: "#2563eb", weight: 5, opacity: 0 }]
                },
                showAlternatives: false,
                collapsible: true
            });
            this.routingControl.addTo(this.map);
            this.map.on("click", function (e) {
                self.handleClickForRouteLrm(e.latlng);
            });
        } else if (this.enableRouting) {
            this.map.on("click", function (e) {
                self.handleClickForRouteLegacy(e.latlng);
            });
        }
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
        var layer = this.baseLayers.light;
        layer.addTo(this.map);
    },

    setMarkers: function (items) {
        if (!this.map || !items || !items.length) return;
        this.clearMarkers();
        for (var i = 0; i < items.length; i++) {
            var lat = parseFloat(items[i].lat);
            var lon = parseFloat(items[i].lon);
            if (isNaN(lat) || isNaN(lon)) continue;
            var title = items[i].title || "Точка " + (i + 1);
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

    setHeatPoints: function (items) {
        if (!this.map || !items || !items.length || typeof L.heatLayer === "undefined") return;
        this.clearHeatPoints();
        var points = [];
        for (var i = 0; i < items.length; i++) {
            var lat = parseFloat(items[i].lat);
            var lon = parseFloat(items[i].lon);
            if (isNaN(lat) || isNaN(lon)) continue;
            var intensity = parseFloat(items[i].intensity);
            if (isNaN(intensity)) intensity = 0.6;
            points.push([lat, lon, intensity]);
        }
        if (!points.length) return;
        this.heatLayer = L.heatLayer(points, {
            radius: 26,
            blur: 20,
            maxZoom: 15,
            minOpacity: 0.35
        }).addTo(this.map);
    },

    clearHeatPoints: function () {
        if (!this.map || !this.heatLayer) return;
        this.map.removeLayer(this.heatLayer);
        this.heatLayer = null;
    },

    setCircles: function (items) {
        if (!this.map || !items || !items.length) return;
        this.clearCircles();
        for (var i = 0; i < items.length; i++) {
            var lat = parseFloat(items[i].lat);
            var lon = parseFloat(items[i].lon);
            var radiusKm = parseFloat(items[i].radiusKm);
            if (isNaN(lat) || isNaN(lon) || isNaN(radiusKm)) continue;
            var title = items[i].title || "Зона";
            var c = L.circle([lat, lon], {
                radius: Math.max(0.1, radiusKm) * 1000.0,
                color: items[i].color || "#22c55e",
                fillColor: items[i].fillColor || "#22c55e",
                fillOpacity: items[i].fillOpacity || 0.12,
                weight: 1.5
            }).addTo(this.map).bindPopup(title);
            this.circleLayers.push(c);
        }
    },

    clearCircles: function () {
        if (!this.map || !this.circleLayers || !this.circleLayers.length) return;
        for (var i = 0; i < this.circleLayers.length; i++) {
            this.map.removeLayer(this.circleLayers[i]);
        }
        this.circleLayers = [];
    },

    /** Два клика → setWaypoints в LRM (панель справа с шагами на routeLanguage). */
    handleClickForRouteLrm: function (latlng) {
        if (!this.map || !this.routingControl) return;

        if (!this.routeStart) {
            this.clearRouteInternal();
            this.routeStart = latlng;
            this.routeEnd = null;
            this.addRouteMarker(latlng, "Старт");
            return;
        }

        if (!this.routeEnd) {
            this.routeEnd = latlng;
            this.addRouteMarker(latlng, "Финиш");
            for (var i = 0; i < this.routeMarkers.length; i++) {
                this.map.removeLayer(this.routeMarkers[i]);
            }
            this.routeMarkers = [];
            this.routingControl.setWaypoints([
                L.latLng(this.routeStart.lat, this.routeStart.lng),
                L.latLng(this.routeEnd.lat, this.routeEnd.lng)
            ]);
            this.drawRouteOsrm(this.routeStart.lat, this.routeStart.lng, this.routeEnd.lat, this.routeEnd.lng);
            return;
        }

        this.clearRouteInternal();
        this.routeStart = latlng;
        this.addRouteMarker(latlng, "Старт");
    },

    /** Fallback без LRM: только линия по OSRM. */
    handleClickForRouteLegacy: function (latlng) {
        if (!this.map) return;
        if (!this.routeStart) {
            this.clearRouteInternal();
            this.routeStart = latlng;
            this.routeEnd = null;
            this.addRouteMarker(latlng, "Старт");
            return;
        }
        if (!this.routeEnd) {
            this.routeEnd = latlng;
            this.addRouteMarker(latlng, "Финиш");
            this.drawRouteOsrm(this.routeStart.lat, this.routeStart.lng, this.routeEnd.lat, this.routeEnd.lng);
            return;
        }
        this.clearRouteInternal();
        this.routeStart = latlng;
        this.addRouteMarker(latlng, "Старт");
    },

    addRouteMarker: function (latlng, label) {
        if (!this.map) return;
        var m = L.marker([latlng.lat, latlng.lng]).addTo(this.map).bindPopup(label);
        this.routeMarkers.push(m);
    },

    clearRoute: function () {
        this.clearRouteInternal();
    },

    /**
     * Явная постановка маршрута по множеству точек (2+).
     * points: [{ lat: number, lon: number, title?: string }]
     */
    setRouteWaypoints: function (points) {
        if (!this.map || !points || points.length < 2) return;

        this.clearRouteInternal();

        var latlngs = [];
        for (var i = 0; i < points.length; i++) {
            var lat = parseFloat(points[i].lat);
            var lon = parseFloat(points[i].lon);
            if (isNaN(lat) || isNaN(lon)) continue;
            latlngs.push(L.latLng(lat, lon));
            this.addRouteMarker({ lat: lat, lng: lon }, points[i].title || ("Точка " + (i + 1)));
        }

        if (latlngs.length < 2) return;

        if (this.routingControl) {
            this.routingControl.setWaypoints(latlngs);
        }
        this.drawRouteOsrmMulti(latlngs);
    },

    clearRouteInternal: function () {
        this.routeRequestId++;
        if (this.routeAbortController) {
            try { this.routeAbortController.abort(); } catch (_) { }
            this.routeAbortController = null;
        }
        if (this.routingControl) {
            this.routingControl.setWaypoints([]);
        }
        if (this.routeLayer && this.map) {
            this.map.removeLayer(this.routeLayer);
            this.routeLayer = null;
        }
        if (this.heatLayer && this.map) {
            this.map.removeLayer(this.heatLayer);
            this.heatLayer = null;
        }
        this.clearCircles();
        for (var i = 0; i < this.routeMarkers.length; i++) {
            this.map.removeLayer(this.routeMarkers[i]);
        }
        this.routeMarkers = [];
        this.routeStart = null;
        this.routeEnd = null;
    },

    drawRouteOsrm: function (startLat, startLon, endLat, endLon) {
        this.drawRouteOsrmMulti([
            L.latLng(startLat, startLon),
            L.latLng(endLat, endLon)
        ]);
    },

    drawRouteOsrmMulti: function (latlngs) {
        if (!this.map) return;
        if (!latlngs || latlngs.length < 2) return;
        if (this.routeAbortController) {
            try { this.routeAbortController.abort(); } catch (_) { }
        }
        this.routeAbortController = new AbortController();
        this.routeRequestId++;
        var currentRequestId = this.routeRequestId;
        var base = this.osrmBaseUrl || "https://router.project-osrm.org";
        base = base.replace(/\/$/, "");
        var coords = latlngs.map(function (p) {
            return encodeURIComponent(p.lng) + "," + encodeURIComponent(p.lat);
        }).join(";");
        var url = base + "/route/v1/driving/" + coords + "?overview=full&geometries=geojson&steps=false";

        var self = this;
        fetch(url, { method: "GET", credentials: "omit", signal: this.routeAbortController.signal })
            .then(function (r) {
                if (!r.ok) throw new Error("OSRM HTTP " + r.status);
                return r.json();
            })
            .then(function (data) {
                if (currentRequestId !== self.routeRequestId) return;
                if (!data || data.code !== "Ok" || !data.routes || !data.routes.length) {
                    console.warn("OSRM:", data && data.message ? data.message : data);
                    return;
                }
                var geom = data.routes[0].geometry;
                var coords = geom && geom.coordinates;
                if (!coords || !coords.length) return;

                var latlngs = coords.map(function (c) {
                    return [c[1], c[0]];
                });

                if (self.routeLayer) {
                    self.map.removeLayer(self.routeLayer);
                }
                self.routeLayer = L.polyline(latlngs, {
                    color: "#2563eb",
                    weight: 5,
                    opacity: 0.85
                }).addTo(self.map);
                self.map.fitBounds(self.routeLayer.getBounds().pad(0.15));
            })
            .catch(function (err) {
                if (err && err.name === "AbortError") return;
                console.error("OSRM fetch failed:", err);
            });
    },

    dispose: function () {
        this.clearMarkers();
        if (this.routingControl && this.map) {
            this.map.removeControl(this.routingControl);
            this.routingControl = null;
        }
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
        this.routeRequestId++;
        if (this.routeAbortController) {
            try { this.routeAbortController.abort(); } catch (_) { }
            this.routeAbortController = null;
        }
        this.clearCircles();
        if (this.map) {
            this.map.remove();
            this.map = null;
        }
        this.baseLayers = {};
    }
};
