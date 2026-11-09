using Microsoft.AspNetCore.Mvc;
using WeatherApp.Services;

namespace WeatherApp.Controllers;

public class HomeController(
    IWeatherService weatherService,
    ILogger<HomeController> logger) : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public IActionResult Privacy() => View();

    [HttpGet("/api/weather")]
    public async Task<IActionResult> GetWeather(
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] string? location,
        CancellationToken cancellationToken)
    {
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            return BadRequest(new { error = "Please provide valid map coordinates." });
        }

        try
        {
            var weather = await weatherService.GetWeatherAsync(
                latitude,
                longitude,
                location,
                cancellationToken);
            return Ok(weather);
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "The weather provider could not return a forecast.");
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = "Weather data is temporarily unavailable. Please try again."
            });
        }
        catch (System.Text.Json.JsonException exception)
        {
            logger.LogError(exception, "The weather provider returned an invalid forecast.");
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = "Weather data could not be read. Please try again."
            });
        }
    }

    [HttpGet("/api/weather/search")]
    public async Task<IActionResult> SearchLocations(
        [FromQuery] string? city,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(city) || city.Trim().Length > 100)
        {
            return BadRequest(new { error = "Enter a city name up to 100 characters long." });
        }

        try
        {
            var locations = await weatherService.SearchLocationsAsync(city.Trim(), cancellationToken);
            return Ok(locations);
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "The location provider could not complete a search.");
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = "Location search is temporarily unavailable. Please try again."
            });
        }
        catch (System.Text.Json.JsonException exception)
        {
            logger.LogError(exception, "The location provider returned an invalid search response.");
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = "Search results could not be read. Please try again."
            });
        }
    }
}
