namespace Sterling.NIPOutwardService.Service.Services.Interfaces.Kafka;

public interface INIPOutwardSendToNIBSSProducerService 
{
    Task<FundsTransferResult<string>> PublishTransaction(NIPOutwardTransaction request);
    OutboundLog GetOutboundLog();
}