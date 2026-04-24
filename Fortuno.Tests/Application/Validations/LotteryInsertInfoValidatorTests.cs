using FluentAssertions;
using FluentValidation.TestHelper;
using Fortuno.API.Validators;
using Fortuno.DTO.Enums;
using Fortuno.DTO.Lottery;

namespace Fortuno.Tests.Application.Validations;

public class LotteryInsertInfoValidatorTests
{
    private readonly LotteryInsertInfoValidator _validator = new();

    private static LotteryInsertInfo Valid() => new()
    {
        Name = "Rifa QA",
        DescriptionMd = "desc",
        RulesMd = "rules",
        PrivacyPolicyMd = "privacy",
        TicketPrice = 10m,
        TotalPrizeValue = 1000m,
        TicketMin = 1,
        TicketMax = 10,
        TicketNumIni = 1,
        TicketNumEnd = 100,
        NumberType = NumberTypeDto.Int64,
        NumberValueMin = 1,
        NumberValueMax = 100,
        ReferralPercent = 5f
    };

    [Fact]
    public void Should_Pass_When_ValidDto()
    {
        var result = _validator.TestValidate(Valid());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_NameEmpty()
    {
        var dto = Valid(); dto.Name = string.Empty;
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Should_Fail_When_NameTooShort()
    {
        var dto = Valid(); dto.Name = "ab";
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Should_Fail_When_TicketPriceNonPositive()
    {
        var dto = Valid(); dto.TicketPrice = 0m;
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.TicketPrice);
    }

    [Fact]
    public void Should_Fail_When_TotalPrizeValueNonPositive()
    {
        var dto = Valid(); dto.TotalPrizeValue = 0m;
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.TotalPrizeValue);
    }

    [Fact]
    public void Should_Fail_When_DescriptionEmpty()
    {
        var dto = Valid(); dto.DescriptionMd = string.Empty;
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.DescriptionMd);
    }

    [Fact]
    public void Should_Fail_When_ReferralPercentOutOfRange()
    {
        var dto = Valid(); dto.ReferralPercent = 150f;
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.ReferralPercent);
    }

    [Fact]
    public void Should_Fail_WhenComposedHasMinGreaterThanMax()
    {
        var dto = Valid();
        dto.NumberType = NumberTypeDto.Composed3;
        dto.NumberValueMin = 50;
        dto.NumberValueMax = 10;
        var result = _validator.TestValidate(dto);
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("NumberValueMin") || e.ErrorMessage.Contains("Min ≤ Max"));
    }

    [Fact]
    public void Should_Fail_When_ComposedTypeHasRangeAbove99()
    {
        var dto = Valid();
        dto.NumberType = NumberTypeDto.Composed3;
        dto.NumberValueMin = 0;
        dto.NumberValueMax = 150;
        var result = _validator.TestValidate(dto);
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("composto") || e.ErrorMessage.Contains("0..99"));
    }

    [Fact]
    public void Should_Fail_When_Int64HasTicketNumEndZero()
    {
        var dto = Valid();
        dto.TicketNumIni = 0;
        dto.TicketNumEnd = 0;
        var result = _validator.TestValidate(dto);
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Int64") || e.ErrorMessage.Contains("TicketNumEnd"));
    }

    [Fact]
    public void Should_Fail_When_Int64HasEndLessThanIni()
    {
        var dto = Valid();
        dto.TicketNumIni = 100;
        dto.TicketNumEnd = 50;
        var result = _validator.TestValidate(dto);
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Int64") || e.ErrorMessage.Contains("TicketNumEnd"));
    }

    [Fact]
    public void Should_Pass_When_Composed_TicketNumIgnored()
    {
        var dto = Valid();
        dto.NumberType = NumberTypeDto.Composed3;
        dto.NumberValueMin = 0;
        dto.NumberValueMax = 99;
        dto.TicketNumIni = 0;     // ignorado em Composed
        dto.TicketNumEnd = 0;     // ignorado em Composed
        _validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }
}
