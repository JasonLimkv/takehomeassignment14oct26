using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace OneST.IntegrationTests;

public class ErrorHttpTests
{
    [Fact]
    public async Task GetWeather_ShowsSafeErrorPage_WhenUnexpectedErrorOccursInProduction()
    {
        const string internalDetail = "test-internal-detail-do-not-expose";
        await using var weatherFactory = new WeatherWebApplicationFactory(() => throw new InvalidOperationException(internalDetail));
        await using var factory = weatherFactory.WithWebHostBuilder(builder => builder.UseEnvironment(Environments.Production));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        Assert.Equal(Environments.Production, factory.Services.GetRequiredService<IHostEnvironment>().EnvironmentName);
        using var response = await client.GetAsync("/Weather");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Something went wrong", html);
        Assert.DoesNotContain(internalDetail, html);
        Assert.DoesNotContain(nameof(InvalidOperationException), html);
        Assert.True(response.Headers.CacheControl?.NoStore == true);
    }
}