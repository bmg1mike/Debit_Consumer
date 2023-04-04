namespace Sterling.NIPOutwardService.Service.Services.Interfaces;

public interface INIPOutwardDebitService 
{
    Task<FundsTransferResult<FundsTransferResultContent>> ProcessTransaction(CreateNIPOutwardTransactionDto request);
}