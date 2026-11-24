using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Bruktborsen.Api.Data;
using Bruktborsen.Api.DTOs.PriceHistory;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bruktborsen.Tests.Integration;

public class PriceHistoryTests
{
    private const string Password = "Test123!Abc";

    [Fact]
    public async Task GetPriceHistory_AnonymousUser_ReturnsOk()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@test.no");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);

        using var anonymous = factory.CreateClient();

        var response = await anonymous.GetAsync(
            $"/api/listings/{listingId}/price-history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPriceHistory_NewListing_ReturnsEmptyList()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@test.no");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);

        var history = await GetHistoryAsync(seller, listingId);

        Assert.Empty(history);
    }

    [Fact]
    public async Task UpdateListing_PriceReduction_RegistersHistory()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@test.no");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);

        var response = await UpdateListingAsync(
            seller, listingId, categoryId, 1200m);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var history = await GetHistoryAsync(seller, listingId);
        var change = Assert.Single(history);

        Assert.Equal(listingId, change.ListingId);
        Assert.Equal(1500m, change.OldPrice);
        Assert.Equal(1200m, change.NewPrice);
    }

    [Fact]
    public async Task UpdateListing_PriceIncrease_RegistersHistory()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@test.no");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);

        var response = await UpdateListingAsync(
            seller, listingId, categoryId, 1800m);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var history = await GetHistoryAsync(seller, listingId);
        var change = Assert.Single(history);

        Assert.Equal(1500m, change.OldPrice);
        Assert.Equal(1800m, change.NewPrice);
    }

    [Fact]
    public async Task UpdateListing_UnchangedPrice_NoHistory()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@test.no");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);

        var response = await UpdateListingAsync(
            seller, listingId, categoryId, 1500m);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var history = await GetHistoryAsync(seller, listingId);

        Assert.Empty(history);
    }

    [Fact]
    public async Task UpdateListing_ChangedTitle_NoPriceHistory()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@test.no");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);

        var response = await UpdateListingAsync(
            seller,
            listingId,
            categoryId,
            1500m,
            "Oppdatert spillkonsoll");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var history = await GetHistoryAsync(seller, listingId);

        Assert.Empty(history);
    }

    [Fact]
    public async Task UpdateListing_MultiplePriceChanges_ReturnsOrderedHistory()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@test.no");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);

        var first = await UpdateListingAsync(
            seller, listingId, categoryId, 1300m);

        var second = await UpdateListingAsync(
            seller, listingId, categoryId, 1100m);

        var third = await UpdateListingAsync(
            seller, listingId, categoryId, 1200m);

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, third.StatusCode);

        var history = await GetHistoryAsync(seller, listingId);

        Assert.Equal(3, history.Count);

        Assert.Equal(1500m, history[0].OldPrice);
        Assert.Equal(1300m, history[0].NewPrice);

        Assert.Equal(1300m, history[1].OldPrice);
        Assert.Equal(1100m, history[1].NewPrice);

        Assert.Equal(1100m, history[2].OldPrice);
        Assert.Equal(1200m, history[2].NewPrice);

        Assert.True(history[0].ChangedAt <= history[1].ChangedAt);
        Assert.True(history[1].ChangedAt <= history[2].ChangedAt);
    }

    [Fact]
    public async Task UpdateListing_OtherUser_NoPriceHistory()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@test.no");

        using var otherUser = await CreateUserClientAsync(
            factory, "other@test.no");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);

        var response = await UpdateListingAsync(
            otherUser, listingId, categoryId, 100m);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var history = await GetHistoryAsync(seller, listingId);

        Assert.Empty(history);
    }

    [Fact]
    public async Task UpdateListing_AnonymousUser_NoPriceHistory()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@test.no");

        using var anonymous = factory.CreateClient();

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);

        var response = await UpdateListingAsync(
            anonymous, listingId, categoryId, 100m);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var history = await GetHistoryAsync(seller, listingId);

        Assert.Empty(history);
    }

    [Fact]
    public async Task UpdateListing_InvalidPrice_NoPriceHistory()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@test.no");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);

        var response = await UpdateListingAsync(
            seller, listingId, categoryId, -100m);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var history = await GetHistoryAsync(seller, listingId);

        Assert.Empty(history);
    }

    [Fact]
    public async Task GetPriceHistory_UnknownListing_ReturnsNotFound()
    {
        await using var factory = new AuthTestFactory();

        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/listings/999999/price-history");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPriceHistory_SoldListing_ReturnsHistory()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@test.no");

        using var buyer = await CreateUserClientAsync(
            factory, "buyer@test.no");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);

        var updateResponse = await UpdateListingAsync(
            seller, listingId, categoryId, 1200m);

        Assert.Equal(
            HttpStatusCode.NoContent,
            updateResponse.StatusCode);

        var offerResponse = await buyer.PostAsJsonAsync(
            "/api/offers",
            new
            {
                ListingId = listingId,
                Amount = 1200m
            });

        Assert.Equal(
            HttpStatusCode.Created,
            offerResponse.StatusCode);

        using var offerJson = JsonDocument.Parse(
            await offerResponse.Content.ReadAsStringAsync());

        var offerId = offerJson.RootElement
            .GetProperty("id")
            .GetInt32();

        var acceptResponse = await seller.PutAsync(
            $"/api/offers/{offerId}/accept",
            null);

        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);

        using var anonymous = factory.CreateClient();

        var history = await GetHistoryAsync(anonymous, listingId);
        var change = Assert.Single(history);

        Assert.Equal(1500m, change.OldPrice);
        Assert.Equal(1200m, change.NewPrice);
    }

    [Fact]
    public async Task UpdateListing_PriceChange_UpdatesCurrentPrice()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@test.no");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);

        var response = await UpdateListingAsync(
            seller, listingId, categoryId, 1200m);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var listingResponse = await seller.GetAsync(
            $"/api/listings/{listingId}");

        Assert.Equal(HttpStatusCode.OK, listingResponse.StatusCode);

        using var json = JsonDocument.Parse(
            await listingResponse.Content.ReadAsStringAsync());

        var currentPrice = json.RootElement
            .GetProperty("price")
            .GetDecimal();

        var history = await GetHistoryAsync(seller, listingId);
        var change = Assert.Single(history);

        Assert.Equal(1200m, currentPrice);
        Assert.Equal(currentPrice, change.NewPrice);
    }

    // -------------------------------------------------
    // Hjelpemetoder
    // -------------------------------------------------

    private static async Task<HttpClient> CreateUserClientAsync(
        AuthTestFactory factory,
        string email)
    {
        var client = factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FullName = "Test Bruker",
                Email = email,
                Password
            });

        Assert.Equal(
            HttpStatusCode.OK,
            registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = email,
                Password
            });

        Assert.Equal(
            HttpStatusCode.OK,
            loginResponse.StatusCode);

        using var json = JsonDocument.Parse(
            await loginResponse.Content.ReadAsStringAsync());

        var token = json.RootElement
            .GetProperty("accessToken")
            .GetString();

        Assert.False(string.IsNullOrWhiteSpace(token));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    private static async Task<int> CreateCategoryAsync(
        AuthTestFactory factory)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<BruktborsenDbContext>();

        var category = new Category
        {
            Name = "Elektronikk"
        };

        db.Categories.Add(category);

        await db.SaveChangesAsync();

        return category.Id;
    }

    private static async Task<int> CreateListingAsync(
        HttpClient client,
        int categoryId)
    {
        var response = await client.PostAsJsonAsync(
            "/api/listings",
            new
            {
                Title = "Brukt spillkonsoll",
                Description =
                    "Pent brukt spillkonsoll med kontroller.",
                Price = 1500m,
                OriginalPrice = 3000m,
                Condition = Condition.Good,
                CategoryId = categoryId
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return json.RootElement
            .GetProperty("id")
            .GetInt32();
    }

    private static async Task<HttpResponseMessage> UpdateListingAsync(
        HttpClient client,
        int listingId,
        int categoryId,
        decimal price,
        string title = "Brukt spillkonsoll")
    {
        return await client.PutAsJsonAsync(
            $"/api/listings/{listingId}",
            new
            {
                Title = title,
                Description =
                    "Pent brukt spillkonsoll med kontroller.",
                Price = price,
                OriginalPrice = 3000m,
                Condition = Condition.Good,
                CategoryId = categoryId
            });
    }

    private static async Task<List<PriceChangeDto>> GetHistoryAsync(
        HttpClient client,
        int listingId)
    {
        var response = await client.GetAsync(
            $"/api/listings/{listingId}/price-history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var history = await response.Content
            .ReadFromJsonAsync<List<PriceChangeDto>>();

        Assert.NotNull(history);

        return history!;
    }
}