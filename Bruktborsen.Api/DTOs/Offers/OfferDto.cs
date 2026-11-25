using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.DTOs.Offers;

public class OfferDto
{
    public int Id { get; set; }

    public decimal Amount { get; set; }

    public OfferStatus Status { get; set; }

    public DateTime CreatedDate { get; set; }

    public int ListingId { get; set; }

    public string ListingTitle { get; set; } = string.Empty;

    public string BuyerId { get; set; } = string.Empty;

    public string BuyerName { get; set; } = string.Empty;
}