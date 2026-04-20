using FluentValidation;
using Fortuno.DTO.LotteryImage;

namespace Fortuno.API.Validators;

public class LotteryImageInsertInfoValidator : AbstractValidator<LotteryImageInsertInfo>
{
    public LotteryImageInsertInfoValidator()
    {
        RuleFor(x => x.LotteryId).GreaterThan(0);
        RuleFor(x => x.ImageBase64).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(260);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
