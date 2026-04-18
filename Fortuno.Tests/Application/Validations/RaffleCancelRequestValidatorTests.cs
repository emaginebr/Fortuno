using FluentValidation.TestHelper;
using Fortuno.API.Validators;
using Fortuno.DTO.Raffle;

namespace Fortuno.Tests.Application.Validations;

public class RaffleCancelRequestValidatorTests
{
    private readonly RaffleCancelRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_When_RedistributionsEmpty()
    {
        var dto = new RaffleCancelRequest();
        _validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Pass_When_RedistributionsValid()
    {
        var dto = new RaffleCancelRequest
        {
            Redistributions = new List<AwardRedistribution>
            {
                new() { RaffleAwardId = 1, TargetRaffleId = 2 }
            }
        };
        _validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_RaffleAwardIdZero()
    {
        var dto = new RaffleCancelRequest
        {
            Redistributions = new List<AwardRedistribution>
            {
                new() { RaffleAwardId = 0, TargetRaffleId = 2 }
            }
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor("Redistributions[0].RaffleAwardId");
    }

    [Fact]
    public void Should_Fail_When_TargetRaffleIdZero()
    {
        var dto = new RaffleCancelRequest
        {
            Redistributions = new List<AwardRedistribution>
            {
                new() { RaffleAwardId = 1, TargetRaffleId = 0 }
            }
        };
        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor("Redistributions[0].TargetRaffleId");
    }
}
