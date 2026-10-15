using System.Text.Json.Serialization;

namespace OneST.Weather;

public sealed class WeatherApiResponse
{
    [JsonPropertyName("code")]
    public int? Code { get; init; }

    [JsonPropertyName("data")]
    public WeatherData? Data { get; init; }
}

public sealed class WeatherData
{
    [JsonPropertyName("items")]
    public List<WeatherForecastItem>? Items { get; init; }
}

public sealed class WeatherForecastItem
{
    [JsonPropertyName("update_timestamp")]
    public DateTimeOffset? UpdatedAt { get; init; }

    [JsonPropertyName("valid_period")]
    public WeatherValidPeriod? ValidPeriod { get; init; }

    [JsonPropertyName("forecasts")]
    public List<AreaForecast>? Forecasts { get; init; }
}

public sealed class WeatherValidPeriod
{
    [JsonPropertyName("start")]
    public DateTimeOffset? Start { get; init; }

    [JsonPropertyName("end")]
    public DateTimeOffset? End { get; init; }
}

public sealed class AreaForecast
{
    [JsonPropertyName("area")]
    public string? Area { get; init; }

    [JsonPropertyName("forecast")]
    public string? Forecast { get; init; }
}