
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Bruktborsen.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bruktborsen.Tests.Integration;

public class CategoryTests
{
    [Fact]
    public async Task GetAll_WithoutLogin_ReturnsOk()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutLogin_ReturnsUnauthorized()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/categories",
            new { Name = "Elektronikk" });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Create_WithUserRole_ReturnsForbidden()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var token = await CreateUserAndLoginAsync(
            client, "user@example.com");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync(
            "/api/categories",
            new { Name = "Elektronikk" });

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Admin_CanCreateCategory()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var token = await CreateAdminAndLoginAsync(
            factory, client, "admin@example.com");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync(
            "/api/categories",
            new { Name = "Elektronikk" });

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        var json = await response.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            "Elektronikk",
            json.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Admin_CannotCreateDuplicateCategory()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var token = await CreateAdminAndLoginAsync(
            factory, client, "duplicate@example.com");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        await client.PostAsJsonAsync(
            "/api/categories",
            new { Name = "Elektronikk" });

        var response = await client.PostAsJsonAsync(
            "/api/categories",
            new { Name = "Elektronikk" });

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }

    [Fact]
    public async Task Admin_CannotCreateWhitespaceCategory()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var token = await CreateAdminAndLoginAsync(
            factory, client, "validation@example.com");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync(
            "/api/categories",
            new { Name = "   " });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }
    
    [Fact]
    public async Task Admin_CanUpdateCategory()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var token = await CreateAdminAndLoginAsync(
            factory, client, "update@example.com");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var created = await client.PostAsJsonAsync(
            "/api/categories",
            new { Name = "Elektronikk" });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var category = await created.Content
            .ReadFromJsonAsync<JsonElement>();

        var id = category.GetProperty("id").GetInt32();

        var response = await client.PutAsJsonAsync(
            $"/api/categories/{id}",
            new { Name = "Datautstyr" });
    
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var updated = await client.GetAsync($"/api/categories/{id}");
        var json = await updated.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Datautstyr", json.GetProperty("name").GetString());
    }

[Fact]
public async Task Admin_CanDeleteUnusedCategory()
{
    await using var factory = new AuthTestFactory();
    using var client = factory.CreateClient();

    var token = await CreateAdminAndLoginAsync(
        factory, client, "delete@example.com");

    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);

    var created = await client.PostAsJsonAsync(
        "/api/categories",
        new { Name = "Møbler" });

    Assert.Equal(HttpStatusCode.Created, created.StatusCode);

    var category = await created.Content
        .ReadFromJsonAsync<JsonElement>();

    var id = category.GetProperty("id").GetInt32();

    var response = await client.DeleteAsync(
        $"/api/categories/{id}");

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

    var getResponse = await client.GetAsync(
        $"/api/categories/{id}");

    Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
}

[Fact]
public async Task User_CannotUpdateOrDeleteCategory()
{
    await using var factory = new AuthTestFactory();
    using var client = factory.CreateClient();

    var token = await CreateUserAndLoginAsync(
        client, "normaluser@example.com");

    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);

    var update = await client.PutAsJsonAsync(
        "/api/categories/1",
        new { Name = "Endret" });

    var delete = await client.DeleteAsync(
        "/api/categories/1");

    Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
    Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
}

[Fact]
public async Task Admin_CannotDeleteCategoryWithListings()
{
    await using var factory = new AuthTestFactory();
    using var client = factory.CreateClient();

    var token = await CreateAdminAndLoginAsync(
        factory, client, "linked@example.com");

    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", token);

    var created = await client.PostAsJsonAsync(
        "/api/categories",
        new { Name = "Spill" });

    Assert.Equal(HttpStatusCode.Created, created.StatusCode);

    var category = await created.Content
        .ReadFromJsonAsync<JsonElement>();

    var id = category.GetProperty("id").GetInt32();

    // Opprett en annonse som bruker kategorien.
    using (var scope = factory.Services.CreateScope())
    {
        var db = scope.ServiceProvider
            .GetRequiredService<Bruktborsen.Api.Data.BruktborsenDbContext>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var seller = await userManager.FindByEmailAsync(
            "linked@example.com");

        Assert.NotNull(seller);

        db.Listings.Add(new Listing
        {
            Title = "Brukt spill",
            Description = "Et brukt spill i god stand",
            Price = 250m,
            CategoryId = id,
            SellerId = seller.Id
        });

        await db.SaveChangesAsync();
    }

    var response = await client.DeleteAsync(
        $"/api/categories/{id}");

    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
}

    

    private static async Task<string> CreateUserAndLoginAsync(
        HttpClient client, string email)
    {
        var register = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FullName = "Test User",
                Email = email,
                Password = "Test123!Abc"
            });

        register.EnsureSuccessStatusCode();

        return await LoginAsync(client, email);
    }

    private static async Task<string> CreateAdminAndLoginAsync(
        AuthTestFactory factory,
        HttpClient client,
        string email)
    {
        await CreateUserAndLoginAsync(client, email);

        using var scope = factory.Services.CreateScope();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync(email);

        Assert.NotNull(user);

        var result = await userManager.AddToRoleAsync(
            user, "Admin");

        Assert.True(result.Succeeded);

        // Logg inn på nytt slik at Admin-rollen
        // blir inkludert i det nye tokenet.
        return await LoginAsync(client, email);
    }

    private static async Task<string> LoginAsync(
        HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = email,
                Password = "Test123!Abc"
            });

        response.EnsureSuccessStatusCode();

        var json = await response.Content
            .ReadFromJsonAsync<JsonElement>();

        return json.GetProperty("accessToken")
            .GetString()!;
    }
}
