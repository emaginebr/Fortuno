using FluentValidation;
using Fortuno.DTO.RaffleAward;

namespace Fortuno.API.Validators;

public class RaffleAwardInsertInfoValidator : AbstractValidator<RaffleAwardInsertInfo>
{
    public RaffleAwardInsertInfoValidator()
    {
        RuleFor(x => x.RaffleId).GreaterThan(0);
        RuleFor(x => x.Position).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(300);
    }
}
