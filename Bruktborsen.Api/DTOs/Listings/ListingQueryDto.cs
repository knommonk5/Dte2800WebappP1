using System.ComponentModel.DataAnnotations;
using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.DTOs.Listings;

public class ListingQueryDto : IValidatableObject
{
    [StringLength(200)]
    public string? Search { get; set; }

    [Range(1, int.MaxValue)]
    public int? CategoryId { get; set; }

    [Range(0, int.MaxValue)]
    public int? MinPrice { get; set; }

    [Range(0, int.MaxValue)]
    public int? MaxPrice { get; set; }

    [EnumDataType(typeof(Condition))]
    public Condition? Condition { get; set; }

    public string Sort { get; set; } = "newest";

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (MinPrice.HasValue &&
            MaxPrice.HasValue &&
            MinPrice.Value > MaxPrice.Value)
        {
            yield return new ValidationResult(
                "Minimumspris kan ikke være høyere enn maksimumspris.",
                new[] { nameof(MinPrice), nameof(MaxPrice) });
        }

        var validSortValues = new[]
        {
            "newest",
            "price_asc",
            "price_desc"
        };

        if (!validSortValues.Contains(
                Sort,
                StringComparer.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                "Sort må være newest, price_asc eller price_desc.",
                new[] { nameof(Sort) });
        }
    }
}