using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.Models;

public class Listing
{
    public int Id { get; set; }
    
    public Guid Version { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int Price { get; set; }
    public decimal OriginalPrice { get; set; }

    public Condition Condition { get; set; }
    public ListingStatus Status { get; set; } = ListingStatus.Active;

    public string? ImageUrl { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? SoldDate { get; set; }
    public decimal? SoldPrice { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string SellerId { get; set; } = string.Empty;
    public ApplicationUser Seller { get; set; } = null!;

    public ICollection<Offer> Offers { get; set; } = new List<Offer>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<PriceChange> PriceChanges { get; set; } = new List<PriceChange>();
}