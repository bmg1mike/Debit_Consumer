namespace Sterling.NIPOutwardService.Data.Repositories.Interfaces.TransactionAmountLimits;

public interface ICBNTransactionAmountLimitRepository
{
    Task<CBNTransactionAmountLimit?> GetByCustomerClass(int customerClass);
}