using FluentValidation;
using Fortuno.DTO.Enums;
using Fortuno.DTO.Lottery;

namespace Fortuno.API.Validators;

public class LotteryInsertInfoValidator : AbstractValidator<LotteryInsertInfo>
{
    public LotteryInsertInfoValidator()
    {
        RuleFor(x => x.EditionNumber).GreaterThan(0);
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

        RuleFor(x => x).Must(HaveConsistentTicketMinMax)
            .WithMessage("Quando Ticket Max > 0, deve ser >= Ticket Min.");

        // Int64: TicketNumIni/End definem a faixa dos números. NumberValueMin/Max é ignorado.
        RuleFor(x => x).Must(HaveValidInt64Range)
            .When(x => x.NumberType == NumberTypeDto.Int64)
            .WithMessage("Para NumberType Int64, TicketNumEnd deve ser maior ou igual a TicketNumIni e maior que zero.");

        // Composed: NumberValueMin/Max ∈ [0, 99] e Min ≤ Max. TicketNumIni/End é ignorado.
        RuleFor(x => x).Must(HaveValidComposedRange)
            .When(x => x.NumberType != NumberTypeDto.Int64)
            .WithMessage("Para tipos compostos, NumberValueMin/Max deve estar em [0..99] e Min ≤ Max.");
    }

    private static bool HaveConsistentTicketMinMax(LotteryInsertInfo x)
        => x.TicketMax == 0 || x.TicketMax >= x.TicketMin;

    private static bool HaveValidInt64Range(LotteryInsertInfo x)
        => x.TicketNumEnd > 0 && x.TicketNumEnd >= x.TicketNumIni;

    private static bool HaveValidComposedRange(LotteryInsertInfo x)
        => x.NumberValueMin is >= 0 and <= 99
            && x.NumberValueMax is >= 0 and <= 99
            && x.NumberValueMin <= x.NumberValueMax;
}
