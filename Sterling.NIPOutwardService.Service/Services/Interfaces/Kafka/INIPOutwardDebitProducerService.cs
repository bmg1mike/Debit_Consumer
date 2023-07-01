namespace Sterling.NIPOutwardService.Service.Services.Interfaces.Kafka;

public interface INIPOutwardDebitProducerService 
{
    Task<FundsTransferResult<string>> PublishTransaction(NIPOutwardTransaction request);
    List<OutboundLog> GetOutboundLogs();
}