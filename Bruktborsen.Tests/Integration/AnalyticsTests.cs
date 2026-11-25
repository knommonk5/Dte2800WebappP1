using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Bruktborsen.Api.Data;
using Bruktborsen.Api.DTOs.Analytics;
using Bruktborsen.Api.DTOs.Listings;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bruktborsen.Tests.Integration;

public class AnalyticsTests
{
    private static async Task<HttpClient> CreateUserClientAsync(
        AuthTestFactory factory,
        string email = "analytics@example.com")
    {
        var client = factory.CreateClient();

        const string password = "Test123!Abc";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                FullName = "Analytics Test",
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

    private static async Task<(
        int CategoryId,
        int SmallCategoryId,
        int LowId,
        int NormalId,
        int HighId,
        int UnknownId)> SeedAnalyticsDataAsync(
            AuthTestFactory factory)
    {
        using var scope = factory.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<BruktborsenDbContext>();

        var seller = new ApplicationUser
        {
            FullName = "Test Selger",
            UserName = "seed-seller@example.com",
            Email = "seed-seller@example.com"
        };

        var category = new Category
        {
            Name = "Elektronikk"
        };

        var smallCategory = new Category
        {
            Name = "Møbler"
        };

        db.Users.Add(seller);
        db.Categories.AddRange(category, smallCategory);

        await db.SaveChangesAsync();

        var createdDate = new DateTime(
            2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        Listing CreateListing(
            string title,
            int price,
            int categoryId,
            ListingStatus status = ListingStatus.Active,
            decimal? soldPrice = null,
            int daysToSell = 5)
        {
            return new Listing
            {
                Title = title,
                Description = "Testannonse for analyse.",
                Price = price,
                OriginalPrice = price,
                Condition = Condition.New,
                Status = status,
                CategoryId = categoryId,
                SellerId = seller.Id,
                CreatedDate = createdDate,
                SoldPrice = soldPrice,
                SoldDate = status == ListingStatus.Sold
                    ? createdDate.AddDays(daysToSell)
                    : null
            };
        }

        // Fem salg i hovedkategorien.
        db.Listings.AddRange(
            CreateListing(
                "Salg 1", 1000, category.Id,
                ListingStatus.Sold, 800m),

            CreateListing(
                "Salg 2", 1000, category.Id,
                ListingStatus.Sold, 900m),

            CreateListing(
                "Salg 3", 1000, category.Id,
                ListingStatus.Sold, 1000m),

            CreateListing(
                "Salg 4", 1200, category.Id,
                ListingStatus.Sold, 1100m),

            CreateListing(
                "Salg 5", 1500, category.Id,
                ListingStatus.Sold, 1200m)
        );

        // Kun to salg i den andre kategorien.
        db.Listings.AddRange(
            CreateListing(
                "Møbelsalg 1", 1000, smallCategory.Id,
                ListingStatus.Sold, 800m),

            CreateListing(
                "Møbelsalg 2", 1200, smallCategory.Id,
                ListingStatus.Sold, 1000m)
        );

        var low = CreateListing(
            "Lav pris", 500, category.Id);

        var normal = CreateListing(
            "Normal pris", 1000, category.Id);

        var high = CreateListing(
            "Høy pris", 1500, category.Id);

        var unknown = CreateListing(
            "Ukjent prisvurdering", 1000, smallCategory.Id);

        var removed = CreateListing(
            "Fjernet annonse", 1000, category.Id,
            ListingStatus.Removed);

        db.Listings.AddRange(
            low, normal, high, unknown, removed);

        await db.SaveChangesAsync();

        return (
            category.Id,
            smallCategory.Id,
            low.Id,
            normal.Id,
            high.Id,
            unknown.Id);
    }

    [Fact]
    public async Task GetPriceSuggestion_WithoutLogin_ReturnsUnauthorized()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/analytics/price-suggestion?categoryId=1&condition=New");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetPriceSuggestion_WithValidData_ReturnsSuggestion()
    {
        await using var factory = new AuthTestFactory();

        using var client = await CreateUserClientAsync(factory);

        var data = await SeedAnalyticsDataAsync(factory);

        var result = await client.GetFromJsonAsync<PriceSuggestionDto>(
            $"/api/analytics/price-suggestion?categoryId={data.CategoryId}&condition=Good");

        Assert.NotNull(result);
        Assert.True(result.HasSufficientData);
        Assert.Equal(5, result.SampleSize);
        Assert.Equal(1000m, result.MedianNormalizedPrice);
        Assert.Equal(700m, result.SuggestedPrice);
        Assert.Equal(Condition.Good, result.Condition);
    }

    [Fact]
    public async Task GetPriceSuggestion_WithInsufficientData_ReturnsNoSuggestion()
    {
        await using var factory = new AuthTestFactory();

        using var client = await CreateUserClientAsync(factory);

        var data = await SeedAnalyticsDataAsync(factory);

        var result = await client.GetFromJsonAsync<PriceSuggestionDto>(
            $"/api/analytics/price-suggestion?categoryId={data.SmallCategoryId}&condition=New");

        Assert.NotNull(result);
        Assert.False(result.HasSufficientData);
        Assert.Equal(2, result.SampleSize);
        Assert.Null(result.SuggestedPrice);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }

    [Theory]
    [InlineData("categoryId=0&condition=New")]
    [InlineData("categoryId=-1&condition=New")]
    [InlineData("categoryId=1&condition=999")]
    [InlineData("categoryId=1&condition=Invalid")]
    [InlineData("categoryId=1")]
    public async Task GetPriceSuggestion_WithInvalidParameters_ReturnsBadRequest(
        string query)
    {
        await using var factory = new AuthTestFactory();

        using var client = await CreateUserClientAsync(factory);

        var response = await client.GetAsync(
            $"/api/analytics/price-suggestion?{query}");

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetPriceSuggestion_WithUnknownCategory_ReturnsNotFound()
    {
        await using var factory = new AuthTestFactory();

        using var client = await CreateUserClientAsync(factory);

        var response = await client.GetAsync(
            "/api/analytics/price-suggestion?categoryId=99999&condition=New");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetCategoryStats_WithoutLogin_ReturnsStatistics()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var data = await SeedAnalyticsDataAsync(factory);

        var result = await client.GetFromJsonAsync<CategoryStatsDto>(
            $"/api/analytics/categories/{data.CategoryId}/stats");

        Assert.NotNull(result);
        Assert.Equal(data.CategoryId, result.CategoryId);
        Assert.Equal("Elektronikk", result.CategoryName);

        Assert.Equal(9, result.TotalListings);
        Assert.Equal(5, result.SoldListings);
        Assert.Equal(3, result.ActiveListings);
        Assert.Equal(1, result.RemovedListings);

        Assert.Equal(1000m, result.AverageSoldPrice);
        Assert.Equal(1000m, result.MedianSoldPrice);
        Assert.Equal(5m, result.AverageDaysToSell);
        Assert.Equal(5m / 9m, result.SaleRatio);

        Assert.NotNull(result.StandardDeviation);

        Assert.Equal(
            158.11m,
            Math.Round(result.StandardDeviation.Value, 2));
    }

    [Fact]
    public async Task GetCategoryStats_WithUnknownCategory_ReturnsNotFound()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/analytics/categories/99999/stats");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetOverview_WithoutLogin_ReturnsMarketStatistics()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        await SeedAnalyticsDataAsync(factory);

        var result = await client.GetFromJsonAsync<MarketOverviewDto>(
            "/api/analytics/overview");

        Assert.NotNull(result);

        Assert.Equal(2, result.TotalCategories);
        Assert.Equal(12, result.TotalListings);
        Assert.Equal(7, result.SoldListings);
        Assert.Equal(4, result.ActiveListings);
        Assert.Equal(1, result.RemovedListings);

        Assert.Equal(6800m, result.TotalSalesValue);
        Assert.Equal(6800m / 7m, result.AverageSoldPrice);
        Assert.Equal(5m, result.AverageDaysToSell);
        Assert.Equal(7m / 12m, result.SaleRatio);

        Assert.Equal(2, result.Categories.Count);
    }

    [Fact]
    public async Task GetOverview_WithEmptyDatabase_ReturnsEmptyStatistics()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var result = await client.GetFromJsonAsync<MarketOverviewDto>(
            "/api/analytics/overview");

        Assert.NotNull(result);

        Assert.Equal(0, result.TotalCategories);
        Assert.Equal(0, result.TotalListings);
        Assert.Equal(0, result.SoldListings);
        Assert.Equal(0m, result.TotalSalesValue);

        Assert.Null(result.AverageSoldPrice);
        Assert.Null(result.AverageDaysToSell);
        Assert.Null(result.SaleRatio);

        Assert.Empty(result.Categories);
    }

    [Fact]
    public async Task GetListingDetails_ReturnsCorrectPriceAssessments()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var data = await SeedAnalyticsDataAsync(factory);

        var low = await client.GetFromJsonAsync<ListingDto>(
            $"/api/listings/{data.LowId}");

        var normal = await client.GetFromJsonAsync<ListingDto>(
            $"/api/listings/{data.NormalId}");

        var high = await client.GetFromJsonAsync<ListingDto>(
            $"/api/listings/{data.HighId}");

        var unknown = await client.GetFromJsonAsync<ListingDto>(
            $"/api/listings/{data.UnknownId}");

        Assert.NotNull(low);
        Assert.NotNull(normal);
        Assert.NotNull(high);
        Assert.NotNull(unknown);

        Assert.Equal("Low", low.PriceAssessment);
        Assert.Equal("Normal", normal.PriceAssessment);
        Assert.Equal("High", high.PriceAssessment);
        Assert.Equal("Unknown", unknown.PriceAssessment);
    }

