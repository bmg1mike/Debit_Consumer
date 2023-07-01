namespace Sterling.NIPOutwardService.Service.Services.Implementations.Kafka;


public class NIPOutwardSendToNIBSSProducerService : INIPOutwardSendToNIBSSProducerService
{
    public readonly IProducer<Null, string> producer;
    public readonly AppSettings appSettings;
    public readonly KafkaSendToNIBSSProducerConfig kafkaSendToNIBSSProducerConfig;
    public readonly KafkaImalSendToNIBSSProducerConfig kafkaImalSendToNIBSSProducerConfig;
    private OutboundLog outboundLog;

    public NIPOutwardSendToNIBSSProducerService(IProducer<Null, string> producer, IOptions<AppSettings> appSettings,
        IOptions<KafkaSendToNIBSSProducerConfig> kafkaSendToNIBSSProducerConfig,
         IOptions<KafkaImalSendToNIBSSProducerConfig> kafkaImalSendToNIBSSProducerConfig)
    {
        this.producer = producer;
        this.appSettings = appSettings.Value;
        this.kafkaSendToNIBSSProducerConfig = kafkaSendToNIBSSProducerConfig.Value;
        this.kafkaImalSendToNIBSSProducerConfig = kafkaImalSendToNIBSSProducerConfig.Value;
        this.outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
    }

    public async Task<FundsTransferResult<string>> PublishTransaction(NIPOutwardTransaction request)
    {
        FundsTransferResult<string> result = new FundsTransferResult<string>();
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.PublishTransaction)}";

        try
        {
            if(request.IsImalTransaction)
            {
                await ProduceImalAsync(request);
            }
            else
            {
                await ProduceAsync(request);
            }
            result.IsSuccess = true;
            result.Message = "Transaction has been pushed for processing";
            
        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            result.Message = "Transaction failed";
            result.ErrorMessage = "Internal Server Error";
            outboundLog.ExceptionDetails = $@"Error thrown, raw request: {request} 
            Exception Details: {ex.Message} {ex.StackTrace}";
        }
        //outboundLog.ResponseDetails = JsonConvert.SerializeObject(result);
        outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);

        return result;
    }

    public async Task ProduceAsync (NIPOutwardTransaction request) =>
        await producer.ProduceAsync(kafkaSendToNIBSSProducerConfig.OutwardSendToNIBSSTopic, new Message<Null, string> 
        {
            Value = JsonConvert.SerializeObject(request),
        });

    public async Task ProduceImalAsync (NIPOutwardTransaction request) =>
        await producer.ProduceAsync(kafkaImalSendToNIBSSProducerConfig.OutwardSendToNIBSSTopic, new Message<Null, string> 
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