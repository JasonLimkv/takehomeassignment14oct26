(() => {
    "use strict";

    const app = document.getElementById("weather-app");
    const form = document.getElementById("weather-form");
    if (!app || !form) return;

    const locationInput = document.getElementById("Location");
    const search = document.getElementById("location-search");
    const popup = document.getElementById("location-popup");
    const optionList = document.getElementById("location-options");
    const options = Array.from(optionList.querySelectorAll('[role="option"]'));
    const noResults = document.getElementById("location-no-results");
    const searchStatus = document.getElementById("location-search-status");
    const refreshButton = form.querySelector('button[type="submit"]');
    const status = document.getElementById("weather-status");
    const result = document.getElementById("weather-result");
    const empty = document.getElementById("weather-empty");
    const heading = document.getElementById("forecast-heading");
    const description = document.getElementById("forecast-description");
    const period = document.getElementById("forecast-period");
    const map = document.getElementById("weather-map");
    const mapImage = document.getElementById("singapore-map");
    const markerLayer = document.getElementById("weather-markers");
    const mapCount = document.getElementById("weather-map-count");
    const mapStatus = document.getElementById("weather-map-status");
    const timeFormat = new Intl.DateTimeFormat("en-SG", {timeZone: "Asia/Singapore", dateStyle: "medium", timeStyle: "short"});
    const forecasts = new Map();
    const markers = [];
    const key = value => typeof value === "string" ? value.trim().toLowerCase() : "";
    let activeRequest = null;
    let expiryTimer;
    let mapFailed = false;
    let selectedLocation = "";
    let matches = [];
    let activeIndex = -1;

    function hasCommittedLocation() {
        return selectedLocation !== "" && key(locationInput.value) === key(selectedLocation);
    }

    function closeSuggestions(restoreSelection = false) {
        popup.hidden = true;
        locationInput.setAttribute("aria-expanded", "false");
        locationInput.removeAttribute("aria-activedescendant");
        activeIndex = -1;
        searchStatus.textContent = "";
        options.forEach(option => option.setAttribute("aria-selected", "false"));
        if (restoreSelection) locationInput.value = selectedLocation;
        refreshButton.disabled = activeRequest !== null || !hasCommittedLocation();
    }

    function openSuggestions(query = "") {
        const term = key(query);
        matches = options.filter(option => key(option.dataset.value).includes(term));
        const visible = new Set(matches);
        options.forEach(option => {
            option.hidden = !visible.has(option);
            option.setAttribute("aria-selected", "false");
        });
        activeIndex = -1;
        locationInput.removeAttribute("aria-activedescendant");
        noResults.hidden = matches.length > 0;
        popup.hidden = false;
        popup.scrollTop = 0;
        locationInput.setAttribute("aria-expanded", "true");
        searchStatus.textContent = matches.length === 0 ? "No matching locations." : `${matches.length} matching ${matches.length === 1 ? "location" : "locations"}.`;
    }

    function highlightOption(index) {
        if (!matches.length) return;
        activeIndex = Math.max(0, Math.min(index, matches.length - 1));
        options.forEach(option => option.setAttribute("aria-selected", "false"));
        const option = matches[activeIndex];
        option.setAttribute("aria-selected", "true");
        locationInput.setAttribute("aria-activedescendant", option.id);
        option.scrollIntoView({ block: "nearest" });
    }

    function chooseLocation(area) {
        const name = optionNames.get(key(area));
        if (!name) return;
        selectedLocation = name;
        locationInput.value = name;
        closeSuggestions();
        selectLocation();
    }

    function readForecast(data) {
        if (typeof data?.area !== "string" || !data.area.trim() || typeof data?.forecast !== "string" || !data.forecast.trim() || typeof data?.start !== "string" || typeof data?.end !== "string") return null;
        const start = new Date(data.start);
        const end = new Date(data.end);
        if (!Number.isFinite(start.getTime()) || !Number.isFinite(end.getTime()) || end <= start) return null;
        return { area: data.area, forecast: data.forecast, start, end };
    }

    function isCurrent(forecast) {
        const now = Date.now();
        return forecast && forecast.start.getTime() <= now && now < forecast.end.getTime();
    }

    function setStatus(message, isError = false) {
        status.textContent = message;
        status.classList.toggle("weather-status-error", isError);
    }

    function cancelRequest() {
        activeRequest?.abort();
        activeRequest = null;
        form.removeAttribute("aria-busy");
        refreshButton.disabled = !hasCommittedLocation();
    }

    function showUnavailable(message) {
        clearTimeout(expiryTimer);
        result.hidden = true;
        empty.hidden = false;
        empty.textContent = message;
    }

    function showForecast(forecast) {
        clearTimeout(expiryTimer);
        heading.textContent = forecast.area;
        description.textContent = forecast.forecast;
        period.textContent = `${timeFormat.format(forecast.start)} - ${timeFormat.format(forecast.end)}`;
        empty.hidden = true;
        result.hidden = false;
        expiryTimer = setTimeout(() => {
            showUnavailable("This forecast has expired.");
            setStatus("Refresh to get the latest forecast.");
        }, Math.min(forecast.end.getTime() - Date.now(), 2147483647));
    }

    function updateUrl(area) {
        const url = new URL(form.action, window.location.href);
        if (area) url.searchParams.set("location", area);
        else url.searchParams.delete("location");
        window.history.replaceState(null, "", url);
    }

    function layoutMarkers() {
        if (mapFailed || !markers.length) return;
        const { width, height } = map.getBoundingClientRect();
        if (!width || !height) return;
        const selectedKey = key(selectedLocation);
        const focused = document.activeElement;
        const priority = marker => marker.button === focused ? 2 : marker.key === selectedKey ? 1 : 0;
        const ordered = [...markers].sort((a, b) => priority(b) - priority(a));
        const visible = [];
        for (const marker of ordered) {
            const x = marker.x * width;
            const y = marker.y * height;
            const overlaps = visible.some(point => Math.abs(point.x - x) < 32 && Math.abs(point.y - y) < 32);
            marker.button.hidden = overlaps;
            marker.button.setAttribute("aria-pressed", String(marker.key === selectedKey));
            if (!overlaps) visible.push({ x, y });
        }
        mapCount.textContent = visible.length === markers.length ? `${markers.length} forecast locations` : `${visible.length} of ${markers.length} map markers`;
    }

    function selectLocation() {
        cancelRequest();
        clearTimeout(expiryTimer);
        layoutMarkers();
        const forecast = forecasts.get(key(selectedLocation));
        updateUrl(selectedLocation);
        if (!selectedLocation) {
            showUnavailable("Choose a location.");
            setStatus("");
        } else if (isCurrent(forecast)) {
            showForecast(forecast);
            setStatus(`Forecast for ${forecast.area}.`);
        } else {
            void refreshForecast();
        }
    }

    async function refreshForecast() {
        if (!hasCommittedLocation()) return;
        cancelRequest();
        const requestedKey = key(selectedLocation);
        const controller = new AbortController();
        activeRequest = controller;
        const timeout = setTimeout(() => controller.abort(), 15000);
        refreshButton.disabled = true;
        form.setAttribute("aria-busy", "true");
        showUnavailable("Loading forecast...");
        setStatus("Loading forecast...");

        try {
            const url = new URL(form.dataset.forecastUrl, window.location.href);
            url.searchParams.set("location", selectedLocation);
            const response = await fetch(url, {
                headers: { "Accept": "application/json" },
                cache: "no-store", signal: controller.signal
            });
            const data = await response.json();
            if (activeRequest !== controller) return;
            if (!response.ok) {
                showUnavailable("Forecast unavailable.");
                setStatus(typeof data?.message === "string" ? data.message : "Could not load the forecast. Please try again.", true);
                return;
            }
            const forecast = readForecast(data);
            if (!isCurrent(forecast) || key(forecast.area) !== requestedKey) throw new Error("Invalid forecast response.");
            forecasts.set(requestedKey, forecast);
            showForecast(forecast);
            setStatus(`Forecast updated for ${forecast.area}.`);
        } catch {
            if (activeRequest !== controller) return;
            showUnavailable("Forecast unavailable.");
            setStatus("Could not load the forecast. Please try again.", true);
        } finally {
            clearTimeout(timeout);
            if (activeRequest === controller) {
                activeRequest = null;
                refreshButton.disabled = !hasCommittedLocation();
                form.removeAttribute("aria-busy");
            }
        }
    }

    function createMarkers(metadata) {
        if (typeof window.d3?.geoMercator !== "function") {
            mapStatus.textContent = "Map markers are unavailable. Forecasts are available in the location list.";
            return;
        }
        const project = d3.geoMercator().scale(104621.76680355378).translate([-189124.2660349435, 2705.921453828157]);
        const used = new Set();
        for (const area of metadata) {
            const areaKey = key(area?.name);
            const forecast = forecasts.get(areaKey);
            const latitude = area?.label_location?.latitude;
            const longitude = area?.label_location?.longitude;
            if (!forecast || used.has(areaKey) || !Number.isFinite(latitude) || !Number.isFinite(longitude)
                || latitude <= -90 || latitude >= 90 || longitude < -180 || longitude > 180) continue;
            const point = project([longitude, latitude]);
            if (!point || !point.every(Number.isFinite)) continue;
            const [x, y] = point;
            if (x < 16 || x > 984 || y < 16 || y > 604) continue;
            const button = document.createElement("button");
            button.type = "button";
            button.className = "weather-marker";
            button.dataset.area = forecast.area;
            button.title = forecast.area;
            button.setAttribute("aria-label", `Show forecast for ${forecast.area}`);
            button.setAttribute("aria-pressed", "false");
            button.style.left = `${x / 10}%`;
            button.style.top = `${y / 6.2}%`;
            button.addEventListener("click", () => chooseLocation(forecast.area));
            markerLayer.append(button);
            markers.push({ key: areaKey, x: x / 1000, y: y / 620, button });
            used.add(areaKey);
        }
        if (!markers.length) mapStatus.textContent = "Map coordinates are unavailable. Forecasts are available in the location list.";
        else if (markers.length < forecasts.size) mapStatus.textContent = "Some locations have no map coordinates.";
        new ResizeObserver(layoutMarkers).observe(map);
        layoutMarkers();
    }

    let data;
    try { data = JSON.parse(app.dataset.weather); }
    catch {
        mapStatus.textContent = "Map data is unavailable.";
        data = {};
    }
    const optionNames = new Map(options.map(option => [key(option.dataset.value), option.dataset.value]));
    for (const row of Array.isArray(data.forecasts) ? data.forecasts : []) {
        const forecast = readForecast({ ...row, start: data.start, end: data.end });
        if (forecast && optionNames.has(key(forecast.area))) {
            forecast.area = optionNames.get(key(forecast.area));
            forecasts.set(key(forecast.area), forecast);
        }
    }

    function mapImageFailed() {
        mapFailed = true;
        markerLayer.hidden = true;
        mapCount.textContent = "";
        mapStatus.textContent = "The map could not be loaded. Forecasts are available in the location list.";
    }
    mapImage.addEventListener("error", mapImageFailed);
    if (mapImage.complete && mapImage.naturalWidth === 0) mapImageFailed();
    createMarkers(Array.isArray(data.metadata) ? data.metadata : []);
    form.addEventListener("submit", event => {
        event.preventDefault();
        if (hasCommittedLocation()) void refreshForecast();
        else openSuggestions(locationInput.value);
    });
    locationInput.addEventListener("focus", () => {
        openSuggestions();
        locationInput.select();
    });
    locationInput.addEventListener("click", () => {
        if (popup.hidden) openSuggestions();
    });
    locationInput.addEventListener("input", () => {
        const wasRefreshing = activeRequest !== null;
        cancelRequest();
        if (wasRefreshing) {
            const forecast = forecasts.get(key(selectedLocation));
            if (isCurrent(forecast)) {
                showForecast(forecast);
                setStatus(`Forecast for ${forecast.area}.`);
            } else {
                showUnavailable("Forecast unavailable.");
                setStatus("Choose a location or refresh the forecast.");
            }
        }
        openSuggestions(locationInput.value);
    });
    locationInput.addEventListener("keydown", event => {
        if (event.isComposing || event.ctrlKey || event.metaKey) return;
        if (event.key === "ArrowDown" || event.key === "ArrowUp") {
            event.preventDefault();
            if (popup.hidden) openSuggestions(hasCommittedLocation() ? "" : locationInput.value);
            highlightOption(activeIndex < 0 ? (event.key === "ArrowDown" ? 0 : matches.length - 1) : activeIndex + (event.key === "ArrowDown" ? 1 : -1));
        } else if (event.key === "Enter") {
            event.preventDefault();
            if (popup.hidden) openSuggestions(locationInput.value);
            const option = matches[activeIndex] || matches.find(item => key(item.dataset.value) === key(locationInput.value)) || (matches.length === 1 ? matches[0] : null);
            if (option) chooseLocation(option.dataset.value);
        } else if (event.key === "Escape") {
            event.preventDefault();
            closeSuggestions(true);
        } else if (event.key === "Tab") {
            closeSuggestions(true);
        }
    });
    optionList.addEventListener("mousedown", event => {
        if (event.button === 0) event.preventDefault();
    });
    optionList.addEventListener("click", event => {
        const option = event.target.closest('[role="option"]');
        if (!option || option.hidden || !optionList.contains(option)) return;
        locationInput.focus({ preventScroll: true });
        chooseLocation(option.dataset.value);
    });
    search.addEventListener("focusout", event => {
        if (!search.contains(event.relatedTarget)) closeSuggestions(true);
    });
    document.addEventListener("pointerdown", event => {
        if (!search.contains(event.target)) closeSuggestions(true);
    });
    document.addEventListener("visibilitychange", () => {
        if (!document.hidden && !result.hidden && !isCurrent(forecasts.get(key(selectedLocation)))) {
            showUnavailable("This forecast has expired.");
            setStatus("Refresh to get the latest forecast.");
        }
    });
    chooseLocation(optionNames.get(key(locationInput.value)) || optionNames.get("city") || optionNames.values().next().value || "");
})();
