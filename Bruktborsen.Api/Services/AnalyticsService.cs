using Bruktborsen.Api.DTOs.Analytics;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Repositories.Interfaces;
using Bruktborsen.Api.Services.Analytics;
using Bruktborsen.Api.Services.Interfaces;

namespace Bruktborsen.Api.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IAnalyticsRepository _repository;

    public AnalyticsService(IAnalyticsRepository repository)
    {
        _repository = repository;
    }

    public async Task<PriceSuggestionDto> GetPriceSuggestionAsync(
        int categoryId,
        Condition condition)
    {
        await EnsureCategoryExistsAsync(categoryId);

        var factor =
            PriceNormalizationHelper.GetConditionFactor(condition);

        var listings =
            await _repository.GetSoldListingsByCategoryAsync(categoryId);

        var normalizedPrices = GetNormalizedPrices(listings);

        var result = new PriceSuggestionDto
        {
            CategoryId = categoryId,
            Condition = condition,
            SampleSize = normalizedPrices.Length,
            HasSufficientData = normalizedPrices.Length >= 3
        };

        if (!result.HasSufficientData)
        {
            result.Message =
                "Ikke nok data. Minst tre salg kreves.";

            return result;
        }

        var median = StatisticsHelper.Median(normalizedPrices);

        result.MedianNormalizedPrice = median;

        result.SuggestedPrice =
            StatisticsHelper.RoundToNearestTen(median * factor);

        return result;
    }

    public async Task<PriceAssessmentDto> GetPriceAssessmentAsync(
        int categoryId,
        decimal price,
        Condition condition)
    {
        await EnsureCategoryExistsAsync(categoryId);

        var normalizedPrice =
            PriceNormalizationHelper.NormalizePrice(price, condition);

        var listings =
            await _repository.GetSoldListingsByCategoryAsync(categoryId);

        var normalizedPrices = GetNormalizedPrices(listings);

        var result = new PriceAssessmentDto
        {
            SampleSize = normalizedPrices.Length,
            NormalizedPrice = normalizedPrice,
            Assessment = "Unknown"
        };

        if (normalizedPrices.Length < 5)
            return result;

        var q1 = StatisticsHelper.FirstQuartile(normalizedPrices);
        var q3 = StatisticsHelper.ThirdQuartile(normalizedPrices);

        var iqr = q3 - q1;

        var lowerBound = q1 - 1.5m * iqr;
        var upperBound = q3 + 1.5m * iqr;

        result.FirstQuartile = q1;
        result.ThirdQuartile = q3;
        result.InterquartileRange = iqr;
        result.LowerBound = lowerBound;
        result.UpperBound = upperBound;

        result.Assessment =
            normalizedPrice < lowerBound ? "Low" :
            normalizedPrice > upperBound ? "High" :
            "Normal";

        return result;
    }

    public async Task<CategoryStatsDto> GetCategoryStatsAsync(
        int categoryId)
    {
        var categories = await _repository.GetCategoriesAsync();

        var category = categories
            .FirstOrDefault(c => c.Id == categoryId);

        if (category is null)
            throw new KeyNotFoundException("Kategorien finnes ikke.");

        var listings =
            await _repository.GetListingsByCategoryAsync(categoryId);

        return BuildCategoryStats(category, listings);
    }

    public async Task<MarketOverviewDto> GetOverviewAsync()
    {
        var categories = await _repository.GetCategoriesAsync();
        var listings = await _repository.GetAllListingsAsync();

        var soldListings = listings
            .Where(l => l.Status == ListingStatus.Sold)
            .ToList();

        var soldPrices = GetValidSoldPrices(soldListings);

        return new MarketOverviewDto
        {
            TotalCategories = categories.Count,
            TotalListings = listings.Count,

            ActiveListings = listings.Count(
                l => l.Status == ListingStatus.Active),

            SoldListings = soldListings.Count,

            RemovedListings = listings.Count(
                l => l.Status == ListingStatus.Removed),

            TotalSalesValue = soldPrices.Sum(),

            AverageSoldPrice = soldPrices.Length > 0
                ? StatisticsHelper.Mean(soldPrices)
                : null,

            AverageDaysToSell =
                CalculateAverageDaysToSell(soldListings),

            SaleRatio = listings.Count > 0
                ? (decimal)soldListings.Count / listings.Count
                : null,

            Categories = categories.Select(category =>
                BuildCategoryStats(
                    category,
                    listings.Where(l =>
                        l.CategoryId == category.Id).ToList()))
                .ToList()
        };
    }

    private static CategoryStatsDto BuildCategoryStats(
        Category category,
        List<Listing> listings)
    {
        var soldListings = listings
            .Where(l => l.Status == ListingStatus.Sold)
            .ToList();

        var soldPrices = GetValidSoldPrices(soldListings);

        return new CategoryStatsDto
        {
            CategoryId = category.Id,
            CategoryName = category.Name,

            TotalListings = listings.Count,

            ActiveListings = listings.Count(
                l => l.Status == ListingStatus.Active),

            SoldListings = soldListings.Count,

            RemovedListings = listings.Count(
                l => l.Status == ListingStatus.Removed),

            AverageSoldPrice = soldPrices.Length > 0
                ? StatisticsHelper.Mean(soldPrices)
                : null,

            MedianSoldPrice = soldPrices.Length > 0
                ? StatisticsHelper.Median(soldPrices)
                : null,

            StandardDeviation = soldPrices.Length >= 2
                ? StatisticsHelper.SampleStandardDeviation(soldPrices)
                : null,

            AverageDaysToSell =
                CalculateAverageDaysToSell(soldListings),

            AverageDiscountPercentage =
                CalculateAverageDiscount(soldListings),

            SaleRatio = listings.Count > 0
                ? (decimal)soldListings.Count / listings.Count
                : null
        };
    }

    private static decimal[] GetNormalizedPrices(
        IEnumerable<Listing> listings)
    {
        return listings
            .Where(l => l.Status == ListingStatus.Sold &&
                        l.SoldPrice.HasValue &&
                        l.SoldPrice.Value > 0 &&
                        Enum.IsDefined(l.Condition))
            .Select(l => PriceNormalizationHelper.NormalizePrice(
                l.SoldPrice!.Value,
                l.Condition))
            .ToArray();
    }

    private static decimal[] GetValidSoldPrices(
        IEnumerable<Listing> listings)
    {
        return listings
            .Where(l => l.Status == ListingStatus.Sold &&
                        l.SoldPrice.HasValue &&
                        l.SoldPrice.Value > 0)
            .Select(l => l.SoldPrice!.Value)
            .ToArray();
    }

    private static decimal? CalculateAverageDaysToSell(
        IEnumerable<Listing> listings)
    {
        var durations = listings
            .Where(l => l.SoldDate.HasValue &&
                        l.SoldDate.Value >= l.CreatedDate)
            .Select(l => (decimal)(
                l.SoldDate!.Value - l.CreatedDate).TotalDays)
            .ToArray();

        return durations.Length > 0
            ? StatisticsHelper.Mean(durations)
            : null;
    }

    private static decimal? CalculateAverageDiscount(
        IEnumerable<Listing> listings)
    {
        var discounts = new List<decimal>();

        foreach (var listing in listings)
        {
            if (!listing.SoldPrice.HasValue ||
                listing.SoldPrice.Value <= 0)
            {
                continue;
            }

            var firstPriceChange = listing.PriceChanges
                .OrderBy(p => p.ChangedDate)
                .ThenBy(p => p.Id)
                .FirstOrDefault();

            var originalPrice =
                firstPriceChange?.OldPrice ?? listing.Price;

            if (originalPrice <= 0)
                continue;

            var discount =
                (originalPrice - listing.SoldPrice.Value)
                / originalPrice * 100m;

            discounts.Add(discount);
        }

        return discounts.Count > 0
            ? StatisticsHelper.Mean(discounts)
            : null;
    }

    private async Task EnsureCategoryExistsAsync(int categoryId)
    {
        if (!await _repository.CategoryExistsAsync(categoryId))
            throw new KeyNotFoundException("Kategorien finnes ikke.");
    }
}