using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.Models;

public class Offer
{
    public int Id { get; set; }

    public decimal Amount { get; set; }

    public OfferStatus Status { get; set; } = OfferStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int ListingId { get; set; }
    public Listing Listing { get; set; } = null!;

    public string BuyerId { get; set; } = string.Empty;
    public ApplicationUser Buyer { get; set; } = null!;
}