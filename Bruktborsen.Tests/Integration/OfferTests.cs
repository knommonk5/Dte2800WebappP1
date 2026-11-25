using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Bruktborsen.Api.Data;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace Bruktborsen.Tests.Integration;

public class OfferTests
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
                Price = 1500,
                OriginalPrice = 3000m,
                Condition = Condition.Good,
                CategoryId = categoryId
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return json.RootElement.GetProperty("id").GetInt32();
    }

    private static async Task<int> CreateOfferAsync(
        HttpClient client,
        int listingId,
        decimal amount = 1200m)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/listings/{listingId}/offers",
            new
            {
                Amount = amount
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return json.RootElement.GetProperty("id").GetInt32();
    }

    private static async Task<OfferStatus> GetOfferStatusAsync(
        AuthTestFactory factory,
        int offerId)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<BruktborsenDbContext>();

        var offer = await db.Offers.FindAsync(offerId);

        Assert.NotNull(offer);

        return offer.Status;
    }

    // Test 1

    [Fact]
    public async Task CreateOffer_WithoutLogin_ReturnsUnauthorized()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/listings/1/offers",
            new
            {
                Amount = 1200m
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    // Test 2

    [Fact]
    public async Task CreateOffer_WithValidData_ReturnsCreated()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@example.com");

        using var buyer = await CreateUserClientAsync(
            factory, "buyer@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);

        var offerId = await CreateOfferAsync(buyer, listingId);

        Assert.True(offerId > 0);

        Assert.Equal(
            OfferStatus.Pending,
            await GetOfferStatusAsync(factory, offerId));
    }

    // Test 3

    [Fact]
    public async Task CreateOffer_OnOwnListing_ReturnsBadRequest()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);

        var response = await seller.PostAsJsonAsync(
            $"/api/listings/{listingId}/offers",
            new
            {
                Amount = 1200m
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    // Test 4

    [Fact]
    public async Task CreateOffer_OnUnknownListing_ReturnsNotFound()
    {
        await using var factory = new AuthTestFactory();

        using var buyer = await CreateUserClientAsync(
            factory, "buyer@example.com");

        var response = await buyer.PostAsJsonAsync(
            "/api/listings/99999/offers",
            new
            {
                Amount = 1200m
            });

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    // Test 5

    [Fact]
    public async Task CreateOffer_WithNegativeAmount_ReturnsBadRequest()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@example.com");

        using var buyer = await CreateUserClientAsync(
            factory, "buyer@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);

        var response = await buyer.PostAsJsonAsync(
            $"/api/listings/{listingId}/offers",
            new
            {
                Amount = -100m
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    // Test 6

    [Fact]
    public async Task GetMyOffers_WithoutLogin_ReturnsUnauthorized()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/offers/my");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    // Test 7

    [Fact]
    public async Task GetMyOffers_ReturnsOwnOffers()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@example.com");

        using var buyer = await CreateUserClientAsync(
            factory, "buyer@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);

        var offerId = await CreateOfferAsync(buyer, listingId);

        var offers = await buyer.GetFromJsonAsync<List<JsonElement>>(
            "/api/offers/my");

        Assert.NotNull(offers);

        var offer = Assert.Single(offers);

        Assert.Equal(
            offerId,
            offer.GetProperty("id").GetInt32());
    }

    // Test 8

    [Fact]
    public async Task GetMyOffers_DoesNotReturnOtherUsersOffers()
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

        var offerId1 = await CreateOfferAsync(
            buyer1, listingId, 1000m);

        var offerId2 = await CreateOfferAsync(
            buyer2, listingId, 1200m);

        var offers = await buyer1.GetFromJsonAsync<List<JsonElement>>(
            "/api/offers/my");

        Assert.NotNull(offers);

        var offer = Assert.Single(offers);

        Assert.Equal(
            offerId1,
            offer.GetProperty("id").GetInt32());

        Assert.NotEqual(
            offerId2,
            offer.GetProperty("id").GetInt32());
    }

    // Test 9

    [Fact]
    public async Task GetListingOffers_AsSeller_ReturnsOffers()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@example.com");

        using var buyer = await CreateUserClientAsync(
            factory, "buyer@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);

        var offerId = await CreateOfferAsync(buyer, listingId);

        var offers = await seller.GetFromJsonAsync<List<JsonElement>>(
            $"/api/listings/{listingId}/offers");

        Assert.NotNull(offers);

        var offer = Assert.Single(offers);

        Assert.Equal(
            offerId,
            offer.GetProperty("id").GetInt32());
    }

    // Test 10

    [Fact]
    public async Task GetListingOffers_AsOtherUser_ReturnsForbidden()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@example.com");

        using var buyer = await CreateUserClientAsync(
            factory, "buyer@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);

        await CreateOfferAsync(buyer, listingId);

        var response = await buyer.GetAsync(
            $"/api/listings/{listingId}/offers");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    // Test 11

    [Fact]
    public async Task AcceptOffer_AsSeller_ReturnsOk()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@example.com");

        using var buyer = await CreateUserClientAsync(
            factory, "buyer@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);
        var offerId = await CreateOfferAsync(buyer, listingId);

        var response = await seller.PostAsync(
            $"/api/offers/{offerId}/accept", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Equal(
            OfferStatus.Accepted,
            await GetOfferStatusAsync(factory, offerId));
    }

    // Test 12

    [Fact]
    public async Task AcceptOffer_MarksListingAsSold()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@example.com");

        using var buyer = await CreateUserClientAsync(
            factory, "buyer@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);
        var offerId = await CreateOfferAsync(buyer, listingId);

        var response = await seller.PostAsync(
            $"/api/offers/{offerId}/accept", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<BruktborsenDbContext>();

        var listing = await db.Listings.FindAsync(listingId);

        Assert.NotNull(listing);
        Assert.Equal(ListingStatus.Sold, listing.Status);
        Assert.NotNull(listing.SoldDate);
        Assert.Equal(1200m, listing.SoldPrice);
    }

    // Test 13

    [Fact]
    public async Task AcceptOffer_RejectsOtherPendingOffers()
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

        var offerId1 = await CreateOfferAsync(
            buyer1, listingId, 1000m);

        var offerId2 = await CreateOfferAsync(
            buyer2, listingId, 1300m);

        var response = await seller.PostAsync(
            $"/api/offers/{offerId2}/accept", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Equal(
            OfferStatus.Rejected,
            await GetOfferStatusAsync(factory, offerId1));

        Assert.Equal(
            OfferStatus.Accepted,
            await GetOfferStatusAsync(factory, offerId2));
    }

    // Test 14

    [Fact]
    public async Task AcceptOffer_AsBuyer_ReturnsForbidden()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@example.com");

        using var buyer = await CreateUserClientAsync(
            factory, "buyer@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);
        var offerId = await CreateOfferAsync(buyer, listingId);

        var response = await buyer.PostAsync(
            $"/api/offers/{offerId}/accept", null);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.Equal(
            OfferStatus.Pending,
            await GetOfferStatusAsync(factory, offerId));
    }

    // Test 15

    [Fact]
    public async Task AcceptOffer_AlreadyProcessed_ReturnsConflict()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@example.com");

        using var buyer = await CreateUserClientAsync(
            factory, "buyer@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);
        var offerId = await CreateOfferAsync(buyer, listingId);

        var first = await seller.PostAsync(
            $"/api/offers/{offerId}/accept", null);

        var second = await seller.PostAsync(
            $"/api/offers/{offerId}/accept", null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        Assert.Equal(
            HttpStatusCode.Conflict,
            second.StatusCode);
    }

    // Test 16

    [Fact]
    public async Task RejectOffer_AsSeller_ReturnsOk()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@example.com");

        using var buyer = await CreateUserClientAsync(
            factory, "buyer@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);
        var offerId = await CreateOfferAsync(buyer, listingId);

        var response = await seller.PostAsync(
            $"/api/offers/{offerId}/reject", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Equal(
            OfferStatus.Rejected,
            await GetOfferStatusAsync(factory, offerId));
    }

    // Test 17

    [Fact]
    public async Task RejectOffer_AsOtherUser_ReturnsForbidden()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@example.com");

        using var buyer = await CreateUserClientAsync(
            factory, "buyer@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);
        var offerId = await CreateOfferAsync(buyer, listingId);

        var response = await buyer.PostAsync(
            $"/api/offers/{offerId}/reject", null);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.Equal(
            OfferStatus.Pending,
            await GetOfferStatusAsync(factory, offerId));
    }

    // Test 18

    [Fact]
    public async Task WithdrawOffer_AsBuyer_ReturnsOk()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@example.com");

        using var buyer = await CreateUserClientAsync(
            factory, "buyer@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);
        var offerId = await CreateOfferAsync(buyer, listingId);

        var response = await buyer.PostAsync(
            $"/api/offers/{offerId}/withdraw", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Equal(
            OfferStatus.Withdrawn,
            await GetOfferStatusAsync(factory, offerId));
    }

    // Test 19

    [Fact]
    public async Task WithdrawOffer_AsOtherUser_ReturnsForbidden()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@example.com");

        using var buyer = await CreateUserClientAsync(
            factory, "buyer@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);
        var offerId = await CreateOfferAsync(buyer, listingId);

        var response = await seller.PostAsync(
            $"/api/offers/{offerId}/withdraw", null);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.Equal(
            OfferStatus.Pending,
            await GetOfferStatusAsync(factory, offerId));
    }

    // Test 20

    [Fact]
    public async Task CreateOffer_OnSoldListing_ReturnsConflict()
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
        var offerId = await CreateOfferAsync(buyer1, listingId);

        var acceptResponse = await seller.PostAsync(
            $"/api/offers/{offerId}/accept", null);

        Assert.Equal(
            HttpStatusCode.OK,
            acceptResponse.StatusCode);

        var response = await buyer2.PostAsJsonAsync(
            $"/api/listings/{listingId}/offers",
            new
            {
                Amount = 1400m
            });

        Assert.Equal(
            HttpStatusCode.Conflict,
            response.StatusCode);
    }
    
    [Fact]
    public async Task AcceptOffer_WithStaleListingStatus_ThrowsConcurrencyException()
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

        var offerId1 = await CreateOfferAsync(
            buyer1, listingId, 1000m);

        var offerId2 = await CreateOfferAsync(
            buyer2, listingId, 1300m);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<BruktborsenDbContext>();

        // Simuler en forespørsel som allerede har lest annonsen før en annen forespørsel aksepterer et bud.
            var staleListing = await db.Listings.FindAsync(listingId);
            var staleOffer = await db.Offers.FindAsync(offerId2);

        Assert.NotNull(staleListing);
        Assert.NotNull(staleOffer);

        Assert.Equal(ListingStatus.Active, staleListing.Status);
        Assert.Equal(OfferStatus.Pending, staleOffer.Status);

        // Den første forespørselen aksepterer budet.
        var response = await seller.PostAsync(
            $"/api/offers/{offerId1}/accept", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Den andre forespørselen forsøker å lagre basert på utdaterte data.
        staleListing.Status = ListingStatus.Sold;
        staleListing.SoldDate = DateTime.UtcNow;
        staleOffer.Status = OfferStatus.Accepted;

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => db.SaveChangesAsync());

        // Kontroller at bare det første budet ble akseptert.
        Assert.Equal(
            OfferStatus.Accepted,
            await GetOfferStatusAsync(factory, offerId1));

        Assert.Equal(
            OfferStatus.Rejected,
            await GetOfferStatusAsync(factory, offerId2));
    }
    
    [Fact]
    public async Task CreateOffer_SecondPendingOffer_ReturnsConflict()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@example.com");

        using var buyer = await CreateUserClientAsync(
            factory, "buyer@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);

        var offerId = await CreateOfferAsync(
            buyer, listingId, 1000m);

        var response = await buyer.PostAsJsonAsync(
            $"/api/listings/{listingId}/offers",
            new
            {
                Amount = 1200m
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<BruktborsenDbContext>();

        var offers = await db.Offers
            .Where(o => o.ListingId == listingId)
            .ToListAsync();

        var offer = Assert.Single(offers);

        Assert.Equal(offerId, offer.Id);
        Assert.Equal(OfferStatus.Pending, offer.Status);
        Assert.Equal(1000m, offer.Amount);
    }
    
    [Fact]
    public async Task CreateOffer_BelowMinimumAmount_ReturnsBadRequest()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@example.com");

        using var buyer = await CreateUserClientAsync(
            factory, "buyer@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);

        var response = await buyer.PostAsJsonAsync(
            $"/api/listings/{listingId}/offers",
            new
            {
                Amount = 749.99m
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<BruktborsenDbContext>();

        Assert.False(await db.Offers.AnyAsync(
            o => o.ListingId == listingId));
    }
    
    [Fact]
    public async Task CreateOffer_ExactlyMinimumAmount_ReturnsCreated()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@example.com");

        using var buyer = await CreateUserClientAsync(
            factory, "buyer@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);

        var offerId = await CreateOfferAsync(
            buyer, listingId, 750m);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<BruktborsenDbContext>();

        var offer = await db.Offers.FindAsync(offerId);

        Assert.NotNull(offer);
        Assert.Equal(750m, offer.Amount);
        Assert.Equal(OfferStatus.Pending, offer.Status);
    }
    
    [Fact]
    public async Task CreateOffer_AfterWithdrawal_ReturnsCreated()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@example.com");

        using var buyer = await CreateUserClientAsync(
            factory, "buyer@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);

        var firstOfferId = await CreateOfferAsync(
            buyer, listingId, 1000m);

        var response = await buyer.PostAsync(
            $"/api/offers/{firstOfferId}/withdraw", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var secondOfferId = await CreateOfferAsync(
            buyer, listingId, 1200m);

        Assert.NotEqual(firstOfferId, secondOfferId);

        Assert.Equal(
            OfferStatus.Withdrawn,
            await GetOfferStatusAsync(factory, firstOfferId));

        Assert.Equal(
            OfferStatus.Pending,
            await GetOfferStatusAsync(factory, secondOfferId));
    }
    
    [Fact]
    public async Task CreateOffer_OnRemovedListing_ReturnsConflict()
    {
        await using var factory = new AuthTestFactory();

        using var seller = await CreateUserClientAsync(
            factory, "seller@example.com");

        using var buyer = await CreateUserClientAsync(
            factory, "buyer@example.com");

        var categoryId = await CreateCategoryAsync(factory);
        var listingId = await CreateListingAsync(seller, categoryId);

        var deleteResponse = await seller.DeleteAsync(
            $"/api/listings/{listingId}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var response = await buyer.PostAsJsonAsync(
            $"/api/listings/{listingId}/offers",
            new
            {
                Amount = 1200m
            });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<BruktborsenDbContext>();

        Assert.False(await db.Offers.AnyAsync(
            o => o.ListingId == listingId));
    }
}