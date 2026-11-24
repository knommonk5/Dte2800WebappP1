using System.ComponentModel.DataAnnotations;
using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.DTOs.Listings;

public class ListingQueryDto : IValidatableObject
{
    [StringLength(200)]
    public string? Search { get; set; }

    [Range(1, int.MaxValue)]
    public int? CategoryId { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal? MinPrice { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal? MaxPrice { get; set; }

    public Condition? Condition { get; set; }

    public string SortBy { get; set; } = "createdAt";

    public string SortOrder { get; set; } = "desc";

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

        if (Condition.HasValue &&
            !Enum.IsDefined(Condition.Value))
        {
            yield return new ValidationResult(
                "Ugyldig tilstand.",
                new[] { nameof(Condition) });
        }

        var validSortFields = new[]
        {
            "createdAt",
            "price",
            "title"
        };

        if (!validSortFields.Contains(
                SortBy,
                StringComparer.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                "SortBy må være createdAt, price eller title.",
                new[] { nameof(SortBy) });
        }

        var validSortOrders = new[]
        {
            "asc",
            "desc"
        };

        if (!validSortOrders.Contains(
                SortOrder,
                StringComparer.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                "SortOrder må være asc eller desc.",
                new[] { nameof(SortOrder) });
        }
    }
}