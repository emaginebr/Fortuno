using FluentValidation;
using Fortuno.DTO.LotteryCombo;

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
