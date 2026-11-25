using System.Net;
using Xunit;

namespace Bruktborsen.Tests.Integration;

public class CorsTests
{
    [Theory]
    [InlineData("https://localhost:7100")]
    [InlineData("http://localhost:5100")]
    public async Task Get_WithAllowedOrigin_ReturnsCorsHeader(
        string origin)
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Get, "/api/categories");

        request.Headers.Add("Origin", origin);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.True(response.Headers.TryGetValues(
            "Access-Control-Allow-Origin", out var origins));

        Assert.Equal(origin, Assert.Single(origins!));
    }

    [Theory]
    [InlineData("https://unknown.example.com")]
    [InlineData("https://localhost:9999")]
    [InlineData("http://localhost:7100")]
    public async Task Get_WithDisallowedOrigin_DoesNotReturnCorsHeader(
        string origin)
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Get, "/api/categories");

        request.Headers.Add("Origin", origin);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.False(response.Headers.Contains(
            "Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Get_WithoutOrigin_ReturnsNormalResponse()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.False(response.Headers.Contains(
            "Access-Control-Allow-Origin"));
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task Preflight_WithAllowedOrigin_ReturnsCorsHeaders(
        string method)
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        const string origin = "https://localhost:7100";

        using var request = new HttpRequestMessage(
            HttpMethod.Options, "/api/listings/1");

        request.Headers.Add("Origin", origin);

        request.Headers.Add(
            "Access-Control-Request-Method", method);

        request.Headers.Add(
            "Access-Control-Request-Headers",
            "authorization, content-type");

        var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.NoContent, response.StatusCode);

        Assert.True(response.Headers.TryGetValues(
            "Access-Control-Allow-Origin", out var origins));

        Assert.Equal(origin, Assert.Single(origins!));

        Assert.True(response.Headers.TryGetValues(
            "Access-Control-Allow-Methods", out var methods));

        Assert.Contains(method, string.Join(",", methods!));

        Assert.True(response.Headers.TryGetValues(
            "Access-Control-Allow-Headers", out var headers));

        var allowedHeaders = string.Join(",", headers!)
            .ToLowerInvariant();

        Assert.Contains("authorization", allowedHeaders);
        Assert.Contains("content-type", allowedHeaders);
    }

    [Fact]
    public async Task Preflight_WithDisallowedOrigin_DoesNotGrantAccess()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Options, "/api/listings");

        request.Headers.Add(
            "Origin", "https://unknown.example.com");

        request.Headers.Add(
            "Access-Control-Request-Method", "POST");

        request.Headers.Add(
            "Access-Control-Request-Headers",
            "authorization, content-type");

        var response = await client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.NoContent, response.StatusCode);

        Assert.False(response.Headers.Contains(
            "Access-Control-Allow-Origin"));

        Assert.False(response.Headers.Contains(
            "Access-Control-Allow-Methods"));

        Assert.False(response.Headers.Contains(
            "Access-Control-Allow-Headers"));
    }
}