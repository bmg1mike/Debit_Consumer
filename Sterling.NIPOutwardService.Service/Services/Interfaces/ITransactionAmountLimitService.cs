namespace Sterling.NIPOutwardService.Service.Services.Interfaces;

public interface ITransactionAmountLimitService
{
    Task<ConcessionTransactionAmountLimit> GetConcessionLimitByDebitAccount(string debitAccountNumber);
    Task<CBNTransactionAmountLimit> GetCBNLimitByCustomerClass(int customerClass);
    Task<EFTTransactionAmountLimit> GetEFTLimit();
    OutboundLog GetOutboundLog();
}