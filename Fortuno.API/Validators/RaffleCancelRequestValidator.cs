using FluentValidation;
using Fortuno.DTO.Raffle;

namespace Fortuno.API.Validators;

public class RaffleCancelRequestValidator : AbstractValidator<RaffleCancelRequest>
{
    public RaffleCancelRequestValidator()
    {
        RuleForEach(x => x.Redistributions).ChildRules(r =>
        {
            r.RuleFor(x => x.RaffleAwardId).GreaterThan(0);
            r.RuleFor(x => x.TargetRaffleId).GreaterThan(0);
        });
    }
}
