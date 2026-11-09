namespace WeatherApp.Models;

public sealed record WeatherLocation(
    string Name,
    string Country,
    double Latitude,
    double Longitude);

public sealed record WeatherSnapshot(
    string Location,
    string Country,
    string Timezone,
    string UpdatedAt,
    CurrentConditions Current,
    IReadOnlyList<HourlyForecast> Hourly,
    IReadOnlyList<DailyForecast> Daily);

public sealed record CurrentConditions(
    double Temperature,
    double FeelsLike,
    int Humidity,
    double WindSpeed,
    double Precipitation,
    int WeatherCode,
    bool IsDay,
    string Description,
    string Sunrise,
    string Sunset);

public sealed record HourlyForecast(
    string Time,
    double Temperature,
    int PrecipitationProbability,
    int WeatherCode);

public sealed record DailyForecast(
    string Date,
    double High,
    double Low,
    int PrecipitationProbability,
    int WeatherCode);
