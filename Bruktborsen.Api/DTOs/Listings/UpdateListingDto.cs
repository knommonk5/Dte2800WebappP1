using System.ComponentModel.DataAnnotations;
using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.DTOs.Listings;

public class UpdateListingDto
{
    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Price { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal OriginalPrice { get; set; }

    [EnumDataType(typeof(Condition))]
    public Condition Condition { get; set; }

    [StringLength(2048)]
    [Url]
    public string? ImageUrl { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}