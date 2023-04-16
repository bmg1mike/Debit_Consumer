namespace Sterling.NIPOutwardService.Service.Services.Interfaces;

public interface INIPOutwardWalletTransactionService 
{
    Task<FundsTransferResult<string>> ProcessTransaction(NIPOutwardWalletTransaction request);
    List<OutboundLog> GetOutboundLogs();
}