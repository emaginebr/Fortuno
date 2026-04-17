using FluentValidation;
using Fortuno.DTO.LotteryImage;

namespace Fortuno.API.Validators;

public class LotteryImageInsertInfoValidator : AbstractValidator<LotteryImageInsertInfo>
{
    public LotteryImageInsertInfoValidator()
    {
        RuleFor(x => x.LotteryId).GreaterThan(0);
        RuleFor(x => x.ImageBase64).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(260);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public class RaffleInsertInfoValidator : AbstractValidator<Fortuno.DTO.Raffle.RaffleInsertInfo>
{
    public RaffleInsertInfoValidator()
    {
        RuleFor(x => x.LotteryId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().Length(3, 160);
        RuleFor(x => x.RaffleDatetime).NotEmpty();
        RuleFor(x => x.VideoUrl).MaximumLength(500);
    }
}

public class RaffleAwardInsertInfoValidator : AbstractValidator<Fortuno.DTO.RaffleAward.RaffleAwardInsertInfo>
{
    public RaffleAwardInsertInfoValidator()
    {
        RuleFor(x => x.RaffleId).GreaterThan(0);
        RuleFor(x => x.Position).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(300);
    }
}
