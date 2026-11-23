using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.Models;

public class Listing
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }
    public decimal OriginalPrice { get; set; }

    public Condition Condition { get; set; }
    public ListingStatus Status { get; set; } = ListingStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SoldAt { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string SellerId { get; set; } = string.Empty;
    public ApplicationUser Seller { get; set; } = null!;

    public ICollection<Offer> Offers { get; set; } = new List<Offer>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<PriceChange> PriceChanges { get; set; } = new List<PriceChange>();
}