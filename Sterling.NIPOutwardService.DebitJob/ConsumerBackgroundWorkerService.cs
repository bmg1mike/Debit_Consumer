using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Sterling.NIPOutwardService.Service.Services.Implementations;

namespace Sterling.NIPOutwardService.DebitJob;
public class ConsumerBackgroundWorkerService : BackgroundService
{
    //public readonly INIPOutwardDebitService nipOutwardDebitService;
    public readonly IConsumer<Ignore, byte[]> consumer;
    public readonly KafkaDebitConsumerConfig kafkaDebitConsumerConfig;
    public readonly IServiceProvider serviceProvider;
    public ConsumerBackgroundWorkerService(//INIPOutwardDebitService nipOutwardDebitService, 
    IConsumer<Ignore, byte[]> consumer, IOptions<KafkaDebitConsumerConfig> kafkaDebitConsumerConfig,
    IServiceProvider serviceProvider)
    {
        //this.nipOutwardDebitService = nipOutwardDebitService;
        this.consumer = consumer;
        this.kafkaDebitConsumerConfig = kafkaDebitConsumerConfig.Value;
        this.serviceProvider = serviceProvider;
    }
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        
        consumer.Subscribe(kafkaDebitConsumerConfig.OutwardDebitTopic);

        while(!stoppingToken.IsCancellationRequested)
        {
            
            var consumeResult = consumer.Consume(stoppingToken);

            if(consumeResult != null)
            {
                var nipOutwardTransaction = AvroConvert.Deserialize<CreateNIPOutwardTransactionDto>(consumeResult.Message.Value);
                Console.WriteLine(JsonConvert.SerializeObject(nipOutwardTransaction));

                using (var scope = serviceProvider.CreateScope()) // this will use `IServiceScopeFactory` internally
                {
                    var nipOutwardDebitService = scope.ServiceProvider.GetRequiredService<NIPOutwardDebitService>();
                    await nipOutwardDebitService.ProcessTransaction(nipOutwardTransaction);
                }
            }

            if(kafkaDebitConsumerConfig.ConsumerConfig.EnableAutoOffsetStore == false)
                consumer.StoreOffset(consumeResult);
         }
    }
}