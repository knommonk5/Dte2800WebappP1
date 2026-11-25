using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Repositories.Interfaces;
using Bruktborsen.Api.Services;
using Xunit;

namespace Bruktborsen.Tests.Unit;

public class AnalyticsServiceTests
{
    private static Listing CreateSoldListing(
        decimal soldPrice,
        Condition condition = Condition.New,
        int price = 1000,
        int daysToSell = 5,
        int categoryId = 1)
    {
        var createdDate = new DateTime(
            2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        return new Listing
        {
            CategoryId = categoryId,
            Price = price,
            SoldPrice = soldPrice,
            Condition = condition,
            Status = ListingStatus.Sold,
            CreatedDate = createdDate,
            SoldDate = createdDate.AddDays(daysToSell)
        };
    }

    private static AnalyticsService CreateService(
        params Listing[] listings)
    {
        return new AnalyticsService(
            new TestAnalyticsRepository(listings));
    }

    [Fact]
    public async Task GetPriceSuggestion_NormalizesAndRoundsPrice()
    {
        var service = CreateService(
            CreateSoldListing(800m, Condition.New),
            CreateSoldListing(850m, Condition.LikeNew),
            CreateSoldListing(840m, Condition.Good));

        var result = await service.GetPriceSuggestionAsync(
            1, Condition.LikeNew);

        Assert.True(result.HasSufficientData);
        Assert.Equal(3, result.SampleSize);
        Assert.Equal(1000m, result.MedianNormalizedPrice);
        Assert.Equal(850m, result.SuggestedPrice);
    }

    [Fact]
    public async Task GetPriceSuggestion_RoundsToNearestTen()
    {
        var service = CreateService(
            CreateSoldListing(900m),
            CreateSoldListing(1100m),
            CreateSoldListing(1300m));

        var result = await service.GetPriceSuggestionAsync(
            1, Condition.LikeNew);

        Assert.Equal(1100m, result.MedianNormalizedPrice);
        Assert.Equal(940m, result.SuggestedPrice);
    }

    [Fact]
    public async Task GetPriceSuggestion_WithInsufficientData_ReturnsNoSuggestion()
    {
        var service = CreateService(
            CreateSoldListing(800m),
            CreateSoldListing(1000m));

        var result = await service.GetPriceSuggestionAsync(
            1, Condition.Good);

        Assert.False(result.HasSufficientData);
        Assert.Equal(2, result.SampleSize);
        Assert.Null(result.SuggestedPrice);
        Assert.Null(result.MedianNormalizedPrice);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }

    [Fact]
    public async Task GetPriceSuggestion_WithNoSales_ReturnsInsufficientData()
    {
        var service = CreateService();

        var result = await service.GetPriceSuggestionAsync(
            1, Condition.Good);

        Assert.False(result.HasSufficientData);
        Assert.Equal(0, result.SampleSize);
        Assert.Null(result.SuggestedPrice);
    }

    [Fact]
    public async Task GetPriceSuggestion_ExcludesOtherCategoriesAndActiveListings()
    {
        var service = CreateService(
            CreateSoldListing(800m),
            CreateSoldListing(1000m),
            CreateSoldListing(1200m),
            CreateSoldListing(9000m, categoryId: 2),
            new Listing
            {
                CategoryId = 1,
                Price = 9000,
                Condition = Condition.New,
                Status = ListingStatus.Active
            });

        var result = await service.GetPriceSuggestionAsync(
            1, Condition.New);

        Assert.Equal(3, result.SampleSize);
        Assert.Equal(1000m, result.SuggestedPrice);
    }

    [Theory]
    [InlineData(500, "Low")]
    [InlineData(600, "Normal")]
    [InlineData(1000, "Normal")]
    [InlineData(1400, "Normal")]
    [InlineData(1500, "High")]
    public async Task GetPriceAssessment_ReturnsCorrectAssessment(
        int price,
        string expected)
    {
        var service = CreateService(
            CreateSoldListing(800m),
            CreateSoldListing(900m),
            CreateSoldListing(1000m),
            CreateSoldListing(1100m),
            CreateSoldListing(1200m));

        var result = await service.GetPriceAssessmentAsync(
            1, price, Condition.New);

        Assert.Equal(5, result.SampleSize);
        Assert.Equal(900m, result.FirstQuartile);
        Assert.Equal(1100m, result.ThirdQuartile);
        Assert.Equal(200m, result.InterquartileRange);
        Assert.Equal(600m, result.LowerBound);
        Assert.Equal(1400m, result.UpperBound);
        Assert.Equal(expected, result.Assessment);
    }

    [Fact]
    public async Task GetPriceAssessment_NormalizesAskingPrice()
    {
        var service = CreateService(
            CreateSoldListing(800m),
            CreateSoldListing(900m),
            CreateSoldListing(1000m),
            CreateSoldListing(1100m),
            CreateSoldListing(1200m));

        var result = await service.GetPriceAssessmentAsync(
            1, 700m, Condition.Good);

        Assert.Equal(1000m, result.NormalizedPrice);
        Assert.Equal("Normal", result.Assessment);
    }

    [Fact]
    public async Task GetPriceAssessment_WithFewerThanFiveSales_ReturnsUnknown()
    {
        var service = CreateService(
            CreateSoldListing(800m),
            CreateSoldListing(900m),
            CreateSoldListing(1000m),
            CreateSoldListing(1100m));

        var result = await service.GetPriceAssessmentAsync(
            1, 1000m, Condition.New);

        Assert.Equal(4, result.SampleSize);
        Assert.Equal("Unknown", result.Assessment);
        Assert.Null(result.LowerBound);
        Assert.Null(result.UpperBound);
    }

    [Fact]
    public async Task GetCategoryStats_ReturnsCorrectStatistics()
    {
        var service = CreateService(
            CreateSoldListing(800m, daysToSell: 2),
            CreateSoldListing(900m, daysToSell: 4),
            CreateSoldListing(1000m, daysToSell: 6),
            new Listing
            {
                CategoryId = 1,
                Status = ListingStatus.Active,
                Price = 1500
            },
            new Listing
            {
                CategoryId = 1,
                Status = ListingStatus.Removed,
                Price = 1500
            });

        var result = await service.GetCategoryStatsAsync(1);

        Assert.Equal(5, result.TotalListings);
        Assert.Equal(3, result.SoldListings);
        Assert.Equal(1, result.ActiveListings);
        Assert.Equal(1, result.RemovedListings);

        Assert.Equal(900m, result.AverageSoldPrice);
        Assert.Equal(900m, result.MedianSoldPrice);
        Assert.Equal(100m, result.StandardDeviation);
        Assert.Equal(4m, result.AverageDaysToSell);
        Assert.Equal(10m, result.AverageDiscountPercentage);
        Assert.Equal(0.6m, result.SaleRatio);
    }

    [Fact]
    public async Task GetCategoryStats_DiscountUsesFirstHistoricalPrice()
    {
        var listing = CreateSoldListing(
            600m, price: 800);

        listing.PriceChanges.Add(new PriceChange
        {
            Id = 2,
            OldPrice = 900m,
            NewPrice = 800m,
            ChangedDate = listing.CreatedDate.AddDays(2)
        });

        listing.PriceChanges.Add(new PriceChange
        {
            Id = 1,
            OldPrice = 1000m,
            NewPrice = 900m,
            ChangedDate = listing.CreatedDate.AddDays(1)
        });

        var service = CreateService(
            listing,
            CreateSoldListing(800m, price: 1000));

        var result = await service.GetCategoryStatsAsync(1);

        Assert.Equal(
            30m,
            result.AverageDiscountPercentage);
    }

    [Fact]
    public async Task GetCategoryStats_WithNoListings_ReturnsEmptyStatistics()
    {
        var service = CreateService();

        var result = await service.GetCategoryStatsAsync(1);

        Assert.Equal(0, result.TotalListings);
        Assert.Equal(0, result.SoldListings);
        Assert.Null(result.AverageSoldPrice);
        Assert.Null(result.MedianSoldPrice);
        Assert.Null(result.StandardDeviation);
        Assert.Null(result.AverageDaysToSell);
        Assert.Null(result.AverageDiscountPercentage);
        Assert.Null(result.SaleRatio);
    }

    [Fact]
    public async Task GetOverview_ReturnsCombinedMarketStatistics()
    {
        var service = CreateService(
            CreateSoldListing(800m, daysToSell: 2),
            CreateSoldListing(
                1200m, daysToSell: 6, categoryId: 2),
            new Listing
            {
                CategoryId = 1,
                Status = ListingStatus.Active,
                Price = 1500
            },
            new Listing
            {
                CategoryId = 2,
                Status = ListingStatus.Removed,
                Price = 1500
            });

        var result = await service.GetOverviewAsync();

        Assert.Equal(2, result.TotalCategories);
        Assert.Equal(4, result.TotalListings);
        Assert.Equal(2, result.SoldListings);
        Assert.Equal(1, result.ActiveListings);
        Assert.Equal(1, result.RemovedListings);
        Assert.Equal(2000m, result.TotalSalesValue);
        Assert.Equal(1000m, result.AverageSoldPrice);
        Assert.Equal(4m, result.AverageDaysToSell);
        Assert.Equal(0.5m, result.SaleRatio);
        Assert.Equal(2, result.Categories.Count);
    }

    [Fact]
    public async Task GetPriceSuggestion_WithUnknownCategory_ThrowsException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetPriceSuggestionAsync(
                999, Condition.New));
    }

    [Fact]
    public async Task GetPriceAssessment_WithUnknownCategory_ThrowsException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetPriceAssessmentAsync(
                999, 1000m, Condition.New));
    }

    [Fact]
    public async Task GetCategoryStats_WithUnknownCategory_ThrowsException()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetCategoryStatsAsync(999));
    }

    private sealed class TestAnalyticsRepository
        : IAnalyticsRepository
    {
        private readonly List<Listing> _listings;

        private readonly List<Category> _categories =
        [
            new Category { Id = 1, Name = "Elektronikk" },
            new Category { Id = 2, Name = "Møbler" }
        ];

        public TestAnalyticsRepository(
            IEnumerable<Listing> listings)
        {
            _listings = listings.ToList();
        }

        public Task<bool> CategoryExistsAsync(int categoryId)
        {
            return Task.FromResult(
                _categories.Any(c => c.Id == categoryId));
        }

        public Task<List<Category>> GetCategoriesAsync()
        {
            return Task.FromResult(_categories.ToList());
        }

        public Task<List<Listing>> GetSoldListingsByCategoryAsync(
            int categoryId)
        {
            return Task.FromResult(
                _listings.Where(l =>
                    l.CategoryId == categoryId &&
                    l.Status == ListingStatus.Sold &&
                    l.SoldPrice.HasValue)
                .ToList());
        }

        public Task<List<Listing>> GetListingsByCategoryAsync(
            int categoryId)
        {
            return Task.FromResult(
                _listings.Where(l =>
                    l.CategoryId == categoryId)
                .ToList());
        }

        public Task<List<Listing>> GetAllListingsAsync()
        {
            return Task.FromResult(_listings.ToList());
        }
    }
}