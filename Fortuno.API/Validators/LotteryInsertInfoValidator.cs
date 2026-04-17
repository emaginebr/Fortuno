using FluentValidation;
using Fortuno.DTO.Enums;
using Fortuno.DTO.Lottery;

namespace Fortuno.API.Validators;

public class LotteryInsertInfoValidator : AbstractValidator<LotteryInsertInfo>
{
    public LotteryInsertInfoValidator()
    {
        RuleFor(x => x.StoreId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().Length(3, 160);
        RuleFor(x => x.DescriptionMd).NotEmpty();
        RuleFor(x => x.RulesMd).NotEmpty();
        RuleFor(x => x.PrivacyPolicyMd).NotEmpty();
        RuleFor(x => x.TicketPrice).GreaterThan(0);
        RuleFor(x => x.TotalPrizeValue).GreaterThan(0);
        RuleFor(x => x.TicketMin).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TicketMax).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TicketNumIni).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TicketNumEnd).GreaterThanOrEqualTo(0);
        RuleFor(x => x.NumberValueMin).GreaterThanOrEqualTo(0);
        RuleFor(x => x.NumberValueMax).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReferralPercent).InclusiveBetween(0f, 100f);

        RuleFor(x => x).Must(HaveValidMinMax)
            .WithMessage("NumberValueMin deve ser menor ou igual a NumberValueMax.");

        RuleFor(x => x).Must(HaveConsistentTicketMinMax)
            .WithMessage("Quando Ticket Max > 0, deve ser >= Ticket Min.");

        RuleFor(x => x).Must(HaveValidComposedRange)
            .WithMessage("Tipo composto exige valor mínimo e máximo entre 0 e 99.");
    }

    private static bool HaveValidMinMax(LotteryInsertInfo x) => x.NumberValueMin <= x.NumberValueMax;

    private static bool HaveConsistentTicketMinMax(LotteryInsertInfo x)
        => x.TicketMax == 0 || x.TicketMax >= x.TicketMin;

    private static bool HaveValidComposedRange(LotteryInsertInfo x)
    {
        if (x.NumberType == NumberTypeDto.Int64) return true;
        return x.NumberValueMin is >= 0 and <= 99 && x.NumberValueMax is >= 0 and <= 99;
    }
}
