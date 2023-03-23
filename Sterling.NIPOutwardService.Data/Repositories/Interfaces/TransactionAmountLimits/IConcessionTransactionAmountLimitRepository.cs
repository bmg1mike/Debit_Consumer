namespace Sterling.NIPOutwardService.Data.Repositories.Interfaces.TransactionAmountLimits;

public interface IConcessionTransactionAmountLimitRepository 
{
    Task<ConcessionTransactionAmountLimit> GetByDebitAccount(string debitAccountNumber);
}