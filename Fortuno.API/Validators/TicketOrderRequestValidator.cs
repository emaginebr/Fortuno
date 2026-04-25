using FluentValidation;
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

        // pickedNumbers é opcional; quando vier, count deve ser <= quantity.
        RuleFor(x => x.PickedNumbers)
            .Must((req, picks) => picks == null || picks.Count <= req.Quantity)
            .WithMessage("pickedNumbers não pode exceder quantity — números faltantes são sorteados aleatoriamente.");
    }
}
