namespace Sterling.NIPOutwardService.Service.Services.Interfaces;

public interface INIPOutwardDebitProcessorService 
{
    Task<FundsTransferResult<string>> ProcessTransaction(NIPOutwardTransaction nipOutwardTransaction);
    List<OutboundLog> GetOutboundLogs();
}