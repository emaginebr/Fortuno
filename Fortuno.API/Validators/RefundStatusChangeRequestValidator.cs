using FluentValidation;
using Fortuno.DTO.Refund;

namespace Fortuno.API.Validators;

public class RefundStatusChangeRequestValidator : AbstractValidator<RefundStatusChangeRequest>
{
    public RefundStatusChangeRequestValidator()
    {
        RuleFor(x => x.TicketIds).NotEmpty();
        RuleFor(x => x.ExternalReference).MaximumLength(160);
    }
}
