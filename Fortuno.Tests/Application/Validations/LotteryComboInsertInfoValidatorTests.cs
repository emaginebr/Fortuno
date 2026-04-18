using FluentAssertions;
using FluentValidation.TestHelper;
using Fortuno.API.Validators;
using Fortuno.DTO.LotteryCombo;

namespace Fortuno.Tests.Application.Validations;

public class LotteryComboInsertInfoValidatorTests
{
    private readonly LotteryComboInsertInfoValidator _validator = new();

    private static LotteryComboInsertInfo Valid() => new()
    {
        LotteryId = 1,
        Name = "5-pack",
        DiscountValue = 10f,
        DiscountLabel = "10% off",
        QuantityStart = 5,
        QuantityEnd = 10
    };

    [Fact]
    public void Should_Pass_When_ValidDto()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_LotteryIdZero()
    {
        var dto = Valid(); dto.LotteryId = 0;
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.LotteryId);
    }

    [Fact]
    public void Should_Fail_When_NameEmpty()
    {
        var dto = Valid(); dto.Name = string.Empty;
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Should_Fail_When_DiscountValueAbove100()
    {
        var dto = Valid(); dto.DiscountValue = 150f;
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.DiscountValue);
    }

    [Fact]
    public void Should_Fail_When_DiscountValueNegative()
    {
        var dto = Valid(); dto.DiscountValue = -1f;
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.DiscountValue);
    }

    [Fact]
    public void Should_Fail_When_DiscountLabelEmpty()
    {
        var dto = Valid(); dto.DiscountLabel = string.Empty;
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.DiscountLabel);
    }

    [Fact]
    public void Should_Fail_When_QuantityEndLessThanStart()
    {
        var dto = Valid(); dto.QuantityStart = 10; dto.QuantityEnd = 5;
        var result = _validator.TestValidate(dto);
        result.Errors.Count.Should().BeGreaterThan(0);
    }
}
