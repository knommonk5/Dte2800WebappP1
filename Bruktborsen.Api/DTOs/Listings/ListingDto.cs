using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.DTOs.Listings;

public class ListingDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int Price { get; set; }
    public decimal OriginalPrice { get; set; }

    public Condition Condition { get; set; }
    public ListingStatus Status { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime? SoldDate { get; set; }
    public decimal? SoldPrice { get; set; }

    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    public string SellerId { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;

    public int OfferCount { get; set; }

    public string PriceAssessment { get; set; } = "Unknown";
}