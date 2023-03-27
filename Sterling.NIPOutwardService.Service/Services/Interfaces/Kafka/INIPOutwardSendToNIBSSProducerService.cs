namespace Sterling.NIPOutwardService.Service.Services.Interfaces.Kafka;

public interface INIPOutwardSendToNIBSSProducerService 
{
    Task<Result<string>> PublishTransaction(NIPOutwardTransaction request);
    OutboundLog GetOutboundLog();
}