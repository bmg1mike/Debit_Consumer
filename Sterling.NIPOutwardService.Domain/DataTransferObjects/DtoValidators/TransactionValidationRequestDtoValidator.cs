namespace Sterling.NIPOutwardService.Domain.DataTransferObjects.DtoValidators;

public class TransactionValidationRequestDtoValidator : AbstractValidator<TransactionValidationRequestDto> 
{
    public TransactionValidationRequestDtoValidator()
    {
        RuleFor(x => x.PaymentReference)
        .NotNull()
        .NotEmpty()
        .MaximumLength(100);
    }
}