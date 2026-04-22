using FluentValidation;
using Fortuno.DTO.Enums;
using Fortuno.DTO.Ticket;

namespace Fortuno.API.Validators;

public class TicketOrderRequestValidator : AbstractValidator<TicketOrderRequest>
{
    public TicketOrderRequestValidator()
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
            .When(x => x.Mode == TicketOrderMode.UserPicks)
            .WithMessage("No modo UserPicks, pickedNumbers é obrigatório e deve ter a mesma quantidade do pedido.");
    }
}
