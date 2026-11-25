using Bruktborsen.Api.Models.Enums;
using Bruktborsen.Api.Services.Analytics;
using Xunit;

namespace Bruktborsen.Tests.Unit;

public class PriceNormalizationHelperTests
{
    [Theory]
    [InlineData(Condition.New, 100)]
    [InlineData(Condition.LikeNew, 85)]
    [InlineData(Condition.Good, 70)]
    [InlineData(Condition.Used, 55)]
    [InlineData(Condition.Defect, 30)]
    public void GetConditionFactor_ReturnsCorrectFactor(
        Condition condition,
        int percentage)
    {
        Assert.Equal(
            percentage / 100m,
            PriceNormalizationHelper.GetConditionFactor(condition));
    }

    [Theory]
    [InlineData(Condition.New, 1000)]
    [InlineData(Condition.LikeNew, 850)]
    [InlineData(Condition.Good, 700)]
    [InlineData(Condition.Used, 550)]
    [InlineData(Condition.Defect, 300)]
    public void NormalizePrice_ReturnsCorrectNormalizedPrice(
        Condition condition,
        int price)
    {
        Assert.Equal(
            1000m,
            PriceNormalizationHelper.NormalizePrice(price, condition));
    }

    [Theory]
    [InlineData(Condition.New, 1000)]
    [InlineData(Condition.LikeNew, 850)]
    [InlineData(Condition.Good, 700)]
    [InlineData(Condition.Used, 550)]
    [InlineData(Condition.Defect, 300)]
    public void DenormalizePrice_ReturnsCorrectAdjustedPrice(
        Condition condition,
        int expected)
    {
        Assert.Equal(
            (decimal)expected,
            PriceNormalizationHelper.DenormalizePrice(1000m, condition));
    }

    [Theory]
    [InlineData(Condition.New)]
    [InlineData(Condition.LikeNew)]
    [InlineData(Condition.Good)]
    [InlineData(Condition.Used)]
    [InlineData(Condition.Defect)]
    public void NormalizeAndDenormalize_ReturnsOriginalPrice(
        Condition condition)
    {
        const decimal originalPrice = 1234.56m;

        var normalizedPrice =
            PriceNormalizationHelper.NormalizePrice(
                originalPrice, condition);

        var restoredPrice =
            PriceNormalizationHelper.DenormalizePrice(
                normalizedPrice, condition);

        Assert.Equal(
            originalPrice,
            Math.Round(restoredPrice, 2));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void NormalizePrice_WithInvalidPrice_ThrowsException(
        int price)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PriceNormalizationHelper.NormalizePrice(
                price, Condition.Good));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void DenormalizePrice_WithInvalidPrice_ThrowsException(
        int price)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PriceNormalizationHelper.DenormalizePrice(
                price, Condition.Good));
    }

    [Fact]
    public void GetConditionFactor_WithInvalidCondition_ThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PriceNormalizationHelper.GetConditionFactor(
                (Condition)999));
    }
}