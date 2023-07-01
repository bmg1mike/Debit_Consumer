namespace Sterling.NIPOutwardService.Data.Repositories.Interfaces;

public interface IDebitAccountRepository 
{
    Task<DebitAccountDetails?> GetDebitAccountDetails(string AccountNumber);
    OutboundLog GetOutboundLog();
}