namespace Sterling.NIPOutwardService.Service.Services.Implementations.Kafka;

public class NIPOutwardDebitProducerService : INIPOutwardDebitProducerService
{
    private readonly IProducer<Null, string> producer;
    private readonly AppSettings appSettings;
    private readonly KafkaDebitProducerConfig kafkaDebitProducerConfig;
    private readonly INIPOutwardDebitProcessorService nipOutwardDebitService;
    private readonly INIPOutwardTransactionService nipOutwardTransactionService;
    private OutboundLog outboundLog;
    private List<OutboundLog> outboundLogs;

    public NIPOutwardDebitProducerService(IProducer<Null, string> producer, IOptions<AppSettings> appSettings,
        IOptions<KafkaDebitProducerConfig> kafkaDebitProducerConfig, INIPOutwardDebitProcessorService nipOutwardDebitService,
        INIPOutwardTransactionService nipOutwardTransactionService)
    {
        this.producer = producer;
        this.appSettings = appSettings.Value;
        this.kafkaDebitProducerConfig = kafkaDebitProducerConfig.Value;
        this.nipOutwardDebitService = nipOutwardDebitService;
        this.outboundLogs = new List<OutboundLog> ();
        this.nipOutwardTransactionService = nipOutwardTransactionService;
    }

    public async Task<FundsTransferResult<string>> PublishTransaction(NIPOutwardTransaction request)
    {
        FundsTransferResult<string> result = new FundsTransferResult<string>();
        var outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.PublishTransaction)}";

        try
        {
            await ProduceAsync(request);
            result.IsSuccess = true;
            result.Message = "Transaction has been pushed for processing";

            request.KafkaStatus = "K1";
            await nipOutwardTransactionService.Update(request);
            outboundLogs.Add(nipOutwardTransactionService.GetOutboundLog());
            
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
        outboundLogs.Add(outboundLog);
        return result;
    }

    public async Task ProduceAsync (NIPOutwardTransaction request) =>
        await producer.ProduceAsync(kafkaDebitProducerConfig.OutwardDebitTopic, new Message<Null, string> 
        {
            Value = JsonConvert.SerializeObject(request),
        });

    public List<OutboundLog> GetOutboundLogs()
    {
        var recordsToBeMoved = this.outboundLogs;
        this.outboundLogs = new List<OutboundLog>();
        return recordsToBeMoved;
    }
}