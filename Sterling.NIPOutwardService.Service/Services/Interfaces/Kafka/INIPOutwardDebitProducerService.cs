namespace Sterling.NIPOutwardService.Service.Services.Interfaces.Kafka;

public interface INIPOutwardDebitProducerService 
{
    Task<Result<string>> PublishTransaction(CreateNIPOutwardTransactionDto request);
}