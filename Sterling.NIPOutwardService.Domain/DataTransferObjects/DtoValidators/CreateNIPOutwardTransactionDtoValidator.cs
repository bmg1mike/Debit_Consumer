namespace Sterling.NIPOutwardService.Domain.DataTransferObjects.DtoValidators;

public partial class CreateNIPOutwardTransactionDtoValidator : AbstractValidator<CreateNIPOutwardTransactionDto>
{
    public CreateNIPOutwardTransactionDtoValidator()
    {
        RuleFor(x => x.NameEnquirySessionID)
        .Length(30)
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
        .Length(10);

        RuleFor(x => x.OriginatorName)
        .NotNull()
        .NotEmpty()
        .MaximumLength(150);

        RuleFor(x => x.BranchCode)
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
        .MaximumLength(10);

        RuleFor(x => x.SubAccountCode)
        .MaximumLength(4);

        RuleFor(x => x.NameEnquiryResponse)
        .NotNull()
        .NotEmpty()
        .MaximumLength(3)
        .Equal("00");

        RuleFor(x => x.DebitAccountNumber)
        .NotNull()
        .NotEmpty()
        .MaximumLength(11);

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