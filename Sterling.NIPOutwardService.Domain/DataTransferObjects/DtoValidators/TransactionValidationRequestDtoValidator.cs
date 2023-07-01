namespace Sterling.NIPOutwardService.Domain.DataTransferObjects.DtoValidators;

public class TransactionValidationRequestDtoValidator : AbstractValidator<TransactionValidationRequestDto> 
{
    public TransactionValidationRequestDtoValidator()
    {
        RuleFor(x => x.SessionID)
        .NotNull()
        .NotEmpty()
        .Length(30);
    }
}