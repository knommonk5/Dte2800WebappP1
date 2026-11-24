using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Bruktborsen.Api.Data;
using Bruktborsen.Api.DTOs.Listings;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bruktborsen.Tests.Integration;

public class ListingSearchTests
{
    private const string Password = "Test123!Abc";

    [Fact]
    public async Task SearchListings_ByTitle_ReturnsMatchingListings()
    {
        await using var factory = new AuthTestFactory();
        await SeedListingsAsync(factory);

        using var client = factory.CreateClient();

        var listings = await GetListingsAsync(
            client, "?search=PlayStation");

        var listing = Assert.Single(listings);

        Assert.Equal("PlayStation 5", listing.Title);
    }

    [Fact]
    public async Task SearchListings_ByDescription_ReturnsMatchingListings()
    {
        await using var factory = new AuthTestFactory();
        await SeedListingsAsync(factory);

        using var client = factory.CreateClient();

        var listings = await GetListingsAsync(
            client, "?search=kontroller");

        var listing = Assert.Single(listings);

        Assert.Equal("PlayStation 5", listing.Title);
    }

    [Fact]
    public async Task SearchListings_CaseInsensitive_ReturnsMatchingListings()
    {
        await using var factory = new AuthTestFactory();
        await SeedListingsAsync(factory);

        using var client = factory.CreateClient();

        var listings = await GetListingsAsync(
            client, "?search=PLAYSTATION");

        var listing = Assert.Single(listings);

        Assert.Equal("PlayStation 5", listing.Title);
    }

    [Fact]
    public async Task SearchListings_NoMatches_ReturnsEmptyList()
    {
        await using var factory = new AuthTestFactory();
        await SeedListingsAsync(factory);

        using var client = factory.CreateClient();

        var listings = await GetListingsAsync(
            client, "?search=sykkel");

        Assert.Empty(listings);
    }

    [Fact]
    public async Task FilterListings_ByCategory_ReturnsMatchingListings()
    {
        await using var factory = new AuthTestFactory();
        var categories = await SeedListingsAsync(factory);

        using var client = factory.CreateClient();

        var listings = await GetListingsAsync(
            client, $"?categoryId={categories.Electronics}");

        Assert.Equal(3, listings.Count);

        Assert.All(listings, listing =>
            Assert.Equal(
                categories.Electronics,
                listing.CategoryId));
    }

    [Fact]
    public async Task FilterListings_UnknownCategory_ReturnsEmptyList()
    {
        await using var factory = new AuthTestFactory();
        await SeedListingsAsync(factory);

        using var client = factory.CreateClient();

        var listings = await GetListingsAsync(
            client, "?categoryId=999999");

        Assert.Empty(listings);
    }

    [Fact]
    public async Task FilterListings_ByMinimumPrice_ReturnsMatchingListings()
    {
        await using var factory = new AuthTestFactory();
        await SeedListingsAsync(factory);

        using var client = factory.CreateClient();

        var listings = await GetListingsAsync(
            client, "?minPrice=1800");

        Assert.Equal(3, listings.Count);

        Assert.All(listings, listing =>
            Assert.True(listing.Price >= 1800m));
    }

    [Fact]
    public async Task FilterListings_ByMaximumPrice_ReturnsMatchingListings()
    {
        await using var factory = new AuthTestFactory();
        await SeedListingsAsync(factory);

        using var client = factory.CreateClient();

        var listings = await GetListingsAsync(
            client, "?maxPrice=900");

        Assert.Equal(2, listings.Count);

        Assert.All(listings, listing =>
            Assert.True(listing.Price <= 900m));
    }

    [Fact]
    public async Task FilterListings_ByPriceRange_ReturnsMatchingListings()
    {
        await using var factory = new AuthTestFactory();
        await SeedListingsAsync(factory);

        using var client = factory.CreateClient();

        var listings = await GetListingsAsync(
            client, "?minPrice=900&maxPrice=2800");

        Assert.Equal(3, listings.Count);

        Assert.All(listings, listing =>
            Assert.InRange(listing.Price, 900m, 2800m));
    }

    [Fact]
    public async Task FilterListings_ByCondition_ReturnsMatchingListings()
    {
        await using var factory = new AuthTestFactory();
        await SeedListingsAsync(factory);

        using var client = factory.CreateClient();

        var listings = await GetListingsAsync(
            client, "?condition=Good");

        Assert.Equal(2, listings.Count);

        Assert.All(listings, listing =>
            Assert.Equal(Condition.Good, listing.Condition));
    }

