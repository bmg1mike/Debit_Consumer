namespace Sterling.NIPOutwardService.Service.Services.Implementations.Kafka;

public class NIPOutwardDebitProducerService : INIPOutwardDebitProducerService
{
    public readonly IProducer<Null, byte[]> producer;
    public readonly AppSettings appSettings;
    public readonly KafkaDebitProducerConfig kafkaDebitProducerConfig;
    public readonly INIPOutwardDebitService nipOutwardDebitService;
    private readonly IInboundLogService inboundLogService;

    public NIPOutwardDebitProducerService(IProducer<Null, byte[]> producer, IOptions<AppSettings> appSettings,
        IOptions<KafkaDebitProducerConfig> kafkaDebitProducerConfig, INIPOutwardDebitService nipOutwardDebitService, 
        IInboundLogService inboundLogService)
    {
        this.producer = producer;
        this.appSettings = appSettings.Value;
        this.kafkaDebitProducerConfig = kafkaDebitProducerConfig.Value;
        this.nipOutwardDebitService = nipOutwardDebitService;
        this.inboundLogService = inboundLogService;
    }

    public async Task<Result<string>> PublishTransaction(CreateNIPOutwardTransactionDto request)
    {
        Result<string> result = new Result<string>();
        InboundLog inboundLog = new InboundLog {
            InboundLogId = ObjectId.GenerateNewId().ToString(), 
            OutboundLogs = new List<OutboundLog>(),
            };

        inboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        inboundLog.APICalled = "NIPOutwardDebitService";
        inboundLog.APIMethod = "FundsTransfer";

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
            inboundLog.ExceptionDetails = $@"Error thrown, raw request: {request} 
            Exception Details: {ex.Message} {ex.StackTrace}";
        }

        inboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
        await inboundLogService.CreateInboundLog(inboundLog);

        return result;
    }

    public async Task ProduceAsync (CreateNIPOutwardTransactionDto request) =>
        await producer.ProduceAsync(kafkaDebitProducerConfig.OutwardDebitTopic, new Message<Null, byte[]> 
        {
            Value = AvroConvert.Serialize(request),
        });

}