using FluentValidation.TestHelper;
using Fortuno.API.Validators;
using Fortuno.DTO.LotteryImage;

namespace Fortuno.Tests.Application.Validations;

public class LotteryImageInsertInfoValidatorTests
{
    private readonly LotteryImageInsertInfoValidator _validator = new();

    private static LotteryImageInsertInfo Valid() => new()
    {
        LotteryId = 1,
        ImageBase64 = "BASE64DATA",
        Description = "banner",
        DisplayOrder = 0
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
    public void Should_Fail_When_ImageBase64Empty()
    {
        var dto = Valid(); dto.ImageBase64 = string.Empty;
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.ImageBase64);
    }

    [Fact]
    public void Should_Fail_When_DescriptionTooLong()
    {
        var dto = Valid(); dto.Description = new string('x', 300);
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Should_Fail_When_DisplayOrderNegative()
    {
        var dto = Valid(); dto.DisplayOrder = -1;
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.DisplayOrder);
    }
}
