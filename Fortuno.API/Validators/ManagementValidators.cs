using FluentValidation;
using Fortuno.DTO.LotteryCombo;
using Fortuno.DTO.Raffle;
using Fortuno.DTO.Refund;

namespace Fortuno.API.Validators;

public class LotteryComboInsertInfoValidator : AbstractValidator<LotteryComboInsertInfo>
{
    public LotteryComboInsertInfoValidator()
    {
        RuleFor(x => x.LotteryId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.DiscountValue).InclusiveBetween(0f, 100f);
        RuleFor(x => x.DiscountLabel).NotEmpty().MaximumLength(80);
        RuleFor(x => x.QuantityStart).GreaterThanOrEqualTo(0);
        RuleFor(x => x.QuantityEnd).GreaterThanOrEqualTo(0);
        RuleFor(x => x).Must(x => x.QuantityEnd == 0 || x.QuantityEnd >= x.QuantityStart)
            .WithMessage("QuantityEnd deve ser >= QuantityStart ou ambos 0.");
    }
}

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

public class RefundStatusChangeRequestValidator : AbstractValidator<RefundStatusChangeRequest>
{
    public RefundStatusChangeRequestValidator()
    {
        RuleFor(x => x.TicketIds).NotEmpty();
        RuleFor(x => x.ExternalReference).MaximumLength(160);
    }
}
