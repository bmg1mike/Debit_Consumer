namespace Sterling.NIPOutwardService.Data.Repositories.Interfaces.TransactionAmountLimits;

public interface IEFTTransactionAmountLimitRepository 
{
    Task<EFTTransactionAmountLimit> GetFirstRecord();
}