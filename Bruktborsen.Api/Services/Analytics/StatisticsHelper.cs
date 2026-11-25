namespace Bruktborsen.Api.Services.Analytics;

public static class StatisticsHelper
{
    public static decimal Mean(IEnumerable<decimal> values)
    {
        var data = values.ToArray();

        if (data.Length == 0)
            throw new ArgumentException("Datasettet er tomt.");

        return data.Average();
    }

    public static decimal Median(IEnumerable<decimal> values)
    {
        return Percentile(values, 0.5m);
    }

    public static decimal FirstQuartile(IEnumerable<decimal> values)
    {
        return Percentile(values, 0.25m);
    }

    public static decimal ThirdQuartile(IEnumerable<decimal> values)
    {
        return Percentile(values, 0.75m);
    }

    public static decimal InterquartileRange(
        IEnumerable<decimal> values)
    {
        var data = values.ToArray();

        return ThirdQuartile(data) - FirstQuartile(data);
    }

    // Lineær interpolasjon tilsvarende Excel QUARTILE.INC.
    public static decimal Percentile(
        IEnumerable<decimal> values,
        decimal percentile)
    {
        if (percentile < 0 || percentile > 1)
            throw new ArgumentOutOfRangeException(nameof(percentile));

        var sorted = values.OrderBy(x => x).ToArray();

        if (sorted.Length == 0)
            throw new ArgumentException("Datasettet er tomt.");

        var position = (sorted.Length - 1) * percentile;

        var lowerIndex = (int)Math.Floor(position);
        var upperIndex = (int)Math.Ceiling(position);

        if (lowerIndex == upperIndex)
            return sorted[lowerIndex];

        var fraction = position - lowerIndex;

        return sorted[lowerIndex] +
               fraction * (sorted[upperIndex] - sorted[lowerIndex]);
    }

    // Utvalgsstandardavvik med n - 1.
    public static decimal SampleStandardDeviation(
        IEnumerable<decimal> values)
    {
        var data = values.ToArray();

        if (data.Length < 2)
            throw new ArgumentException(
                "Standardavvik krever minst to observasjoner.");

        var mean = Mean(data);

        var squaredDifferences = data.Sum(value =>
        {
            var difference = value - mean;
            return difference * difference;
        });

        var variance = squaredDifferences / (data.Length - 1);

        return (decimal)Math.Sqrt((double)variance);
    }

    public static decimal RoundToNearestTen(decimal value)
    {
        return Math.Round(
            value / 10m,
            0,
            MidpointRounding.AwayFromZero) * 10m;
    }
}