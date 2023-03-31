namespace Sterling.NIPOutwardService.Service.Services.Implementations.Kafka;

public class NIPOutwardDebitProducerService : INIPOutwardDebitProducerService
{
    public readonly IProducer<Null, string> producer;
    public readonly AppSettings appSettings;
    public readonly KafkaDebitProducerConfig kafkaDebitProducerConfig;
    public readonly INIPOutwardDebitProcessorService nipOutwardDebitService;
    private OutboundLog outboundLog;

    public NIPOutwardDebitProducerService(IProducer<Null, string> producer, IOptions<AppSettings> appSettings,
        IOptions<KafkaDebitProducerConfig> kafkaDebitProducerConfig, INIPOutwardDebitProcessorService nipOutwardDebitService)
    {
        this.producer = producer;
        this.appSettings = appSettings.Value;
        this.kafkaDebitProducerConfig = kafkaDebitProducerConfig.Value;
        this.nipOutwardDebitService = nipOutwardDebitService;
        this.outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
    }

    public async Task<FundsTransferResult<string>> PublishTransaction(NIPOutwardTransaction request)
    {
        FundsTransferResult<string> result = new FundsTransferResult<string>();
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.PublishTransaction)}";

        try
        {
            await ProduceAsync(request);
            result.IsSuccess = true;
            result.Message = "Transaction has been pushed for processing";
            
        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            result.Message = "Internal Server Error";
            outboundLog.ExceptionDetails = $@"Error thrown, raw request: {request} 
            Exception Details: {ex.Message} {ex.StackTrace}";
        }
        //outboundLog.ResponseDetails = JsonConvert.SerializeObject(result);
        outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);

        return result;
    }

    public async Task ProduceAsync (NIPOutwardTransaction request) =>
        await producer.ProduceAsync(kafkaDebitProducerConfig.OutwardDebitTopic, new Message<Null, string> 
        {
            Value = JsonConvert.SerializeObject(request),
        });

    public OutboundLog GetOutboundLog()
    {
        var recordsToBeMoved = this.outboundLog;
        this.outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
        return recordsToBeMoved;
    }
}