using System.Collections.Concurrent;
using Sterling.NIPOutwardService.Domain.Entities;

namespace Sterling.NIPOutwardService.DebitJob;
public class ConsumerBackgroundWorkerService : BackgroundService
{
    public readonly IConsumer<Ignore, string> consumer;
    public readonly KafkaDebitConsumerConfig kafkaDebitConsumerConfig;
    public readonly IServiceProvider serviceProvider;
    private readonly Serilog.ILogger logger;
    public ConsumerBackgroundWorkerService(IConsumer<Ignore, string> consumer, 
    IOptions<KafkaDebitConsumerConfig> kafkaDebitConsumerConfig,
    IServiceProvider serviceProvider, Serilog.ILogger logger)
    {
        this.consumer = consumer;
        this.kafkaDebitConsumerConfig = kafkaDebitConsumerConfig.Value;
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            consumer.Subscribe(kafkaDebitConsumerConfig.OutwardDebitTopic);

            while(!stoppingToken.IsCancellationRequested)
            {
                
                var nipOutwardTransactionResults = new ConcurrentBag<ConsumeResult<Ignore, string>>();

                for (int i = 0; i < kafkaDebitConsumerConfig.NumberOfTransactionToConsume; i++)
                {
                    var consumedResult = consumer.Consume(TimeSpan.Zero);
                    if(consumedResult == null)
                        break;
                    else
                        nipOutwardTransactionResults.Add(consumedResult);
                    //nipOutwardTransactionResults.Add(consumer.Consume(stoppingToken));
                }

                logger.Information($"starting call to process {kafkaDebitConsumerConfig.NumberOfTransactionToConsume} transactions in parallel");

                ParallelLoopResult parallelLoopResult = Parallel.ForEach(nipOutwardTransactionResults, transactionResult => 
                {
                    try
                    {
                        if(transactionResult != null)
                        {
                            var nipOutwardTransaction = JsonConvert.DeserializeObject<NIPOutwardTransaction>(transactionResult.Message.Value);
                            
                            if(nipOutwardTransaction != null)
                            {
                                using (var scope = serviceProvider.CreateScope()) // this will use `IServiceScopeFactory` internally
                                {
                                    var nipOutwardDebitService = scope.ServiceProvider.GetRequiredService<NIPOutwardDebitProcessorService>();
                                    var result = nipOutwardDebitService.ProcessTransaction(nipOutwardTransaction).Result;
                                }
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        logger.Error(ex.Message, ex);
                    }

                    if(kafkaDebitConsumerConfig.ConsumerConfig.EnableAutoOffsetStore == false)
                        consumer.StoreOffset(transactionResult);
                    
                });

                if (parallelLoopResult.IsCompleted)
                {
                    logger.Information($"call to process {kafkaDebitConsumerConfig.NumberOfTransactionToConsume} transactions in parallel ended");
                    continue;
                }
            }
        }
        catch (System.Exception ex)
        {
            logger.Error(ex.Message, ex);
           
        }

    }
}