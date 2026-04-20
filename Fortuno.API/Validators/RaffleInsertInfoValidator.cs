using FluentValidation;
using Fortuno.DTO.Raffle;

namespace Fortuno.API.Validators;

public class RaffleInsertInfoValidator : AbstractValidator<RaffleInsertInfo>
{
    public RaffleInsertInfoValidator()
    {
        RuleFor(x => x.LotteryId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().Length(3, 160);
        RuleFor(x => x.RaffleDatetime).NotEmpty();
        RuleFor(x => x.VideoUrl).MaximumLength(500);
    }
}
