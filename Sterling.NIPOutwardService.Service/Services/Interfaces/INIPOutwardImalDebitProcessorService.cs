namespace Sterling.NIPOutwardService.Service.Services.Interfaces;

public interface INIPOutwardImalDebitProcessorService 
{
    Task<FundsTransferResult<string>> ProcessTransaction(NIPOutwardTransaction nipOutwardTransaction);
    List<OutboundLog> GetOutboundLogs();
}