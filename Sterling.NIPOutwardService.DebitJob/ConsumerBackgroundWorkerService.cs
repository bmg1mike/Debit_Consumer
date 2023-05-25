using System.Collections.Concurrent;
using MongoDB.Bson;
using Sterling.NIPOutwardService.Domain.Common;
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

                logger.Information($"starting call to process {nipOutwardTransactionResults.Count} transactions in parallel");

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
                                    var inboundLogService = scope.ServiceProvider.GetRequiredService<InboundLogService>();
                                     var inboundLog = new InboundLog {
                                        InboundLogId = ObjectId.GenerateNewId().ToString(), 
                                        OutboundLogs = new List<OutboundLog>(),
                                    };
                                    inboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);;
                                    inboundLog.APICalled = "NIPOutwardService(Consumer)";
                                    inboundLog.APIMethod = "FundsTransfer";
                                    inboundLog.RequestDetails = transactionResult.Message.Value;

                                    var result = new FundsTransferResult<string>();
                                    if(nipOutwardTransaction.IsImalTransaction)
                                    {
                                        var nipOutwardImalDebitProcessorService = scope.ServiceProvider.GetRequiredService<NIPOutwardImalDebitProcessorService>();
                                        result = nipOutwardImalDebitProcessorService.ProcessTransaction(nipOutwardTransaction).Result;
                                        inboundLog.OutboundLogs.AddRange(nipOutwardImalDebitProcessorService.GetOutboundLogs());
                                    }
                                    else
                                    {
                                        var nipOutwardDebitProcessorService = scope.ServiceProvider.GetRequiredService<NIPOutwardDebitProcessorService>();
                                        result = nipOutwardDebitProcessorService.ProcessTransaction(nipOutwardTransaction).Result;
                                        inboundLog.OutboundLogs.AddRange(nipOutwardDebitProcessorService.GetOutboundLogs());
                                    }

                                    inboundLog.ResponseDetails = JsonConvert.SerializeObject(result);
                                    inboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                                    var logResult = inboundLogService.CreateInboundLog(inboundLog).Result;
                                }
                            }
                        }
                    } 
                    catch (System.Exception ex)
                    {
                        logger.Error(ex.Message + $"raw request {transactionResult.Message.Value}", ex);
                    }

                    if(kafkaDebitConsumerConfig.ConsumerConfig.EnableAutoOffsetStore == false)
                        consumer.StoreOffset(transactionResult);
                    
                });

                if (parallelLoopResult.IsCompleted)
                {
                    logger.Information($"call to process {nipOutwardTransactionResults.Count} transactions in parallel ended");
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