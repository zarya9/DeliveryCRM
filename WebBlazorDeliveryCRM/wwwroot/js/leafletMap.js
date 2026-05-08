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
    osrmBaseUrl: "",
    routeLanguage: "ru",
    enableRouting: false,
    routeAbortController: null,
    routeRequestId: 0,

    init: function (elementId, centerLat, centerLon, zoom, theme, osrmBaseUrl, enableRouting, routeLanguage) {
        if (this.map) {
            this.dispose();
        }
        var el = document.getElementById(elementId);
        if (!el) return;
        centerLat = centerLat || 55.8024;
        centerLon = centerLon || 49.1167;
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

        var osmAttribution = '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener noreferrer">OpenStreetMap</a>';
        this.baseLayers = {
            light: L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
                attribution: osmAttribution
            }),
            dark: L.tileLayer("https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png", {
                attribution: osmAttribution
            }),
            contrast: L.tileLayer("https://{s}.tile.openstreetmap.fr/hot/{z}/{x}/{y}.png", {
                attribution: osmAttribution
            })
        };

        var layer = this.baseLayers[this.currentTheme] || this.baseLayers.light;
        layer.addTo(this.map);

        var attr = L.control.attribution({ position: "bottomright" }).addTo(this.map);
        attr.setPrefix("");
        attr.addAttribution(osmAttribution);

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
            var kind = items[i].kind || "";
            var markerOpts = {};
            if (kind === "courier" || kind === "hub") {
                var courierOnline = !!items[i].online;
                var courierStroke = courierOnline ? "#16a34a" : "#6b7280";
                var courierBg = courierOnline ? "#ecfdf3" : "#f3f4f6";
                var svg =
                    kind === "hub"
                        ? '<svg viewBox="0 0 24 24" width="22" height="22" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true"><path d="M3.50002 10V15C3.50002 17.8284 3.50002 19.2426 4.37869 20.1213C5.25737 21 6.67159 21 9.50002 21H14.5C17.3284 21 18.7427 21 19.6213 20.1213C20.5 19.2426 20.5 17.8284 20.5 15V10M17 7.50184C17 8.88255 15.8807 9.99997 14.5 9.99997C13.1193 9.99997 12 8.88068 12 7.49997C12 8.88068 10.8807 9.99997 9.50002 9.99997C8.1193 9.99997 7.00002 8.88068 7.00002 7.49997C7.00002 8.88068 5.82655 9.99997 4.37901 9.99997C3.59984 9.99997 2.90008 9.67567 2.42 9.16087C1.59462 8.2758 2.12561 6.97403 2.81448 5.98842L3.20202 5.45851C4.08386 4.2527 4.52478 3.6498 5.16493 3.32494C5.80508 3.00008 6.55201 3.00018 8.04587 3.00038L15.9551 3.00143C17.4485 3.00163 18.1952 3.00173 18.8351 3.32658C19.475 3.65143 19.9158 4.25414 20.7974 5.45957L21.1855 5.99029C21.8744 6.97589 22.4054 8.27766 21.58 9.16273C21.0999 9.67754 20.4002 10.0018 19.621 10.0018C18.1734 10.0018 17 8.88255 17 7.50184Z" stroke="#0f766e" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/></svg>'
                        : '<svg viewBox="0 0 24 24" width="22" height="22" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true"><path d="M2.5 12L4.5 13M21.5 12.5L19.5 13M8 17.5L8.24567 16.8858C8.61101 15.9725 8.79368 15.5158 9.17461 15.2579C9.55553 15 10.0474 15 11.0311 15H12.9689C13.9526 15 14.4445 15 14.8254 15.2579C15.2063 15.5158 15.389 15.9725 15.7543 16.8858L16 17.5M2 17V19.882C2 20.2607 2.24075 20.607 2.62188 20.7764C2.86918 20.8863 3.10538 21 3.39058 21H5.10942C5.39462 21 5.63082 20.8863 5.87812 20.7764C6.25925 20.607 6.5 20.2607 6.5 19.882V18M17.5 18V19.882C17.5 20.2607 17.7408 20.607 18.1219 20.7764C18.3692 20.8863 18.6054 21 18.8906 21H20.6094C20.8946 21 21.1308 20.8863 21.3781 20.7764C21.7592 20.607 22 20.2607 22 19.882V17M20 8.5L21 8M4 8.5L3 8M4.5 9L5.5883 5.73509C6.02832 4.41505 6.24832 3.75503 6.7721 3.37752C7.29587 3 7.99159 3 9.38304 3H14.617C16.0084 3 16.7041 3 17.2279 3.37752C17.7517 3.75503 17.9717 4.41505 18.4117 5.73509L19.5 9M4.5 9H19.5C20.4572 10.0135 22 11.4249 22 12.9996V16.4702C22 17.0407 21.6205 17.5208 21.1168 17.5875L18 18H6L2.88316 17.5875C2.37955 17.5208 2 17.0407 2 16.4702V12.9996C2 11.4249 3.54279 10.0135 4.5 9Z" stroke="' + courierStroke + '" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/></svg>';
                var wrap =
                    '<div class="lm-pin-wrap" style="width:32px;height:32px;display:flex;align-items:center;justify-content:center;background:' + (kind === "courier" ? courierBg : "#fff") + ';border-radius:8px;box-shadow:0 1px 4px rgba(0,0,0,.25);border:1px solid rgba(0,0,0,.08)">' +
                    svg +
                    "</div>";
                markerOpts.icon = L.divIcon({
                    html: wrap,
                    className: "lm-div-marker",
                    iconSize: [32, 32],
                    iconAnchor: [16, 16]
                });
            }
            var marker = L.marker([lat, lon], markerOpts).addTo(this.map).bindPopup(title);
            this.markers.push(marker);
        }
        if (this.markers.length > 1) {
            var group = new L.featureGroup(this.markers);
            this.map.fitBounds(group.getBounds().pad(0.12), { maxZoom: 14 });
        } else if (this.markers.length === 1) {
            this.map.setView(this.markers[0].getLatLng(), Math.max(this.map.getZoom(), 12));
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
                self.map.fitBounds(self.routeLayer.getBounds().pad(0.15), { maxZoom: 14 });
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
