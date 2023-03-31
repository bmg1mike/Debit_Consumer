namespace Sterling.NIPOutwardService.Domain.DataTransferObjects.DtoValidators;

public partial class CreateNIPOutwardTransactionDtoValidator : AbstractValidator<CreateNIPOutwardTransactionDto>
{
    public CreateNIPOutwardTransactionDtoValidator()
    {
        RuleFor(x => x.NameEnquirySessionID)
        .MaximumLength(30)
        .NotEmpty()
        .NotNull();

        RuleFor(x => x.TransactionCode)
        .NotNull()
        .NotEmpty()
        .MaximumLength(50);

        RuleFor(x => x.ChannelCode)
        .NotNull();

        RuleFor(x => x.PaymentReference)
        .NotNull()
        .NotEmpty()
        .MaximumLength(100);

        RuleFor(x => x.Amount)
        .NotNull()
        .NotEmpty()
        .GreaterThan(0)
        .ScalePrecision(2,18);

        RuleFor(x => x.CreditAccountName)
        .NotNull()
        .NotEmpty()
        .MaximumLength(150);

        RuleFor(x => x.CreditAccountNumber)
        .NotNull()
        .NotEmpty()
        .MaximumLength(10);

        RuleFor(x => x.OriginatorName)
        .NotNull()
        .NotEmpty()
        .MaximumLength(150);

        RuleFor(x => x.BranchCode)
        .NotNull()
        .NotEmpty()
        .MaximumLength(10);

        RuleFor(x => x.CustomerID)
        .NotNull()
        .NotEmpty()
        .MaximumLength(10);

        RuleFor(x => x.CurrencyCode)
        .NotNull()
        .NotEmpty()
        .Equal("NGN")
        .MaximumLength(4);

        RuleFor(x => x.LedgerCode)
        .NotNull()
        .NotEmpty()
        .MaximumLength(10);

        RuleFor(x => x.SubAccountCode)
        .NotNull()
        .NotEmpty()
        .MaximumLength(4);

        RuleFor(x => x.NameResponse)
        .NotNull()
        .NotEmpty()
        .MaximumLength(3);

        RuleFor(x => x.DebitAccountNumber)
        .NotNull()
        .NotEmpty()
        .MaximumLength(10);

        RuleFor(x => x.BeneficiaryBankCode)
        .NotNull()
        .NotEmpty()
        .MaximumLength(10);

        RuleFor(x => x.OriginatorBVN)
        .NotNull()
        .NotEmpty()
        .Length(11);

        RuleFor(x => x.BeneficiaryBVN)
        .NotNull()
        .NotEmpty()
        .Length(11);

        RuleFor(x => x.BeneficiaryKYCLevel)
        .NotNull()
        .NotEmpty()
        .MaximumLength(10);

        RuleFor(x => x.OriginatorKYCLevel)
        .NotNull()
        .NotEmpty()
        .MaximumLength(10);

        RuleFor(x => x.AppId)
        .NotNull()
        .NotEmpty()
        .LessThan(int.MaxValue);

        RuleFor(x => x.PriorityLevel)
        .NotNull()
        .NotEmpty()
        .LessThan(255)
        .GreaterThan(0);
    }
}