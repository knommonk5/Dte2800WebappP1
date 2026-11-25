namespace Bruktborsen.Api.DTOs.Analytics;

public class CategoryStatsDto
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public int TotalListings { get; set; }
    public int ActiveListings { get; set; }
    public int SoldListings { get; set; }
    public int RemovedListings { get; set; }

    public decimal? AverageSoldPrice { get; set; }
    public decimal? MedianSoldPrice { get; set; }
    public decimal? StandardDeviation { get; set; }

    public decimal? AverageDaysToSell { get; set; }
    public decimal? AverageDiscountPercentage { get; set; }

    public decimal? SaleRatio { get; set; }
}