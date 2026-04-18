using FluentAssertions;
using Fortuno.Domain.Enums;
using Fortuno.Domain.Services;

namespace Fortuno.Tests.Domain.Services;

public class NumberCompositionServiceTests
{
    private readonly NumberCompositionService _sut = new();

    [Theory]
    [InlineData(NumberType.Int64, 1)]
    [InlineData(NumberType.Composed3, 3)]
    [InlineData(NumberType.Composed4, 4)]
    [InlineData(NumberType.Composed5, 5)]
    [InlineData(NumberType.Composed6, 6)]
    [InlineData(NumberType.Composed7, 7)]
    [InlineData(NumberType.Composed8, 8)]
    public void ComponentCount_ShouldReturnExpected(NumberType type, int expected)
    {
        _sut.ComponentCount(type).Should().Be(expected);
    }

    [Fact]
    public void Compose_Int64_ShouldReturnFirstComponent()
    {
        _sut.Compose(NumberType.Int64, new[] { 1234 }).Should().Be(1234);
    }

    [Fact]
    public void Compose_Composed3_ShouldBuildNumberFromComponents()
    {
        // 01, 23, 45 -> 1 * 100^2 + 23 * 100 + 45 = 10000 + 2300 + 45 = 12345
        var value = _sut.Compose(NumberType.Composed3, new[] { 1, 23, 45 });
        value.Should().Be(12345);
    }

    [Fact]
    public void Compose_ShouldThrow_WhenComponentCountMismatch()
    {
        Action act = () => _sut.Compose(NumberType.Composed3, new[] { 1, 2 });
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Compose_ShouldThrow_WhenComponentOutOfRange()
    {
        Action act = () => _sut.Compose(NumberType.Composed3, new[] { 1, 200, 3 });
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Decompose_ShouldReverseCompose()
    {
        var composed = _sut.Compose(NumberType.Composed4, new[] { 1, 2, 3, 4 });
        var parts = _sut.Decompose(NumberType.Composed4, composed);
        parts.Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public void IsValid_Int64_ShouldRespectRange()
    {
        _sut.IsValid(NumberType.Int64, 50, 10, 100).Should().BeTrue();
        _sut.IsValid(NumberType.Int64, 5, 10, 100).Should().BeFalse();
        _sut.IsValid(NumberType.Int64, 150, 10, 100).Should().BeFalse();
    }

    [Fact]
    public void IsValid_Composed_ShouldRejectWhenAnyComponentOutOfRange()
    {
        var composed = _sut.Compose(NumberType.Composed3, new[] { 10, 20, 30 });
        _sut.IsValid(NumberType.Composed3, composed, 0, 99).Should().BeTrue();
        _sut.IsValid(NumberType.Composed3, composed, 15, 99).Should().BeFalse();
    }

    [Fact]
    public void CountPossibilities_ShouldReturnRangePowerComponents()
    {
        _sut.CountPossibilities(NumberType.Int64, 0, 9).Should().Be(10);
        _sut.CountPossibilities(NumberType.Composed3, 0, 9).Should().Be(1000);
    }

    [Fact]
    public void CountPossibilities_ShouldReturnZero_WhenMaxLessThanMin()
    {
        _sut.CountPossibilities(NumberType.Int64, 10, 5).Should().Be(0);
    }

    [Fact]
    public void EnumerateAll_Int64_ShouldYieldRange()
    {
        _sut.EnumerateAll(NumberType.Int64, 1, 3).Should().Equal(1L, 2L, 3L);
    }

    [Fact]
    public void EnumerateAll_Composed_ShouldProduceCartesianProduct()
    {
        var list = _sut.EnumerateAll(NumberType.Composed3, 0, 1).ToList();
        list.Count.Should().Be(8);
    }
}
