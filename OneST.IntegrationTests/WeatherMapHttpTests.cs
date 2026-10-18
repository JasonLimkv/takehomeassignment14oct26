using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Mvc.Testing;

namespace OneST.IntegrationTests;

public class WeatherMapHttpTests
{
    [Fact]
    public async Task GetWeather_EmbedsForecastsAndCoordinates_WithLocalAssets()
    {
        await using var factory = new WeatherWebApplicationFactory(() => CreateResponse());
        using var client = CreateClient(factory);
        using var response = await client.GetAsync("/Weather");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var page = await new HtmlParser().ParseDocumentAsync(await response.Content.ReadAsStringAsync());
        var app = page.QuerySelector("#weather-app");
        Assert.NotNull(app);
        using var data = JsonDocument.Parse(app.GetAttribute("data-weather")!);
        var root = data.RootElement;
        var forecast = Assert.Single(root.GetProperty("forecasts").EnumerateArray());
        Assert.Equal("Bedok", forecast.GetProperty("area").GetString());
        Assert.Equal("Cloudy", forecast.GetProperty("forecast").GetString());
        var metadata = Assert.Single(root.GetProperty("metadata").EnumerateArray());
        Assert.Equal("Bedok", metadata.GetProperty("name").GetString());
        var coordinates = metadata.GetProperty("label_location");
        Assert.Equal(1.321, coordinates.GetProperty("latitude").GetDouble());
        Assert.Equal(103.924, coordinates.GetProperty("longitude").GetDouble());
        Assert.True(root.GetProperty("start").GetDateTimeOffset() <= DateTimeOffset.UtcNow);
        Assert.True(root.GetProperty("end").GetDateTimeOffset() > DateTimeOffset.UtcNow);
        Assert.NotNull(page.QuerySelector("img#singapore-map[src^='/images/singapore']"));
        Assert.Single(page.QuerySelectorAll("script[src^='/lib/d3/d3']"));
        Assert.Single(page.QuerySelectorAll("script[src^='/js/weather']"));
        Assert.Empty(page.QuerySelectorAll("script:not([src])"));
    }

    [Fact]
    public async Task GetWeather_EncodesProviderText_InMapData()
    {
        const string payload = "\"><img id='weather-injection' src=x onerror=alert(1)>";
        await using var factory = new WeatherWebApplicationFactory(() => CreateResponse(payload));
        using var client = CreateClient(factory);
        using var response = await client.GetAsync("/Weather");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var page = await new HtmlParser().ParseDocumentAsync(await response.Content.ReadAsStringAsync());
        Assert.Null(page.QuerySelector("#weather-injection"));
        Assert.Empty(page.QuerySelectorAll("[onerror]"));
        using var data = JsonDocument.Parse(page.QuerySelector("#weather-app")!.GetAttribute("data-weather")!);
        var forecast = Assert.Single(data.RootElement.GetProperty("forecasts").EnumerateArray());
        Assert.Equal(payload, forecast.GetProperty("area").GetString());
        Assert.Contains(page.QuerySelectorAll("#location-options [role='option']"), option => option.TextContent == payload && option.GetAttribute("data-value") == payload);
    }

    [Fact]
    public async Task GetWeather_KeepsForecasts_WhenCoordinatesAreMissing()
    {
        await using var factory = new WeatherWebApplicationFactory(() => CreateResponse(includeMetadata: false));
        using var client = CreateClient(factory);
        using var response = await client.GetAsync("/Weather");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var page = await new HtmlParser().ParseDocumentAsync(await response.Content.ReadAsStringAsync());
        Assert.NotNull(page.QuerySelector("#location-options [role='option'][data-value='Bedok']"));
        using var data = JsonDocument.Parse(page.QuerySelector("#weather-app")!.GetAttribute("data-weather")!);
        Assert.Empty(data.RootElement.GetProperty("metadata").EnumerateArray());
        Assert.Single(data.RootElement.GetProperty("forecasts").EnumerateArray());
    }

    [Fact]
    public async Task GetWeather_KeepsLocalMap_WhenProviderFails()
    {
        await using var factory = new WeatherWebApplicationFactory(() => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        using var client = CreateClient(factory);
        using var response = await client.GetAsync("/Weather");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var page = await new HtmlParser().ParseDocumentAsync(await response.Content.ReadAsStringAsync());
        Assert.NotNull(page.QuerySelector("#singapore-map"));
        Assert.Null(page.QuerySelector("#weather-form"));
        Assert.Equal("Weather is temporarily unavailable. Please try again later.", page.QuerySelector("[role='alert']")?.TextContent.Trim());
    }

    private static HttpClient CreateClient(WeatherWebApplicationFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

    private static HttpResponseMessage CreateResponse(string area = "Bedok", bool includeMetadata = true)
    {
        var now = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8));
        var data = new Dictionary<string, object>
        {
            ["items"] = new[]
            {
                new
                {
                    update_timestamp = now,
                    valid_period = new { start = now.AddMinutes(-5), end = now.AddMinutes(115) },
                    forecasts = new[] { new { area, forecast = "Cloudy" } }
                }
            }
        };
        if (includeMetadata)
            data["area_metadata"] = new[]
            {
                new { name = area, label_location = new { latitude = 1.321, longitude = 103.924 } }
            };
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { code = 0, data })
        };
    }
}
