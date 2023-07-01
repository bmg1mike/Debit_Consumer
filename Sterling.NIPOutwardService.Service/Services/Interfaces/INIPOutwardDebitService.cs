namespace Sterling.NIPOutwardService.Service.Services.Interfaces;

public interface INIPOutwardDebitService 
{
    Task<FundsTransferResult<string>> ProcessAndLog(CreateNIPOutwardTransactionDto request);
}