namespace Sterling.NIPOutwardService.Service.Services.Interfaces;

public interface INIPOutwardDebitService 
{
    Task<Result<string>> ProcessTransaction(CreateNIPOutwardTransactionDto request);
}