using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using WeatherApp.Models;

namespace WeatherApp.Services;

public sealed class OpenMeteoWeatherService(HttpClient httpClient) : IWeatherService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<WeatherSnapshot> GetWeatherAsync(
        double latitude,
        double longitude,
        string? location,
        CancellationToken cancellationToken)
    {
        var query = string.Join("&",
            $"latitude={latitude.ToString(CultureInfo.InvariantCulture)}",
            $"longitude={longitude.ToString(CultureInfo.InvariantCulture)}",
            "current=temperature_2m,relative_humidity_2m,apparent_temperature,is_day,precipitation,weather_code,wind_speed_10m",
            "hourly=temperature_2m,precipitation_probability,weather_code",
            "daily=weather_code,temperature_2m_max,temperature_2m_min,sunrise,sunset,precipitation_probability_max",
            "forecast_days=6",
            "timezone=auto");

        using var response = await httpClient.GetAsync(
            $"https://api.open-meteo.com/v1/forecast?{query}",
            cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var forecast = await JsonSerializer.DeserializeAsync<ForecastResponse>(
            stream,
            JsonOptions,
            cancellationToken) ?? throw new JsonException("The forecast response was empty.");

        if (forecast.Current is null || forecast.Hourly is null || forecast.Daily is null)
        {
            throw new JsonException("The forecast response did not contain all required data.");
        }

        var city = string.IsNullOrWhiteSpace(location)
            ? "Your location"
            : location;
        var current = forecast.Current;
        var daily = forecast.Daily;
        var hourly = forecast.Hourly;

        return new WeatherSnapshot(
            city,
            string.Empty,
            forecast.Timezone ?? "Local time",
            current.Time ?? string.Empty,
            new CurrentConditions(
                current.Temperature,
                current.FeelsLike,
                current.Humidity,
                current.WindSpeed,
                current.Precipitation,
                current.WeatherCode,
                current.IsDay == 1,
                DescribeWeather(current.WeatherCode),
                daily.Sunrise?.FirstOrDefault() ?? string.Empty,
                daily.Sunset?.FirstOrDefault() ?? string.Empty),
            hourly.Time
                .Select((time, index) => new HourlyForecast(
                    time,
                    hourly.Temperature.ElementAtOrDefault(index),
                    hourly.PrecipitationProbability.ElementAtOrDefault(index),
                    hourly.WeatherCode.ElementAtOrDefault(index)))
                .ToArray(),
            daily.Time
                .Select((date, index) => new DailyForecast(
                    date,
                    daily.TemperatureMax.ElementAtOrDefault(index),
                    daily.TemperatureMin.ElementAtOrDefault(index),
                    daily.PrecipitationProbabilityMax.ElementAtOrDefault(index),
                    daily.WeatherCode.ElementAtOrDefault(index)))
                .ToArray());
    }

    public async Task<IReadOnlyList<WeatherLocation>> SearchLocationsAsync(
        string city,
        CancellationToken cancellationToken)
    {
        var query = Uri.EscapeDataString(city);
        using var response = await httpClient.GetAsync(
            $"https://geocoding-api.open-meteo.com/v1/search?name={query}&count=6&language=en&format=json",
            cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync<GeocodingResponse>(
            stream,
            JsonOptions,
            cancellationToken) ?? throw new JsonException("The location response was empty.");

        return result.Results?.Select(item => new WeatherLocation(
            item.Name,
            item.Country ?? string.Empty,
            item.Latitude,
            item.Longitude)).ToArray() ?? [];
    }

    private static string DescribeWeather(int code) => code switch
    {
        0 => "Clear sky",
        1 => "Mainly clear",
        2 => "Partly cloudy",
        3 => "Overcast",
        45 or 48 => "Foggy",
        51 or 53 or 55 => "Light drizzle",
        56 or 57 => "Freezing drizzle",
        61 or 63 => "Light rain",
        65 => "Heavy rain",
        66 or 67 => "Freezing rain",
        71 or 73 => "Light snow",
        75 or 77 => "Snow",
        80 or 81 => "Rain showers",
        82 => "Heavy showers",
        85 or 86 => "Snow showers",
        95 => "Thunderstorm",
        96 or 99 => "Thunderstorm with hail",
        _ => "Cloudy"
    };

    private sealed class ForecastResponse
    {
        public string? Timezone { get; init; }
        public CurrentResponse? Current { get; init; }
        public HourlyResponse? Hourly { get; init; }
        public DailyResponse? Daily { get; init; }
    }

    private sealed class CurrentResponse
    {
        [JsonPropertyName("time")]
        public string? Time { get; init; }
        [JsonPropertyName("temperature_2m")]
        public double Temperature { get; init; }
        [JsonPropertyName("relative_humidity_2m")]
        public int Humidity { get; init; }
        [JsonPropertyName("apparent_temperature")]
        public double FeelsLike { get; init; }
        [JsonPropertyName("is_day")]
        public int IsDay { get; init; }
        [JsonPropertyName("precipitation")]
        public double Precipitation { get; init; }
        [JsonPropertyName("weather_code")]
        public int WeatherCode { get; init; }
        [JsonPropertyName("wind_speed_10m")]
        public double WindSpeed { get; init; }
    }

    private sealed class HourlyResponse
    {
        [JsonPropertyName("time")]
        public List<string> Time { get; init; } = [];
        [JsonPropertyName("temperature_2m")]
        public List<double> Temperature { get; init; } = [];
        [JsonPropertyName("precipitation_probability")]
        public List<int> PrecipitationProbability { get; init; } = [];
        [JsonPropertyName("weather_code")]
        public List<int> WeatherCode { get; init; } = [];
    }

    private sealed class DailyResponse
    {
        [JsonPropertyName("time")]
        public List<string> Time { get; init; } = [];
        [JsonPropertyName("weather_code")]
        public List<int> WeatherCode { get; init; } = [];
        [JsonPropertyName("temperature_2m_max")]
        public List<double> TemperatureMax { get; init; } = [];
        [JsonPropertyName("temperature_2m_min")]
        public List<double> TemperatureMin { get; init; } = [];
        [JsonPropertyName("sunrise")]
        public List<string>? Sunrise { get; init; }
        [JsonPropertyName("sunset")]
        public List<string>? Sunset { get; init; }
        [JsonPropertyName("precipitation_probability_max")]
        public List<int> PrecipitationProbabilityMax { get; init; } = [];
    }

    private sealed class GeocodingResponse
    {
        public List<GeocodingResult>? Results { get; init; }
    }

    private sealed class GeocodingResult
    {
        public string Name { get; init; } = string.Empty;
        public string? Country { get; init; }
        public double Latitude { get; init; }
        public double Longitude { get; init; }
    }
}
