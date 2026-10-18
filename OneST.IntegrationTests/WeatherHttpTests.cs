using System.Net;
using System.Net.Http.Json;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace OneST.IntegrationTests;

public class WeatherHttpTests
{
    [Fact]
    public async Task GetWeather_ReturnsLocationOptions()
    {
        await using var factory = new WeatherWebApplicationFactory(CreateWeatherResponse);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/Weather");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        using var page = await new HtmlParser().ParseDocumentAsync(await response.Content.ReadAsStringAsync());
        var input = Assert.IsAssignableFrom<IHtmlInputElement>(page.QuerySelector("input[name='location']"));
        Assert.Equal("combobox", input.GetAttribute("role"));
        Assert.Equal("list", input.GetAttribute("aria-autocomplete"));
        Assert.Equal("false", input.GetAttribute("aria-expanded"));
        Assert.Equal("location-options", input.GetAttribute("aria-controls"));
        Assert.Equal("Bedok", page.QuerySelector("#location-options [role='option'][data-value='Bedok']")?.TextContent.Trim());
    }
    [Fact]
    public async Task GetForecast_ReturnsJson_WhenLocationIsValid()
    {
        await using var factory = new WeatherWebApplicationFactory(CreateWeatherResponse);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
        using var response = await client.GetAsync("/Weather?handler=Forecast&location=Bedok");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal("Bedok", root.GetProperty("area").GetString());
        Assert.Equal("Cloudy", root.GetProperty("forecast").GetString());
        var start = root.GetProperty("start").GetDateTimeOffset();
        var end = root.GetProperty("end").GetDateTimeOffset();
        var now = DateTimeOffset.UtcNow;
        Assert.True(start <= now && now < end);
        Assert.Equal(TimeSpan.FromHours(2), end - start);
    }
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Atlantis")]
    public async Task GetForecast_ReturnsBadRequest_WhenLocationIsInvalid(string? location)
    {
        await using var factory = new WeatherWebApplicationFactory(CreateWeatherResponse);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        var url = "/Weather?handler=Forecast";
        if (location is not null)
        {
            url = QueryHelpers.AddQueryString(url, "location", location);
        }

        using var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Choose a location from the list.", json.RootElement.GetProperty("message").GetString());
        Assert.False(json.RootElement.TryGetProperty("forecast", out _));
    }
    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task GetForecast_ReturnsServiceUnavailable_WhenProviderFails(
        HttpStatusCode providerStatus)
    {
        await using var factory = new WeatherWebApplicationFactory(() => new HttpResponseMessage(providerStatus));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/Weather?handler=Forecast&location=Bedok");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Weather is temporarily unavailable. Please try again later.", json.RootElement.GetProperty("message").GetString());
        Assert.False(json.RootElement.TryGetProperty("forecast", out _));
    }
    private static HttpResponseMessage CreateWeatherResponse()
    {
        var now = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8));
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                code = 0,
                data = new
                {
                    items = new[]
                    {
                        new
                        {
                            update_timestamp = now,
                            valid_period = new { start = now.AddMinutes(-5), end = now.AddMinutes(115) },
                            forecasts = new[] { new { area = "Bedok", forecast = "Cloudy" } }
                        }
                    }
                }
            })
        };
    }
}