    [Fact]
    public async Task FilterListings_CombinedFilters_ReturnsMatchingListings()
    {
        await using var factory = new AuthTestFactory();
        var categories = await SeedListingsAsync(factory);

        using var client = factory.CreateClient();

        var listings = await GetListingsAsync(
            client,
            $"?categoryId={categories.Electronics}" +
            "&search=konsoll" +
            "&minPrice=1000" +
            "&maxPrice=3000" +
            "&condition=Good");

        var listing = Assert.Single(listings);

        Assert.Equal("Nintendo Switch", listing.Title);
        Assert.Equal(1800m, listing.Price);
    }

    [Fact]
    public async Task SortListings_ByPriceAscending_ReturnsOrderedListings()
    {
        await using var factory = new AuthTestFactory();
        await SeedListingsAsync(factory);

        using var client = factory.CreateClient();

        var listings = await GetListingsAsync(
            client, "?sortBy=price&sortOrder=asc");

        Assert.Equal(
            new[] { 600m, 900m, 1800m, 2800m, 3500m },
            listings.Select(l => l.Price).ToArray());
    }

    [Fact]
    public async Task SortListings_ByPriceDescending_ReturnsOrderedListings()
    {
        await using var factory = new AuthTestFactory();
        await SeedListingsAsync(factory);

        using var client = factory.CreateClient();

        var listings = await GetListingsAsync(
            client, "?sortBy=price&sortOrder=desc");

        Assert.Equal(
            new[] { 3500m, 2800m, 1800m, 900m, 600m },
            listings.Select(l => l.Price).ToArray());
    }

    [Fact]
    public async Task SortListings_ByTitleAscending_ReturnsOrderedListings()
    {
        await using var factory = new AuthTestFactory();
        await SeedListingsAsync(factory);

        using var client = factory.CreateClient();

        var listings = await GetListingsAsync(
            client, "?sortBy=title&sortOrder=asc");

        Assert.Equal(
            new[]
            {
                "Gamingstol",
                "Nintendo Switch",
                "PlayStation 5",
                "Skrivebord",
                "Xbox Series X"
            },
            listings.Select(l => l.Title).ToArray());
    }

    [Fact]
    public async Task SortListings_ByTitleDescending_ReturnsOrderedListings()
    {
        await using var factory = new AuthTestFactory();
        await SeedListingsAsync(factory);

        using var client = factory.CreateClient();

        var listings = await GetListingsAsync(
            client, "?sortBy=title&sortOrder=desc");

        Assert.Equal(
            new[]
            {
                "Xbox Series X",
                "Skrivebord",
                "PlayStation 5",
                "Nintendo Switch",
                "Gamingstol"
            },
            listings.Select(l => l.Title).ToArray());
    }

    [Fact]
    public async Task SortListings_ByCreatedAtAscending_ReturnsOldestFirst()
    {
        await using var factory = new AuthTestFactory();
        await SeedListingsAsync(factory);

        using var client = factory.CreateClient();

        var listings = await GetListingsAsync(
            client, "?sortBy=createdAt&sortOrder=asc");

        Assert.Equal(
            new[]
            {
                "PlayStation 5",
                "Nintendo Switch",
                "Xbox Series X",
                "Gamingstol",
                "Skrivebord"
            },
            listings.Select(l => l.Title).ToArray());
    }

    [Fact]
    public async Task SortListings_DefaultSort_ReturnsNewestFirst()
    {
        await using var factory = new AuthTestFactory();
        await SeedListingsAsync(factory);

        using var client = factory.CreateClient();

        var listings = await GetListingsAsync(client);

        Assert.Equal(
            new[]
            {
                "Skrivebord",
                "Gamingstol",
                "Xbox Series X",
                "Nintendo Switch",
                "PlayStation 5"
            },
            listings.Select(l => l.Title).ToArray());
    }

    [Fact]
    public async Task SortListings_EqualPrices_ReturnsStableOrder()
    {
        await using var factory = new AuthTestFactory();
        await SeedListingsAsync(factory);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<BruktborsenDbContext>();

            foreach (var listing in db.Listings.ToList())
            {
                listing.Price = 1000m;
            }

            await db.SaveChangesAsync();
        }

        using var client = factory.CreateClient();

        var listings = await GetListingsAsync(
            client, "?sortBy=price&sortOrder=asc");

        Assert.Equal(5, listings.Count);

