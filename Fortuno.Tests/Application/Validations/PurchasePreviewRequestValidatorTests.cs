using FluentValidation.TestHelper;
using Fortuno.API.Validators;
using Fortuno.DTO.Enums;
using Fortuno.DTO.Purchase;

namespace Fortuno.Tests.Application.Validations;

public class PurchasePreviewRequestValidatorTests
{
    private readonly PurchasePreviewRequestValidator _validator = new();

    private static PurchasePreviewRequest Valid() => new()
    {
        LotteryId = 1,
        Quantity = 3,
        Mode = PurchaseAssignmentModeDto.Random
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
    public void Should_Fail_When_QuantityNonPositive()
    {
        var dto = Valid(); dto.Quantity = 0;
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Should_Fail_When_ReferralCodeHasInvalidChars()
    {
        var dto = Valid(); dto.ReferralCode = "bad-code";
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.ReferralCode);
    }

    [Fact]
    public void Should_Fail_When_UserPicksWithoutPickedNumbers()
    {
        var dto = Valid();
        dto.Mode = PurchaseAssignmentModeDto.UserPicks;
        dto.PickedNumbers = null;
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.PickedNumbers);
    }

    [Fact]
    public void Should_Fail_When_UserPicksCountDiffersFromQuantity()
    {
        var dto = Valid();
        dto.Mode = PurchaseAssignmentModeDto.UserPicks;
        dto.Quantity = 3;
        dto.PickedNumbers = new List<long> { 1, 2 };
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.PickedNumbers);
    }

    [Fact]
    public void Should_Pass_When_UserPicksCountMatchesQuantity()
    {
        var dto = Valid();
        dto.Mode = PurchaseAssignmentModeDto.UserPicks;
        dto.Quantity = 3;
        dto.PickedNumbers = new List<long> { 1, 2, 3 };
        _validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }
}
