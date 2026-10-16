using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;

namespace OneST.IntegrationTests;

public class IndexHttpTests
{
    [Fact]
    public async Task GetIndex_ReturnsUenPage()
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        using var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("name=\"Uen\"", html);
        Assert.Contains("name=\"__RequestVerificationToken\"", html);
    }
    [Theory]
    [InlineData("12345678A", ".alert-success", "Valid UEN format.")]
    [InlineData("T09LL0001B", ".alert-success", "Valid UEN format.")]
    [InlineData("T09LL0001BX", "#uen-error", "Enter no more than 10 characters.")]
    [InlineData(" t09ll0001b ", ".alert-success", "Valid UEN format.")]
    [InlineData("123", ".alert-danger", "Invalid UEN format.")]
    [InlineData("", "#uen-error", "Enter a UEN.")]
    [InlineData("   ", "#uen-error", "Enter a UEN.")]
    public async Task PostIndex_ReturnsExpectedMessage(string uen, string resultSelector, string expectedMessage)
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        using var pageResponse = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);
        using var page = await new HtmlParser().ParseDocumentAsync(await pageResponse.Content.ReadAsStringAsync());

        var form = page.QuerySelector("form.uen-form");
        Assert.NotNull(form);
        Assert.Equal("post", form.GetAttribute("method")?.ToLowerInvariant());
        var input = Assert.IsAssignableFrom<IHtmlInputElement>(form.QuerySelector("input[name='Uen']"));
        var token = Assert.IsAssignableFrom<IHtmlInputElement>(form.QuerySelector("input[name='__RequestVerificationToken']"));
        Assert.NotNull(input.Name);
        Assert.NotNull(token.Name);
        Assert.False(string.IsNullOrWhiteSpace(token.Value));

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            [input.Name] = uen,
            [token.Name] = token.Value
        });
        using var response = await client.PostAsync(form.GetAttribute("action") ?? "/", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        using var result = await new HtmlParser().ParseDocumentAsync( await response.Content.ReadAsStringAsync());
        Assert.Equal(expectedMessage, result.QuerySelector(resultSelector)?.TextContent.Trim());
    }
    [Theory]
    [InlineData(null)]
    [InlineData("invalid-token")]
    public async Task PostIndex_ReturnsBadRequest_WhenAntiforgeryTokenIsMissingOrInvalid(
        string? tokenValue)
    {
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        using var pageResponse = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);

        var fields = new Dictionary<string, string>
        {
            ["Uen"] = "T09LL0001B"
        };
        if (tokenValue is not null)
        {
            fields["__RequestVerificationToken"] = tokenValue;
        }

        using var content = new FormUrlEncodedContent(fields);
        using var response = await client.PostAsync("/", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    [Fact]
    public async Task PostIndex_EncodesHtmlInSubmittedUen()
    {
        const string payload = "\"><img id='uen-injection' src=x onerror=alert(1)>";
        await using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        using var pageResponse = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);
        using var page = await new HtmlParser().ParseDocumentAsync(
            await pageResponse.Content.ReadAsStringAsync());
        var token = Assert.IsAssignableFrom<IHtmlInputElement>(
            page.QuerySelector("form.uen-form input[name='__RequestVerificationToken']"));
        Assert.NotNull(token.Name);

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Uen"] = payload,
            [token.Name] = token.Value
        });
        using var response = await client.PostAsync("/", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);

        var html = await response.Content.ReadAsStringAsync();
        using var result = await new HtmlParser().ParseDocumentAsync(html);
        var returnedInput = Assert.IsAssignableFrom<IHtmlInputElement>(
            result.QuerySelector("input[name='Uen']"));

        Assert.Equal(payload, returnedInput.Value);
        Assert.DoesNotContain(payload, html);
        Assert.Null(result.QuerySelector("#uen-injection"));
    }
}