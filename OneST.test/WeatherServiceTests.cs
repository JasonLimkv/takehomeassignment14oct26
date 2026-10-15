using System.Net;
using System.Text;
using OneST.Weather;
using Xunit;

namespace OneST.Tests;

public class WeatherServiceTests
{
    [Fact]
    public async Task GetForecastsAsync_ReturnsForecasts_WhenResponseIsValid()
    {
        const string json = """
        {
          "code": 0,
          "data": {
            "items": [{
              "update_timestamp": "2026-10-15T10:00:00+08:00",
              "valid_period": {
                "start": "2026-10-15T10:00:00+08:00",
                "end": "2026-10-15T12:00:00+08:00"
              },
              "forecasts": [{ "area": "Bedok", "forecast": "Cloudy" }]
            }]
          }
        }
        """;

        using var client = new HttpClient(new StubHandler(json));
        var service = new WeatherService(client);
        var updates = await service.GetForecastsAsync();

        var update = Assert.Single(updates);
        Assert.NotNull(update.Forecasts);
        var forecast = Assert.Single(update.Forecasts);
        Assert.Equal("Bedok", forecast.Area);
        Assert.Equal("Cloudy", forecast.Forecast);
        Assert.Equal(TimeSpan.FromHours(8), update.UpdatedAt?.Offset);
        Assert.Equal(
            new DateTimeOffset(2026, 10, 15, 10, 0, 0, TimeSpan.FromHours(8)),
            update.ValidPeriod?.Start);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("""{"code":1}""")]
    [InlineData("""{"code":0}""")]
    [InlineData("""{"code":0,"data":{}}""")]
    [InlineData("""{"code":0,"data":{"items":[]}}""")]
    public async Task GetForecastsAsync_Throws_WhenDataIsMissingOrUnsuccessful(
        string json)
    {
        using var client = new HttpClient(new StubHandler(json));
        var service = new WeatherService(client);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => service.GetForecastsAsync());
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task GetForecastsAsync_Throws_WhenHttpRequestFails(
        HttpStatusCode statusCode)
    {
        using var client = new HttpClient(new StubHandler("{}", statusCode));
        var service = new WeatherService(client);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GetForecastsAsync());

        Assert.Equal(statusCode, exception.StatusCode);
    }
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _json;
        private readonly HttpStatusCode _statusCode;

        public StubHandler(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _json = json;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_json, Encoding.UTF8, "application/json")
            });
        }
    }
}