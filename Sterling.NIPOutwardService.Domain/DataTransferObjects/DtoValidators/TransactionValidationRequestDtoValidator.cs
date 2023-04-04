namespace Sterling.NIPOutwardService.Domain.DataTransferObjects.DtoValidators;

public class TransactionValidationRequestDtoValidator : AbstractValidator<TransactionValidationRequestDto> 
{
    public TransactionValidationRequestDtoValidator()
    {
        RuleFor(x => x.FundsTransferSessionId)
        .NotNull()
        .NotEmpty()
        .MaximumLength(100);
    }
}