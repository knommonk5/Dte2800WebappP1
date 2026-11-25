using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.DTOs.Analytics;

public class PriceSuggestionDto
{
    public int CategoryId { get; set; }
    public Condition Condition { get; set; }
    public int SampleSize { get; set; }
    public bool HasSufficientData { get; set; }
    public decimal? SuggestedPrice { get; set; }
    public decimal? MedianNormalizedPrice { get; set; }
    public string? Message { get; set; }
}