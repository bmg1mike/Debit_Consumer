namespace Sterling.NIPOutwardService.Service.Services.Interfaces;

public interface INIPOutwardDebitProcessorService 
{
    Task<Result<string>> ProcessTransaction(NIPOutwardTransaction nipOutwardTransaction);
    List<OutboundLog> GetOutboundLogs();
}