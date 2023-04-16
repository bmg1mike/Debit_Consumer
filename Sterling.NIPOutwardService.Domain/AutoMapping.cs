using Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.WalletFraudAnalytics;

namespace Sterling.NIPOutwardService.Domain;

public partial class AutoMapping : Profile
{
    public AutoMapping()
    {
        CreateMap<CreateNIPOutwardTransactionDto, NIPOutwardTransaction>();
        CreateMap(typeof(FundsTransferResult<>), typeof(FundsTransferResult<string>));
        CreateMap<NIPOutwardTransaction, CreateVTellerTransactionDto>();
        CreateMap<CreateNIPOutwardTransactionDto, NIPOutwardWalletTransaction>();
    }
}