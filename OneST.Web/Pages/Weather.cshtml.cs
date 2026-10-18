using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using OneST.Weather;

namespace OneST.Web.Pages;

public class WeatherModel(WeatherService weatherService, ILogger<WeatherModel> logger) : PageModel
{
    public string? Location { get; private set; }
    public WeatherForecastItem? CurrentForecast { get; private set; }
    public IReadOnlyList<AreaForecast> Locations { get; private set; } = [];
    public IReadOnlyList<WeatherAreaMetadata> AreaMetadata { get; private set; } = [];
    public AreaForecast? SelectedForecast { get; private set; }
    public string? ErrorMessage { get; private set; }

    public Task OnGetAsync(string? location, CancellationToken cancellationToken) => LoadForecastAsync(location, cancellationToken);

    public async Task<JsonResult> OnGetForecastAsync(string? location, CancellationToken cancellationToken)
    {
        await LoadForecastAsync(location, cancellationToken);

        if (ErrorMessage is not null)
            return new JsonResult(new { message = ErrorMessage })
                { StatusCode = 503 };

        if (SelectedForecast is null)
            return new JsonResult(new { message = "Choose a location from the list." })
                { StatusCode = 400 };

        return new JsonResult(new
        {
            area = SelectedForecast.Area,
            forecast = SelectedForecast.Forecast,
            start = CurrentForecast?.ValidPeriod?.Start,
            end = CurrentForecast?.ValidPeriod?.End
        });
    }

    private async Task LoadForecastAsync(string? location, CancellationToken cancellationToken)
    {
        Location = location?.Trim();
        try
        {
            var data = await weatherService.GetForecastsAsync(cancellationToken);
            var updates = data.Items ?? [];
            AreaMetadata = data.AreaMetadata ?? [];
            var now = DateTimeOffset.UtcNow;
            CurrentForecast = updates.Where(item => item?.ValidPeriod is { Start: { } start, End: { } end } && start <= now && now < end)
                .OrderByDescending(item => item.UpdatedAt).FirstOrDefault();

            if (CurrentForecast?.Forecasts is not { Count: > 0 } rows)
                throw new InvalidDataException("No current forecast is available.");

            Locations = rows.Where(row => row is not null && !string.IsNullOrWhiteSpace(row.Area) && !string.IsNullOrWhiteSpace(row.Forecast))
                .OrderBy(row => row.Area, StringComparer.OrdinalIgnoreCase).ToArray();

            if (Locations.Count == 0)
                throw new InvalidDataException("No forecast locations are available.");

            if (string.IsNullOrWhiteSpace(Location))
                return;

            SelectedForecast = Locations.FirstOrDefault(row => string.Equals(row.Area, Location, StringComparison.OrdinalIgnoreCase));
            if (SelectedForecast is not null)
                Location = SelectedForecast.Area;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested && (ex is HttpRequestException or JsonException or InvalidDataException or OperationCanceledException))
        {
            logger.LogWarning(ex, "Unable to load weather forecasts.");
            ErrorMessage = "Weather is temporarily unavailable. Please try again later.";
        }
    }
}
