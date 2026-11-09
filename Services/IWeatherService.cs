using WeatherApp.Models;

namespace WeatherApp.Services;

public interface IWeatherService
{
    Task<WeatherSnapshot> GetWeatherAsync(
        double latitude,
        double longitude,
        string? location,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WeatherLocation>> SearchLocationsAsync(
        string city,
        CancellationToken cancellationToken);
}