    [Fact]
    public async Task GetListings_ReturnsCalculatedPriceAssessments()
    {
        await using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var data = await SeedAnalyticsDataAsync(factory);

        var listings = await client.GetFromJsonAsync<List<ListingDto>>(
            "/api/listings");

        Assert.NotNull(listings);
        Assert.Equal(4, listings.Count);

        Assert.All(listings, listing =>
            Assert.Equal(ListingStatus.Active, listing.Status));

        Assert.Contains(listings, l =>
            l.Id == data.LowId &&
            l.PriceAssessment == "Low");

        Assert.Contains(listings, l =>
            l.Id == data.NormalId &&
            l.PriceAssessment == "Normal");

        Assert.Contains(listings, l =>
            l.Id == data.HighId &&
            l.PriceAssessment == "High");

        Assert.Contains(listings, l =>
            l.Id == data.UnknownId &&
            l.PriceAssessment == "Unknown");
    }

    [Fact]
    public async Task GetListingDetails_ReturnsCorrectOfferCount()
    {
        await using var factory = new AuthTestFactory();

        using var buyer1 = await CreateUserClientAsync(
            factory, "buyer1@example.com");

        using var buyer2 = await CreateUserClientAsync(
            factory, "buyer2@example.com");

        var data = await SeedAnalyticsDataAsync(factory);

        var firstOffer = await buyer1.PostAsJsonAsync(
            $"/api/listings/{data.NormalId}/offers",
            new { Amount = 800m });

        var secondOffer = await buyer2.PostAsJsonAsync(
            $"/api/listings/{data.NormalId}/offers",
            new { Amount = 900m });

        Assert.Equal(
            HttpStatusCode.Created,
            firstOffer.StatusCode);

        Assert.Equal(
            HttpStatusCode.Created,
            secondOffer.StatusCode);

        using var client = factory.CreateClient();

        var listing = await client.GetFromJsonAsync<ListingDto>(
            $"/api/listings/{data.NormalId}");

        Assert.NotNull(listing);
        Assert.Equal(2, listing.OfferCount);
        Assert.Equal("Normal", listing.PriceAssessment);
    }
}