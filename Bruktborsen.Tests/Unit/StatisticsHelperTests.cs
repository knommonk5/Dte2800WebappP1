using Bruktborsen.Api.Services.Analytics;
using Xunit;

namespace Bruktborsen.Tests.Unit;

public class StatisticsHelperTests
{
    [Fact]
    public void Mean_ReturnsCorrectAverage()
    {
        decimal[] values = [100m, 200m, 300m, 400m];

        Assert.Equal(250m, StatisticsHelper.Mean(values));
    }

    [Fact]
    public void Median_WithOddNumberOfValues_ReturnsMiddleValue()
    {
        decimal[] values = [500m, 100m, 300m, 200m, 400m];

        Assert.Equal(300m, StatisticsHelper.Median(values));
    }

    [Fact]
    public void Median_WithEvenNumberOfValues_ReturnsAverageOfMiddleValues()
    {
        decimal[] values = [400m, 100m, 300m, 200m];

        Assert.Equal(250m, StatisticsHelper.Median(values));
    }

    [Fact]
    public void Median_WithSingleValue_ReturnsValue()
    {
        decimal[] values = [500m];

        Assert.Equal(500m, StatisticsHelper.Median(values));
    }

    [Fact]
    public void FirstQuartile_UsesLinearInterpolation()
    {
        decimal[] values = [400m, 100m, 300m, 200m];

        Assert.Equal(
            175m,
            StatisticsHelper.FirstQuartile(values));
    }

    [Fact]
    public void ThirdQuartile_UsesLinearInterpolation()
    {
        decimal[] values = [400m, 100m, 300m, 200m];

        Assert.Equal(
            325m,
            StatisticsHelper.ThirdQuartile(values));
    }

    [Fact]
    public void InterquartileRange_ReturnsDifferenceBetweenQuartiles()
    {
        decimal[] values = [100m, 200m, 300m, 400m];

        Assert.Equal(
            150m,
            StatisticsHelper.InterquartileRange(values));
    }

    [Fact]
    public void SampleStandardDeviation_UsesSampleVariance()
    {
        decimal[] values = [100m, 200m, 300m];

        Assert.Equal(
            100m,
            StatisticsHelper.SampleStandardDeviation(values));
    }

    [Fact]
    public void SampleStandardDeviation_WithIdenticalValues_ReturnsZero()
    {
        decimal[] values = [500m, 500m, 500m];

        Assert.Equal(
            0m,
            StatisticsHelper.SampleStandardDeviation(values));
    }

    [Theory]
    [InlineData(104, 100)]
    [InlineData(105, 110)]
    [InlineData(106, 110)]
    [InlineData(110, 110)]
    public void RoundToNearestTen_ReturnsCorrectValue(
        int value,
        int expected)
    {
        Assert.Equal(
            (decimal)expected,
            StatisticsHelper.RoundToNearestTen(value));
    }

    [Fact]
    public void Mean_WithEmptyDataset_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            StatisticsHelper.Mean(Array.Empty<decimal>()));
    }

    [Fact]
    public void Median_WithEmptyDataset_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            StatisticsHelper.Median(Array.Empty<decimal>()));
    }

    [Fact]
    public void SampleStandardDeviation_WithSingleValue_ThrowsArgumentException()
    {
        decimal[] values = [100m];

        Assert.Throws<ArgumentException>(() =>
            StatisticsHelper.SampleStandardDeviation(values));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void Percentile_WithInvalidPercentile_ThrowsArgumentOutOfRangeException(
        int percentile)
    {
        decimal[] values = [100m, 200m, 300m];

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StatisticsHelper.Percentile(values, percentile));
    }
}