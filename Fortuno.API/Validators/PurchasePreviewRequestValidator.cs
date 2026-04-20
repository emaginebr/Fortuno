using FluentValidation;
using Fortuno.DTO.Enums;
using Fortuno.DTO.Purchase;

namespace Fortuno.API.Validators;

public class PurchasePreviewRequestValidator : AbstractValidator<PurchasePreviewRequest>
{
    public PurchasePreviewRequestValidator()
    {
        RuleFor(x => x.LotteryId).GreaterThan(0);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.ReferralCode)
            .MaximumLength(8)
            .Matches("^[A-Z0-9]*$").WithMessage("ReferralCode deve conter apenas A-Z e dígitos.")
            .When(x => !string.IsNullOrWhiteSpace(x.ReferralCode));

        RuleFor(x => x.PickedNumbers)
            .NotNull().NotEmpty()
            .Must((req, picks) => picks != null && picks.Count == req.Quantity)
            .When(x => x.Mode == PurchaseAssignmentModeDto.UserPicks)
            .WithMessage("No modo UserPicks, pickedNumbers é obrigatório e deve ter a mesma quantidade do pedido.");
    }
}
