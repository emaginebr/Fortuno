using FluentValidation;
using Fortuno.DTO.Lottery;

namespace Fortuno.API.Validators;

public class LotteryCancelRequestValidator : AbstractValidator<LotteryCancelRequest>
{
    public LotteryCancelRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MinimumLength(20)
            .MaximumLength(1000)
            .WithMessage("Motivo do cancelamento é obrigatório (mínimo 20 caracteres).");
    }
}
