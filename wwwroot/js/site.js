(() => {
    const defaultLocation = { latitude: 37.7749, longitude: -122.4194, name: "San Francisco", region: "California" };
    const elements = {
        app: document.getElementById("weatherApp"),
        today: document.getElementById("todayLabel"),
        form: document.getElementById("searchForm"),
        search: document.getElementById("citySearch"),
        results: document.getElementById("searchResults"),
        status: document.getElementById("statusMessage"),
        location: document.getElementById("locationName"),
        locationMeta: document.getElementById("locationMeta"),
        currentDate: document.getElementById("currentDate"),
        temperature: document.getElementById("currentTemperature"),
        condition: document.getElementById("currentCondition"),
        feelsLike: document.getElementById("feelsLike"),
        humidity: document.getElementById("humidity"),
        humidityCaption: document.getElementById("humidityCaption"),
        humidityMeter: document.getElementById("humidityMeter"),
        wind: document.getElementById("windSpeed"),
        precipitation: document.getElementById("precipitation"),
        rainCaption: document.getElementById("rainCaption"),
        sunrise: document.getElementById("sunrise"),
        sunset: document.getElementById("sunset"),
        sunSummary: document.getElementById("sunSummary"),
        updated: document.getElementById("updatedAt"),
        art: document.getElementById("weatherArt"),
        hourly: document.getElementById("hourlyForecast"),
        daily: document.getElementById("dailyForecast"),
        locate: document.getElementById("useLocation")
    };

    const weatherSymbol = (code) => {
        if (code === 0) return { symbol: "☀", className: "sunny" };
        if (code === 1 || code === 2) return { symbol: "⛅", className: "sunny" };
        if (code === 3 || code === 45 || code === 48) return { symbol: "☁", className: "cloudy" };
        if (code >= 71 && code <= 77 || code === 85 || code === 86) return { symbol: "❄", className: "rainy" };
        if (code >= 95) return { symbol: "⚡", className: "rainy" };
        if (code >= 51 && code <= 67 || code >= 80 && code <= 82) return { symbol: "☂", className: "rainy" };
        return { symbol: "☁", className: "cloudy" };
    };

    const timeParts = (value) => {
        const time = value?.split("T")[1];
        if (!time) return { hour: 0, label: "--:--", short: "--" };
        const [hour, minute = "00"] = time.split(":");
        const numericHour = Number(hour);
        return {
            hour: numericHour,
            label: new Date(2000, 0, 1, numericHour, Number(minute))
                .toLocaleTimeString([], { hour: "numeric", minute: "2-digit" }),
            short: new Date(2000, 0, 1, numericHour)
                .toLocaleTimeString([], { hour: "numeric" })
        };
    };

    const formatDay = (value, index) => {
        if (index === 0) return "Today";
        if (index === 1) return "Tomorrow";
        const [year, month, day] = value.split("-").map(Number);
        return new Date(Date.UTC(year, month - 1, day)).toLocaleDateString([], {
            weekday: "short",
            timeZone: "UTC"
        });
    };

    const showError = (message) => {
        elements.status.textContent = message;
        elements.status.hidden = false;
    };

    const clearError = () => {
        elements.status.hidden = true;
        elements.status.textContent = "";
    };

    const setLoading = (isLoading) => {
        elements.app.classList.toggle("is-loading", isLoading);
        elements.locate.disabled = isLoading;
    };

    const setTimes = (data) => {
        const sunrise = timeParts(data.current.sunrise).label;
        const sunset = timeParts(data.current.sunset).label;
        elements.sunrise.textContent = sunrise;
        elements.sunset.textContent = sunset;
        elements.sunSummary.textContent = `Sunrise ${sunrise} · Sunset ${sunset}`;
    };

    const renderHourly = (forecast, currentTime) => {
        const upcoming = forecast.filter((item) => item.time >= currentTime);
        const slots = upcoming.filter((_, index) => index % 3 === 0).slice(0, 8);
        elements.hourly.replaceChildren();

        for (const item of slots) {
            const time = timeParts(item.time);
            const weather = weatherSymbol(item.weatherCode);
            const column = document.createElement("div");
            column.className = "hourly-item";
            column.innerHTML = `<span class="hourly-time">${time.short}</span>
                <span class="hourly-icon ${weather.className}" aria-label="${weather.className}">${weather.symbol}</span>
                <span class="hourly-temp">${Math.round(item.temperature)}°</span>
                <span class="hourly-rain">${Math.round(item.precipitationProbability)}% rain</span>`;
            elements.hourly.append(column);
        }
    };

    const renderDaily = (forecast) => {
        elements.daily.replaceChildren();
        forecast.forEach((item, index) => {
            const weather = weatherSymbol(item.weatherCode);
            const row = document.createElement("div");
            row.className = "daily-item";
            row.innerHTML = `<span class="daily-day">${formatDay(item.date, index)}</span>
                <span class="daily-icon ${weather.className}" aria-label="${weather.className}">${weather.symbol}</span>
                <span class="daily-temps"><strong>${Math.round(item.high)}°</strong><span>${Math.round(item.low)}°</span></span>
                <span class="daily-rain">${Math.round(item.precipitationProbability)}%</span>`;
            elements.daily.append(row);
        });
    };

    const renderWeather = (data) => {
        const current = data.current;
        const now = new Date();
        elements.location.textContent = data.location;
        elements.locationMeta.textContent = [data.country, data.timezone].filter(Boolean).join(" · ");
        elements.currentDate.textContent = now.toLocaleDateString([], {
            weekday: "long",
            month: "long",
            day: "numeric"
        }).toUpperCase();
        elements.temperature.textContent = Math.round(current.temperature);
        elements.condition.textContent = current.description;
        elements.feelsLike.textContent = `${Math.round(current.feelsLike)}°`;
        elements.humidity.textContent = current.humidity;
        elements.humidityMeter.style.width = `${Math.max(0, Math.min(100, current.humidity))}%`;
        elements.humidityCaption.textContent = current.humidity >= 70
            ? "A little humid today"
            : current.humidity <= 30 ? "Dry, comfortable air" : "Comfortable air";
        elements.wind.textContent = Math.round(current.windSpeed);
        elements.precipitation.textContent = Number(current.precipitation).toFixed(1);
        elements.rainCaption.textContent = current.precipitation > 0 ? "Rainfall so far today" : "No rain so far today";
        elements.updated.textContent = `Updated ${timeParts(data.updatedAt).label}`;
        elements.art.classList.toggle("is-night", !current.isDay);
        elements.art.classList.toggle("is-rainy", current.weatherCode >= 51);
        elements.art.classList.toggle("is-cloudy", current.weatherCode > 1);
        elements.today.textContent = now.toLocaleDateString([], { weekday: "long", month: "short", day: "numeric" });
        setTimes(data);
        renderHourly(data.hourly, data.updatedAt);
        renderDaily(data.daily);
    };

    const loadWeather = async (location) => {
        clearError();
        setLoading(true);
        try {
            const query = new URLSearchParams({
                latitude: location.latitude,
                longitude: location.longitude,
                location: location.name
            });
            const response = await fetch(`/api/weather?${query}`);
            const data = await response.json();
            if (!response.ok) throw new Error(data.error || "Weather data is temporarily unavailable.");
            renderWeather(data);
        } catch (error) {
            showError(error instanceof Error ? error.message : "Weather data is temporarily unavailable.");
        } finally {
            setLoading(false);
        }
    };

    const closeResults = () => {
        elements.results.hidden = true;
        elements.results.replaceChildren();
    };

    const searchCities = async (city) => {
        clearError();
        closeResults();
        const submit = elements.form.querySelector(".search-submit");
        submit.disabled = true;
        try {
            const response = await fetch(`/api/weather/search?city=${encodeURIComponent(city)}`);
            const locations = await response.json();
            if (!response.ok) throw new Error(locations.error || "Location search is temporarily unavailable.");
            if (locations.length === 0) {
                const empty = document.createElement("p");
                empty.className = "search-empty";
                empty.textContent = "No cities found. Try a different search.";
                elements.results.append(empty);
                elements.results.hidden = false;
                return;
            }

            for (const location of locations) {
                const button = document.createElement("button");
                button.type = "button";
                button.className = "search-result";
                button.setAttribute("role", "option");
                const name = document.createElement("strong");
                name.textContent = location.name;
                const detail = document.createElement("span");
                detail.textContent = location.country || "Location";
                button.append(name, detail);
                button.addEventListener("click", () => {
                    elements.search.value = "";
                    closeResults();
                    loadWeather({
                        latitude: location.latitude,
                        longitude: location.longitude,
                        name: location.country ? `${location.name}, ${location.country}` : location.name
                    });
                });
                elements.results.append(button);
            }
            elements.results.hidden = false;
        } catch (error) {
            showError(error instanceof Error ? error.message : "Location search is temporarily unavailable.");
        } finally {
            submit.disabled = false;
        }
    };

    elements.form.addEventListener("submit", (event) => {
        event.preventDefault();
        const city = elements.search.value.trim();
        if (city) searchCities(city);
    });

    elements.search.addEventListener("keydown", (event) => {
        if (event.key === "Escape") closeResults();
    });

    document.addEventListener("click", (event) => {
        if (!elements.results.contains(event.target) && !elements.form.contains(event.target)) closeResults();
    });

    elements.locate.addEventListener("click", () => {
        clearError();
        if (!navigator.geolocation) {
            showError("Your browser does not support location sharing. Search for a city instead.");
            return;
        }

        elements.locate.disabled = true;
        navigator.geolocation.getCurrentPosition(
            (position) => loadWeather({
                latitude: position.coords.latitude,
                longitude: position.coords.longitude,
                name: "Your location"
            }),
            (error) => {
                elements.locate.disabled = false;
                const message = error.code === error.PERMISSION_DENIED
                    ? "Location access was denied. You can search for a city instead."
                    : "Your location could not be determined. Please try again or search for a city.";
                showError(message);
            },
            { enableHighAccuracy: false, timeout: 10000, maximumAge: 300000 }
        );
    });

    loadWeather(defaultLocation);
})();
