using FluentValidation;
using Fortuno.DTO.Purchase;

namespace Fortuno.API.Validators;

public class PurchaseConfirmRequestValidator : AbstractValidator<PurchaseConfirmRequest>
{
    public PurchaseConfirmRequestValidator()
    {
        Include(new PurchasePreviewRequestValidator());
    }
}