        Assert.Equal(
            listings.Select(l => l.Id).OrderBy(id => id).ToArray(),
            listings.Select(l => l.Id).ToArray());
    }

    [Fact]
    public async Task SearchListings_SoldListing_IsExcluded()
    {
        await using var factory = new AuthTestFactory();
        await SeedListingsAsync(factory);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<BruktborsenDbContext>();

            var listing = db.Listings.Single(
                l => l.Title == "PlayStation 5");

            listing.Status = ListingStatus.Sold;
            listing.SoldAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
        }

        using var client = factory.CreateClient();

        var listings = await GetListingsAsync(
            client, "?search=PlayStation");

        Assert.Empty(listings);
    }

    [Fact]
    public async Task SearchListings_RemovedListing_IsExcluded()
    {
        await using var factory = new AuthTestFactory();
        await SeedListingsAsync(factory);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<BruktborsenDbContext>();

            var listing = db.Listings.Single(
                l => l.Title == "PlayStation 5");

            listing.Status = ListingStatus.Removed;

            await db.SaveChangesAsync();
        }

        using var client = factory.CreateClient();

        var listings = await GetListingsAsync(
            client, "?search=PlayStation");

        Assert.Empty(listings);
    }

    [Theory]
    [InlineData("?minPrice=-100")]
    [InlineData("?maxPrice=-100")]
    [InlineData("?minPrice=3000&maxPrice=1000")]
    [InlineData("?categoryId=0")]
    [InlineData("?categoryId=-1")]
    [InlineData("?condition=999")]
    [InlineData("?condition=Invalid")]
    [InlineData("?sortBy=invalid")]
    [InlineData("?sortOrder=invalid")]
    [InlineData("?minPrice=abc")]
    public async Task GetListings_InvalidQuery_ReturnsBadRequest(
        string query)
    {
        await using var factory = new AuthTestFactory();

        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            $"/api/listings{query}");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task SearchListings_SearchTooLong_ReturnsBadRequest()
    {
        await using var factory = new AuthTestFactory();

        using var client = factory.CreateClient();

        var search = new string('a', 201);

        var response = await client.GetAsync(
            $"/api/listings?search={search}");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
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

    private static async Task<(int Electronics, int Furniture)>
        SeedListingsAsync(AuthTestFactory factory)
    {
        using var seller = await CreateUserClientAsync(
            factory, "seller@test.no");

        int electronicsId;
        int furnitureId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<BruktborsenDbContext>();

            var electronics = new Category
            {
                Name = "Elektronikk"
            };

            var furniture = new Category
            {
                Name = "Møbler"
            };

            db.Categories.AddRange(electronics, furniture);

            await db.SaveChangesAsync();

            electronicsId = electronics.Id;
            furnitureId = furniture.Id;
        }

        var listingIds = new List<int>();

        listingIds.Add(await CreateListingAsync(
            seller,
            electronicsId,
            "PlayStation 5",
            "Pent brukt konsoll med to kontroller.",
            3500m,
            Condition.LikeNew));

        listingIds.Add(await CreateListingAsync(
            seller,
            electronicsId,
            "Nintendo Switch",
            "Baerbar konsoll med original emballasje.",
            1800m,
            Condition.Good));

        listingIds.Add(await CreateListingAsync(
            seller,
            electronicsId,
            "Xbox Series X",
            "Brukt konsoll med strømkabel.",
            2800m,
            Condition.Used));

        listingIds.Add(await CreateListingAsync(
            seller,
            furnitureId,
            "Gamingstol",
            "Komfortabel stol med justerbar rygg.",
            900m,
            Condition.Good));

        listingIds.Add(await CreateListingAsync(
            seller,
            furnitureId,
            "Skrivebord",
            "Solid skrivebord med god arbeidsplass.",
            600m,
            Condition.Used));

        // Faste publiseringsdatoer for deterministiske tester.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<BruktborsenDbContext>();

            var startDate = new DateTime(
                2026, 1, 1, 12, 0, 0,
                DateTimeKind.Utc);

            for (var i = 0; i < listingIds.Count; i++)
            {
                var listing = await db.Listings.FindAsync(
                    listingIds[i]);

                Assert.NotNull(listing);

                listing!.CreatedAt = startDate.AddDays(i);
            }

            await db.SaveChangesAsync();
        }

        return (electronicsId, furnitureId);
    }

    private static async Task<int> CreateListingAsync(
        HttpClient client,
        int categoryId,
        string title,
        string description,
        decimal price,
        Condition condition)
    {
        var response = await client.PostAsJsonAsync(
            "/api/listings",
            new
            {
                Title = title,
                Description = description,
                Price = price,
                OriginalPrice = price * 2,
                Condition = condition,
                CategoryId = categoryId
            });

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        using var json = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync());

        return json.RootElement
            .GetProperty("id")
            .GetInt32();
    }

    private static async Task<List<ListingDto>> GetListingsAsync(
        HttpClient client,
        string query = "")
    {
        var response = await client.GetAsync(
            $"/api/listings{query}");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var listings = await response.Content
            .ReadFromJsonAsync<List<ListingDto>>();

        Assert.NotNull(listings);

        return listings!;
    }
}