using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using OneST.Weather;

namespace OneST.IntegrationTests;

public sealed class WeatherWebApplicationFactory(Func<HttpResponseMessage> responseFactory) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
            services.Configure<HttpClientFactoryOptions>(nameof(WeatherService), options =>
                options.HttpMessageHandlerBuilderActions.Add(handlerBuilder =>
                    handlerBuilder.PrimaryHandler = new StubWeatherHandler(responseFactory))));
    }

    private sealed class StubWeatherHandler(Func<HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("https://api-open.data.gov.sg/v2/real-time/api/two-hr-forecast", request.RequestUri?.AbsoluteUri);

            return Task.FromResult(responseFactory());
        }
    }
}