namespace Bruktborsen.Api.DTOs.Analytics;

public class MarketOverviewDto
{
    public int TotalCategories { get; set; }
    public int TotalListings { get; set; }
    public int ActiveListings { get; set; }
    public int SoldListings { get; set; }
    public int RemovedListings { get; set; }

    public decimal TotalSalesValue { get; set; }
    public decimal? AverageSoldPrice { get; set; }
    public decimal? AverageDaysToSell { get; set; }
    public decimal? SaleRatio { get; set; }

    public List<CategoryStatsDto> Categories { get; set; } = new();
}