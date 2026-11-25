namespace Bruktborsen.Api.DTOs.PriceHistory;

public class PriceChangeDto
{
    public int Id { get; set; }

    public int ListingId { get; set; }

    public decimal OldPrice { get; set; }

    public decimal NewPrice { get; set; }

    public DateTime ChangedDate { get; set; }
}