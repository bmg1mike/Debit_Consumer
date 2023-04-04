namespace Sterling.NIPOutwardService.Domain.DataTransferObjects.DtoValidators;

public class NameEnquiryRequestDtoValidator : AbstractValidator<NameEnquiryRequestDto>
{
    public NameEnquiryRequestDtoValidator()
    {
        RuleFor(x => x.SessionID)
        .MaximumLength(30)
        .NotEmpty()
        .NotNull();

        RuleFor(x => x.DestinationInstitutionCode)
        .NotNull()
        .NotEmpty()
        .MaximumLength(10);

        RuleFor(x => x.ChannelCode)
        .NotNull()
        .NotEmpty()
        .MaximumLength(10);

        RuleFor(x => x.AccountNumber)
        .NotNull()
        .NotEmpty()
        .Length(10);
    }
}