namespace Sterling.NIPOutwardService.Domain;

public partial class AutoMapping : Profile
{
    public AutoMapping()
    {
        CreateMap<CreateNIPOutwardTransactionDto, NIPOutwardTransaction>();
        CreateMap(typeof(Result<>), typeof(Result<string>));
        CreateMap<NIPOutwardTransaction, CreateVTellerTransactionDto>();
    }
}