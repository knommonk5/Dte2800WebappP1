using System.ComponentModel.DataAnnotations;

namespace Bruktborsen.Api.DTOs.Offers;

public class CreateOfferDto
{
    [Range(1, int.MaxValue,
        ErrorMessage = "Ugyldig annonse-ID.")]
    public int ListingId { get; set; }

    [Range(typeof(decimal), "0.01", "999999999",
        ErrorMessage = "Budbeløpet må være større enn null.")]
    public decimal Amount { get; set; }
}