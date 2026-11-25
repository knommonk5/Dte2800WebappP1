
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Bruktborsen.Api.Data;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Bruktborsen.Api.DTOs.Listings;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Tests.Integration;

public class ListingFavoriteTests
{
    private const string Password = "Test123!Abc";

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

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = email,
                Password
            });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

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
                Description = "Pent brukt spillkonsoll med kontroller.",
                Price = 1500m,
                OriginalPrice = 3000m,
                Condition = Condition.Good,
                CategoryId = categoryId
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return json.RootElement.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task GetListings_WithoutLogin_ReturnsOk()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/listings");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateListing_WithoutLogin_ReturnsUnauthorized()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/listings",
            new
            {
                Title = "Testannonse",
                Description = "Dette er en testannonse.",
                Price = 100m,
                OriginalPrice = 200m,
                Condition = Condition.Good,
                CategoryId = 1
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateListing_WithLogin_ReturnsCreated()
    {
        await using var factory = new AuthTestFactory();
        using var client = await CreateUserClientAsync(
            factory, "create@example.com");

        var categoryId = await CreateCategoryAsync(factory);

        var listingId = await CreateListingAsync(client, categoryId);

        Assert.True(listingId > 0);
    }

    [Fact]
    public async Task CreateListing_WithInvalidCategory_ReturnsBadRequest()
    {
        await using var factory = new AuthTestFactory();
        using var client = await CreateUserClientAsync(
            factory, "category@example.com");

        var response = await client.PostAsJsonAsync(
            "/api/listings",
            new
            {
                Title = "Testannonse",
                Description = "Dette er en gyldig beskrivelse.",
                Price = 100m,
                OriginalPrice = 200m,
                Condition = Condition.Good,
                CategoryId = 99999
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateOwnListing_ReturnsNoContent()
    {
        await using var factory = new AuthTestFactory();
        using var client = await CreateUserClientAsync(
            factory, "owner@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(client, categoryId);

        var response = await client.PutAsJsonAsync(
            $"/api/listings/{listingId}",
            new
            {
                Title = "Oppdatert spillkonsoll",
                Description = "Oppdatert beskrivelse av spillkonsollen.",
                Price = 1200m,
                OriginalPrice = 3000m,
                Condition = Condition.Used,
                CategoryId = categoryId
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAnotherUsersListing_ReturnsForbidden()
    {
        await using var factory = new AuthTestFactory();

        using var owner = await CreateUserClientAsync(
            factory, "owner@example.com");

        using var other = await CreateUserClientAsync(
            factory, "other@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(owner, categoryId);

        var response = await other.PutAsJsonAsync(
            $"/api/listings/{listingId}",
            new
            {
                Title = "Ulovlig oppdatering",
                Description = "Denne brukeren eier ikke annonsen.",
                Price = 100m,
                OriginalPrice = 3000m,
                Condition = Condition.Good,
                CategoryId = categoryId
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAnotherUsersListing_ReturnsForbidden()
    {
        await using var factory = new AuthTestFactory();

        using var owner = await CreateUserClientAsync(
            factory, "owner@example.com");

        using var other = await CreateUserClientAsync(
            factory, "other@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(owner, categoryId);

        var response = await other.DeleteAsync(
            $"/api/listings/{listingId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetFavorites_WithoutLogin_ReturnsUnauthorized()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/favorites");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddFavorite_ReturnsCreated()
    {
        await using var factory = new AuthTestFactory();
        using var client = await CreateUserClientAsync(
            factory, "favorite@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(client, categoryId);

        var response = await client.PostAsync(
            $"/api/favorites/{listingId}", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task AddSameFavoriteTwice_ReturnsConflict()
    {
        await using var factory = new AuthTestFactory();
        using var client = await CreateUserClientAsync(
            factory, "duplicate@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(client, categoryId);

        var first = await client.PostAsync(
            $"/api/favorites/{listingId}", null);

        var second = await client.PostAsync(
            $"/api/favorites/{listingId}", null);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task GetFavorites_ReturnsOnlyOwnFavorites()
    {
        await using var factory = new AuthTestFactory();

        using var owner = await CreateUserClientAsync(
            factory, "owner@example.com");

        using var other = await CreateUserClientAsync(
            factory, "other@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(owner, categoryId);

        var addResponse = await owner.PostAsync(
            $"/api/favorites/{listingId}", null);

        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        var ownerFavorites = await owner.GetFromJsonAsync<
            List<JsonElement>>("/api/favorites");

        var otherFavorites = await other.GetFromJsonAsync<
            List<JsonElement>>("/api/favorites");

        Assert.NotNull(ownerFavorites);
        Assert.NotNull(otherFavorites);

        Assert.Single(ownerFavorites);
        Assert.Empty(otherFavorites);
    }

    [Fact]
    public async Task RemoveFavorite_ReturnsNoContent()
    {
        await using var factory = new AuthTestFactory();
        using var client = await CreateUserClientAsync(
            factory, "remove@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(client, categoryId);

        var addResponse = await client.PostAsync(
            $"/api/favorites/{listingId}", null);

        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        var removeResponse = await client.DeleteAsync(
            $"/api/favorites/{listingId}");

        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);
    }

    [Fact]
    public async Task AddFavorite_WithUnknownListing_ReturnsNotFound()
    {
        await using var factory = new AuthTestFactory();
        using var client = await CreateUserClientAsync(
            factory, "missing@example.com");

        var response = await client.PostAsync(
            "/api/favorites/99999", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    public async Task UpdateSoldListing_ReturnsConflict()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@example.com");

        using var buyer = await CreateUserClientAsync(
            factory, "buyer@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);

        await SellListingAsync(seller, buyer, listingId);

        var response = await seller.PutAsJsonAsync(
            $"/api/listings/{listingId}",
            new
            {
                Title = "Endret annonse",
                Description = "Oppdatert beskrivelse.",
                Price = 1000,
                OriginalPrice = 3000m,
                Condition = Condition.Good,
                CategoryId = categoryId
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<BruktborsenDbContext>();

        var listing = await db.Listings.FindAsync(listingId);

        Assert.NotNull(listing);
        Assert.Equal(ListingStatus.Sold, listing.Status);
        Assert.Equal(1500, listing.Price);
        Assert.Equal(1200m, listing.SoldPrice);
        Assert.Equal("Brukt spillkonsoll", listing.Title);

        Assert.False(await db.PriceChanges.AnyAsync(
            p => p.ListingId == listingId));
    }
    
    [Fact]
    public async Task DeleteSoldListing_ReturnsConflict()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@example.com");

        using var buyer = await CreateUserClientAsync(
            factory, "buyer@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);

        await SellListingAsync(seller, buyer, listingId);

        var response = await seller.DeleteAsync(
            $"/api/listings/{listingId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<BruktborsenDbContext>();

        var listing = await db.Listings.FindAsync(listingId);

        Assert.NotNull(listing);
        Assert.Equal(ListingStatus.Sold, listing.Status);
        Assert.Equal(1200m, listing.SoldPrice);
    }
    
    [Fact]
    public async Task DeleteOwnListing_SetsRemovedStatus()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);

        var response = await seller.DeleteAsync(
            $"/api/listings/{listingId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<BruktborsenDbContext>();

        var listing = await db.Listings.FindAsync(listingId);

        Assert.NotNull(listing);
        Assert.Equal(ListingStatus.Removed, listing.Status);

        using var anonymous = factory.CreateClient();

        var detailResponse = await anonymous.GetAsync(
            $"/api/listings/{listingId}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            detailResponse.StatusCode);

        var listings = await anonymous
            .GetFromJsonAsync<List<ListingDto>>("/api/listings");

        Assert.NotNull(listings);
        Assert.DoesNotContain(listings, l => l.Id == listingId);
    }
    
    [Fact]
    public async Task DeleteListing_RejectsPendingOffers()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@example.com");

        using var buyer1 = await CreateUserClientAsync(
            factory, "buyer1@example.com");

        using var buyer2 = await CreateUserClientAsync(
            factory, "buyer2@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);

        var firstOffer = await buyer1.PostAsJsonAsync(
            $"/api/listings/{listingId}/offers",
            new { Amount = 1000m });

        var secondOffer = await buyer2.PostAsJsonAsync(
            $"/api/listings/{listingId}/offers",
            new { Amount = 1200m });

        Assert.Equal(HttpStatusCode.Created, firstOffer.StatusCode);
        Assert.Equal(HttpStatusCode.Created, secondOffer.StatusCode);

        var response = await seller.DeleteAsync(
            $"/api/listings/{listingId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<BruktborsenDbContext>();

        var listing = await db.Listings.FindAsync(listingId);

        Assert.NotNull(listing);
        Assert.Equal(ListingStatus.Removed, listing.Status);

        var offers = await db.Offers
            .Where(o => o.ListingId == listingId)
            .ToListAsync();

        Assert.Equal(2, offers.Count);

        Assert.All(offers, offer =>
            Assert.Equal(OfferStatus.Rejected, offer.Status));
    }
    
    [Fact]
    public async Task GetMyListings_ReturnsOwnListingsWithAllStatuses()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@example.com");

        using var other = await CreateUserClientAsync(
            factory, "other@example.com");

        var categoryId = await CreateCategoryAsync(factory);

        var activeId = await CreateListingAsync(seller, categoryId);
        var soldId = await CreateListingAsync(seller, categoryId);
        var removedId = await CreateListingAsync(seller, categoryId);
        var otherId = await CreateListingAsync(other, categoryId);

        await SellListingAsync(seller, other, soldId);

        var deleteResponse = await seller.DeleteAsync(
            $"/api/listings/{removedId}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var listings = await seller
            .GetFromJsonAsync<List<ListingDto>>("/api/listings/my");

        Assert.NotNull(listings);
        Assert.Equal(3, listings.Count);

        Assert.Contains(listings, l =>
            l.Id == activeId &&
            l.Status == ListingStatus.Active);

        Assert.Contains(listings, l =>
            l.Id == soldId &&
            l.Status == ListingStatus.Sold);

        Assert.Contains(listings, l =>
            l.Id == removedId &&
            l.Status == ListingStatus.Removed);

        Assert.DoesNotContain(listings, l => l.Id == otherId);
    }
    
    [Fact]
    public async Task GetMyListings_WithoutLogin_ReturnsUnauthorized()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/listings/my");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
    
    private static async Task SellListingAsync(
        HttpClient seller,
        HttpClient buyer,
        int listingId)
    {
        var offerResponse = await buyer.PostAsJsonAsync(
            $"/api/listings/{listingId}/offers",
            new { Amount = 1200m });

        Assert.Equal(
            HttpStatusCode.Created,
            offerResponse.StatusCode);

        using var json = JsonDocument.Parse(
            await offerResponse.Content.ReadAsStringAsync());

        var offerId = json.RootElement
            .GetProperty("id")
            .GetInt32();

        var acceptResponse = await seller.PostAsync(
            $"/api/offers/{offerId}/accept", null);

        Assert.Equal(
            HttpStatusCode.OK,
            acceptResponse.StatusCode);
    }
}
