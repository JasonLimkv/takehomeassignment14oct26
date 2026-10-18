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
    [JsonPropertyName("area_metadata")]
    public List<WeatherAreaMetadata>? AreaMetadata { get; init; }

    [JsonPropertyName("items")]
    public List<WeatherForecastItem>? Items { get; init; }
}
public sealed class WeatherAreaMetadata
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("label_location")]
    public WeatherLabelLocation? LabelLocation { get; init; }
}

public sealed class WeatherLabelLocation
{
    [JsonPropertyName("latitude")]
    public double? Latitude { get; init; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; init; }
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