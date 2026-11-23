using System.ComponentModel.DataAnnotations;
using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.DTOs.Listings;

public class UpdateListingDto
{
    [Required]
    [StringLength(150, MinimumLength = 3)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(5000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "999999999")]
    public decimal Price { get; set; }

    [Range(typeof(decimal), "0", "999999999")]
    public decimal OriginalPrice { get; set; }

    public Condition Condition { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}