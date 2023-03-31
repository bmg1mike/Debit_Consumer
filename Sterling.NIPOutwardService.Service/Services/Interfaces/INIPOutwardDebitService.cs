namespace Sterling.NIPOutwardService.Service.Services.Interfaces;

public interface INIPOutwardDebitService 
{
    Task<FundsTransferResult<string>> ProcessTransaction(CreateNIPOutwardTransactionDto request);
}