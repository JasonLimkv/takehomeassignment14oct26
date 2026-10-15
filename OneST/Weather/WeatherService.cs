using System.Net.Http.Json;

namespace OneST.Weather;

public sealed class WeatherService
{
    private const string ForecastUrl =
        "https://api-open.data.gov.sg/v2/real-time/api/two-hr-forecast";

    private readonly HttpClient _httpClient;

    public WeatherService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<WeatherForecastItem>> GetForecastsAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient
            .GetFromJsonAsync<WeatherApiResponse>(
                ForecastUrl, cancellationToken);

        if (response is null || response.Code != 0)
        {
            throw new InvalidDataException(
                "The weather provider returned an unsuccessful response.");
        }

        var forecasts = response.Data?.Items;

        if (forecasts is null || forecasts.Count == 0)
        {
            throw new InvalidDataException(
                "The weather provider returned no forecast data.");
        }

        return forecasts;
    }
}