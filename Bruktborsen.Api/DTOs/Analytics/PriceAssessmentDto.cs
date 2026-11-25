namespace Bruktborsen.Api.DTOs.Analytics;

public class PriceAssessmentDto
{
    public int SampleSize { get; set; }

    public string Assessment { get; set; } = "Unknown";

    public decimal NormalizedPrice { get; set; }

    public decimal? FirstQuartile { get; set; }
    public decimal? ThirdQuartile { get; set; }
    public decimal? InterquartileRange { get; set; }

    public decimal? LowerBound { get; set; }
    public decimal? UpperBound { get; set; }
}