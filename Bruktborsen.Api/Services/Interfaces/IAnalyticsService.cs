using Bruktborsen.Api.DTOs.Analytics;
using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.Services.Interfaces;

public interface IAnalyticsService
{
    Task<PriceSuggestionDto> GetPriceSuggestionAsync(
        int categoryId,
        Condition condition);

    Task<PriceAssessmentDto> GetPriceAssessmentAsync(
        int categoryId,
        decimal price,
        Condition condition);

    Task<CategoryStatsDto> GetCategoryStatsAsync(
        int categoryId);

    Task<MarketOverviewDto> GetOverviewAsync();
}