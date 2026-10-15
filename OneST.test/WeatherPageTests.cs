using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using OneST.Weather;
using OneST.Web.Pages;
using Xunit;

namespace OneST.Tests;

public class WeatherPageTests
{
    [Theory]
    [InlineData("Bedok")]
    [InlineData(" bedok ")]
    public async Task OnGetForecastAsync_ReturnsForecast_WhenLocationIsValid(string location)
    {
        var now = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8));
        var start = now.AddHours(-1);
        var end = now.AddHours(1);
        using var client = new HttpClient(new StubHandler(CreateForecastJson(start, end)));
        var page = CreatePage(client);

        var result = await page.OnGetForecastAsync(location, CancellationToken.None);

        Assert.Equal(200, result.StatusCode ?? 200);
        var body = JsonSerializer.SerializeToElement(result.Value);
        Assert.Equal("Bedok", body.GetProperty("area").GetString());
        Assert.Equal("Cloudy", body.GetProperty("forecast").GetString());
        Assert.Equal(start, body.GetProperty("start").GetDateTimeOffset());
        Assert.Equal(end, body.GetProperty("end").GetDateTimeOffset());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Unknown")]
    public async Task OnGetForecastAsync_Returns400_WhenLocationIsInvalid(string? location)
    {
        var now = DateTimeOffset.UtcNow;
        using var client = new HttpClient(new StubHandler(
            CreateForecastJson(now.AddHours(-1), now.AddHours(1))));
        var page = CreatePage(client);

        var result = await page.OnGetForecastAsync(location, CancellationToken.None);

        Assert.Equal(400, result.StatusCode);
        var body = JsonSerializer.SerializeToElement(result.Value);
        Assert.Equal("Choose a location from the list.", body.GetProperty("message").GetString());
        Assert.False(body.TryGetProperty("forecast", out _));
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task OnGetForecastAsync_Returns503_WhenProviderRequestFails(HttpStatusCode statusCode)
    {
        using var client = new HttpClient(new StubHandler("{}", statusCode));
        var page = CreatePage(client);

        var result = await page.OnGetForecastAsync("Bedok", CancellationToken.None);

        Assert.Equal(503, result.StatusCode);
        var body = JsonSerializer.SerializeToElement(result.Value);
        Assert.Equal("Weather is temporarily unavailable. Please try again later.",
            body.GetProperty("message").GetString());
        Assert.False(body.TryGetProperty("forecast", out _));
    }

    [Theory]
    [InlineData(-3)]
    [InlineData(1)]
    public async Task OnGetForecastAsync_Returns503_WhenForecastIsNotCurrent(int startOffsetHours)
    {
        var start = DateTimeOffset.UtcNow.AddHours(startOffsetHours);
        using var client = new HttpClient(new StubHandler(CreateForecastJson(start, start.AddHours(2))));
        var page = CreatePage(client);

        var result = await page.OnGetForecastAsync("Bedok", CancellationToken.None);

        Assert.Equal(503, result.StatusCode);
        var body = JsonSerializer.SerializeToElement(result.Value);
        Assert.Equal("Weather is temporarily unavailable. Please try again later.",
            body.GetProperty("message").GetString());
        Assert.False(body.TryGetProperty("forecast", out _));
    }

    [Fact]
    public async Task OnGetForecastAsync_Returns503_WhenProviderTimesOut()
    {
        using var client = new HttpClient(new StubHandler("{}", simulateTimeout: true));
        var page = CreatePage(client);

        var result = await page.OnGetForecastAsync("Bedok", CancellationToken.None);

        Assert.Equal(503, result.StatusCode);
        var body = JsonSerializer.SerializeToElement(result.Value);
        Assert.Equal("Weather is temporarily unavailable. Please try again later.",
            body.GetProperty("message").GetString());
        Assert.False(body.TryGetProperty("forecast", out _));
    }

    [Fact]
    public async Task OnGetForecastAsync_PropagatesCancellation_WhenCallerCancels()
    {
        using var client = new HttpClient(new StubHandler("{}"));
        using var cancellation = new CancellationTokenSource();
        var page = CreatePage(client);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => page.OnGetForecastAsync("Bedok", cancellation.Token));
    }

    private static WeatherModel CreatePage(HttpClient client)
        => new(new WeatherService(client), NullLogger<WeatherModel>.Instance);

    // Relative dates keep these tests valid after the assessment date.
    private static string CreateForecastJson(DateTimeOffset start, DateTimeOffset end)
        => JsonSerializer.Serialize(new
        {
            code = 0,
            data = new
            {
                items = new[]
                {
                    new
                    {
                        update_timestamp = start,
                        valid_period = new { start, end },
                        forecasts = new[] { new { area = "Bedok", forecast = "Cloudy" } }
                    }
                }
            }
        });

    private sealed class StubHandler(
        string json,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        bool simulateTimeout = false) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (simulateTimeout)
                throw new TaskCanceledException("Simulated provider timeout.");

            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}
