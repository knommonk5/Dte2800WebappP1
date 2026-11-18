
using System.Net;
using System.Net.Http.Json;
using Bruktborsen.Api.Data;
using Bruktborsen.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Bruktborsen.Tests.Integration;

public class AuthenticationTests
{
    [Fact]
    public async Task Register_CreatesUserWithUserRole()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FullName = "Test Bruker",
                Email = "test@example.com",
                Password = "Test123!Abc"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync(
            "test@example.com");

        Assert.NotNull(user);
        Assert.Equal("Test Bruker", user.FullName);
        Assert.True(await userManager.IsInRoleAsync(user, "User"));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FullName = "Test Bruker",
                Email = "wrong@example.com",
                Password = "Test123!Abc"
            });

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = "wrong@example.com",
                Password = "FeilPassord123!"
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsBearerToken()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FullName = "Test Bruker",
                Email = "login@example.com",
                Password = "Test123!Abc"
            });

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = "login@example.com",
                Password = "Test123!Abc"
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<
            System.Text.Json.JsonElement>();

        Assert.Equal(
            "Bearer",
            json.GetProperty("tokenType").GetString());

        Assert.False(string.IsNullOrWhiteSpace(
            json.GetProperty("accessToken").GetString()));
    }
}

public class AuthTestFactory :
    WebApplicationFactory<Program>,
    IAsyncDisposable
{
    private readonly SqliteConnection _connection =
        new("Data Source=:memory:");

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<
                DbContextOptions<BruktborsenDbContext>>();

            services.RemoveAll<
                Microsoft.EntityFrameworkCore.Infrastructure
                    .IDbContextOptionsConfiguration<BruktborsenDbContext>>();

            services.AddDbContext<BruktborsenDbContext>(
                options => options.UseSqlite(_connection));

            using var scope = services.BuildServiceProvider()
                .CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<BruktborsenDbContext>();

            db.Database.EnsureCreated();
        });
    }

    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
