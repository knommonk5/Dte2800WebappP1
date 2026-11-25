
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
using System.Net.Http.Headers;
using System.Text.Json;
using Bruktborsen.Api.DTOs.Auth;

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
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Register_WithInvalidFullName_ReturnsBadRequest(
        string? fullName)
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FullName = fullName,
                Email = "invalid@example.com",
                Password = "Test123!Abc"
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        using var scope = factory.Services.CreateScope();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync(
            "invalid@example.com");

        Assert.Null(user);
    }
    
    [Fact]
    public async Task Register_WithoutFullName_ReturnsBadRequest()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                Email = "missing@example.com",
                Password = "Test123!Abc"
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        using var scope = factory.Services.CreateScope();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        Assert.Null(await userManager.FindByEmailAsync(
            "missing@example.com"));
    }
    
    [Theory]
    [InlineData(150, HttpStatusCode.OK)]
    [InlineData(151, HttpStatusCode.BadRequest)]
    public async Task Register_WithFullNameLength_ReturnsExpectedStatus(
        int length,
        HttpStatusCode expectedStatus)
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var fullName = new string('A', length);

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FullName = fullName,
                Email = "length@example.com",
                Password = "Test123!Abc"
            });

        Assert.Equal(expectedStatus, response.StatusCode);

        using var scope = factory.Services.CreateScope();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync(
            "length@example.com");

        if (expectedStatus == HttpStatusCode.OK)
        {
            Assert.NotNull(user);
            Assert.Equal(fullName, user.FullName);
        }
        else
        {
            Assert.Null(user);
        }
    }
    
    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var registration = new
        {
            FullName = "Test Bruker",
            Email = "duplicate@example.com",
            Password = "Test123!Abc"
        };

        var first = await client.PostAsJsonAsync(
            "/api/auth/register", registration);

        var second = await client.PostAsJsonAsync(
            "/api/auth/register", registration);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            second.StatusCode);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<BruktborsenDbContext>();

        Assert.Equal(1, await db.Users.CountAsync(
            u => u.Email == registration.Email));
    }
    
    [Fact]
    public async Task Me_WithValidToken_ReturnsCurrentUser()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        const string email = "profile@example.com";
        const string password = "Test123!Abc";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FullName = "  Test Bruker  ",
                Email = email,
                Password = password
            });

        Assert.Equal(
            HttpStatusCode.OK,
            registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = email,
                Password = password
            });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        var json = await loginResponse.Content
            .ReadFromJsonAsync<JsonElement>();

        var token = json.GetProperty("accessToken").GetString();

        Assert.False(string.IsNullOrWhiteSpace(token));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var result = await client.GetFromJsonAsync<UserDto>(
            "/api/auth/me");

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Id));
        Assert.Equal("Test Bruker", result.FullName);
        Assert.Equal(email, result.Email);
        Assert.Contains("User", result.Roles);
        Assert.DoesNotContain("Admin", result.Roles);

        using var scope = factory.Services.CreateScope();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync(email);

        Assert.NotNull(user);
        Assert.Equal(user.Id, result.Id);
    }
    
    [Fact]
    public async Task Me_WithInvalidToken_ReturnsUnauthorized()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer", "invalid-token");

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
    
    [Fact]
    public async Task Register_WithAdminRoleInRequest_DoesNotGrantAdmin()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FullName = "Test Bruker",
                Email = "role@example.com",
                Password = "Test123!Abc",
                Role = "Admin",
                Roles = new[] { "Admin" }
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync(
            "role@example.com");

        Assert.NotNull(user);

        var roles = await userManager.GetRolesAsync(user);

        Assert.Single(roles);
        Assert.Contains("User", roles);
        Assert.DoesNotContain("Admin", roles);
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
