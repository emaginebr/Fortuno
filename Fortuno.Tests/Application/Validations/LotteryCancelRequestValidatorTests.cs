using FluentValidation.TestHelper;
using Fortuno.API.Validators;
using Fortuno.DTO.Lottery;

namespace Fortuno.Tests.Application.Validations;

public class LotteryCancelRequestValidatorTests
{
    private readonly LotteryCancelRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_When_ValidReason()
    {
        var dto = new LotteryCancelRequest { Reason = "Motivo adequado com mais de vinte caracteres para passar." };
        _validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_ReasonEmpty()
    {
        var dto = new LotteryCancelRequest { Reason = string.Empty };
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void Should_Fail_When_ReasonTooShort()
    {
        var dto = new LotteryCancelRequest { Reason = "curto" };
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void Should_Fail_When_ReasonTooLong()
    {
        var dto = new LotteryCancelRequest { Reason = new string('x', 1500) };
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Reason);
    }
}
