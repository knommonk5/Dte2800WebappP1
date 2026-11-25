namespace Bruktborsen.Api.Models;

public class PriceChange
{
    public int Id { get; set; }

    public int ListingId { get; set; }
    public Listing Listing { get; set; } = null!;

    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }

    public DateTime ChangedDate { get; set; } = DateTime.UtcNow;
}