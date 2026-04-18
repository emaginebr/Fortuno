using FluentValidation.TestHelper;
using Fortuno.API.Validators;
using Fortuno.DTO.Refund;

namespace Fortuno.Tests.Application.Validations;

public class RefundStatusChangeRequestValidatorTests
{
    private readonly RefundStatusChangeRequestValidator _validator = new();

    [Fact]
    public void Should_Pass_When_ValidDto()
    {
        var dto = new RefundStatusChangeRequest
        {
            TicketIds = new List<long> { 1, 2 },
            ExternalReference = "ext-123"
        };
        _validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_TicketIdsEmpty()
    {
        var dto = new RefundStatusChangeRequest { TicketIds = new List<long>() };
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.TicketIds);
    }

    [Fact]
    public void Should_Fail_When_ExternalReferenceTooLong()
    {
        var dto = new RefundStatusChangeRequest
        {
            TicketIds = new List<long> { 1 },
            ExternalReference = new string('x', 200)
        };
        _validator.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.ExternalReference);
    }

    [Fact]
    public void Should_Pass_When_ExternalReferenceOmitted()
    {
        var dto = new RefundStatusChangeRequest
        {
            TicketIds = new List<long> { 1 },
            ExternalReference = null
        };
        _validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }
}
