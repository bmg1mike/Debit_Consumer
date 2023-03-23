namespace Sterling.NIPOutwardService.Service.Services.Interfaces;

public partial interface INIPOutwardTransactionService 
{
    Task<Result<NIPOutwardTransaction>> Create(CreateNIPOutwardTransactionDto request);
    Result<NIPOutwardTransaction> ValidateCreateNIPOutwardTransactionDto(CreateNIPOutwardTransactionDto request);
    OutboundLog GetOutboundLog();
    Task<int> Update(NIPOutwardTransaction request);
}