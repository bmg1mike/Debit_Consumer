namespace Sterling.NIPOutwardService.Service.Services.Implementations;

public class NIPOutwardDebitService : INIPOutwardDebitService
{
    private readonly INIPOutwardDebitProcessorService nipOutwardDebitProcessorService;
    private readonly INIPOutwardDebitProducerService nipOutwardDebitProducerService;
    private readonly INIPOutwardTransactionService nipOutwardTransactionService;
    private InboundLog inboundLog;
    private readonly IInboundLogService inboundLogService;
    private readonly IMapper mapper;
    public NIPOutwardDebitService(INIPOutwardDebitProcessorService nipOutwardDebitProcessorService, 
    INIPOutwardDebitProducerService nipOutwardDebitProducerService, IInboundLogService inboundLogService,
    INIPOutwardTransactionService nipOutwardTransactionService, IMapper mapper)
    {
        this.nipOutwardDebitProcessorService = nipOutwardDebitProcessorService;
        this.nipOutwardDebitProducerService = nipOutwardDebitProducerService;
        this.inboundLog = new InboundLog {
            InboundLogId = ObjectId.GenerateNewId().ToString(), 
            OutboundLogs = new List<OutboundLog>(),
            };
        this.inboundLogService = inboundLogService;
        this.nipOutwardTransactionService = nipOutwardTransactionService;
        this.mapper = mapper;
    }

    public async Task<FundsTransferResult<string>> ProcessTransaction(CreateNIPOutwardTransactionDto request)
    {
        var response = new FundsTransferResult<string>();
        inboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        inboundLog.APICalled = "NIPOutwardService";
        inboundLog.APIMethod = "FundsTransfer";

        inboundLog.RequestDetails = JsonConvert.SerializeObject(request);
        
        var createTransactionResult = await CreateTransaction(request);
        

        if(!createTransactionResult.IsSuccess)
        {
            await inboundLogService.CreateInboundLog(inboundLog);
            inboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            inboundLog.ResponseDetails = JsonConvert.SerializeObject(createTransactionResult);
            return mapper.Map<FundsTransferResult<string>>(createTransactionResult);
        }

        if(request.PriorityLevel == 1)
        {
            response =  await nipOutwardDebitProcessorService.ProcessTransaction(createTransactionResult.Content);
            inboundLog.OutboundLogs.AddRange(nipOutwardDebitProcessorService.GetOutboundLogs());
        }
        else
        {
            response = await nipOutwardDebitProducerService.PublishTransaction(createTransactionResult.Content);
            inboundLog.OutboundLogs.Add(nipOutwardDebitProducerService.GetOutboundLog());
        }           
        
        
        inboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
        inboundLog.ResponseDetails = JsonConvert.SerializeObject(response);
        await inboundLogService.CreateInboundLog(inboundLog);
        
        return response;
    }

    public async Task<FundsTransferResult<NIPOutwardTransaction>> CreateTransaction(CreateNIPOutwardTransactionDto request) 
    {
        FundsTransferResult<NIPOutwardTransaction> result = new FundsTransferResult<NIPOutwardTransaction>();
        result.IsSuccess = false;
        try
        {
            var insertRecordResult = await nipOutwardTransactionService.Create(request);
            inboundLog.OutboundLogs.Add(nipOutwardTransactionService.GetOutboundLog());

            return insertRecordResult;

        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            result.Message = "Internal Server Error";
            inboundLog.ExceptionDetails = $@"Error thrown, raw request: {request} 
            Exception Details: {ex.Message} {ex.StackTrace}";
            return result;
        }
    }
}