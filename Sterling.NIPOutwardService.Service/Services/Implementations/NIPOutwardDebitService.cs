namespace Sterling.NIPOutwardService.Service.Services.Implementations;

public class NIPOutwardDebitService : INIPOutwardDebitService
{
    private readonly INIPOutwardDebitProcessorService nipOutwardDebitProcessorService;
    private readonly INIPOutwardDebitProducerService nipOutwardDebitProducerService;
    private readonly INIPOutwardTransactionService nipOutwardTransactionService;
    private InboundLog inboundLog;
    private readonly IInboundLogService inboundLogService;
    private readonly IMapper mapper;
    private readonly IUtilityHelper utilityHelper;
    public NIPOutwardDebitService(INIPOutwardDebitProcessorService nipOutwardDebitProcessorService, 
    INIPOutwardDebitProducerService nipOutwardDebitProducerService, IInboundLogService inboundLogService,
    INIPOutwardTransactionService nipOutwardTransactionService, IMapper mapper, IUtilityHelper utilityHelper)
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
        this.utilityHelper = utilityHelper;
    }

    public async Task<Result<string>> ProcessTransaction(CreateNIPOutwardTransactionDto request)
    {
        var response = new Result<string>();
        inboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        inboundLog.APICalled = "NIPOutwardService";
        inboundLog.APIMethod = "FundsTransfer";

        inboundLog.RequestDetails = JsonConvert.SerializeObject(request);
        
        var createTransactionResult = await CreateTransaction(request);
        

        if(!createTransactionResult.IsSuccess)
        {
            inboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            inboundLog.ResponseDetails = JsonConvert.SerializeObject(createTransactionResult);
            await inboundLogService.CreateInboundLog(inboundLog);
            return mapper.Map<Result<string>>(createTransactionResult);
        }

        NIPOutwardTransaction nipOutwardTransaction = createTransactionResult.Content;

        var generateFundsTransferSessionIdResult = await GenerateFundsTransferSessionId(nipOutwardTransaction);

        if(!generateFundsTransferSessionIdResult.IsSuccess)
        {
            inboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            inboundLog.ResponseDetails = JsonConvert.SerializeObject(generateFundsTransferSessionIdResult);
            await inboundLogService.CreateInboundLog(inboundLog);
            return mapper.Map<Result<string>>(generateFundsTransferSessionIdResult);
        }

        nipOutwardTransaction = generateFundsTransferSessionIdResult.Content;

        if(request.PriorityLevel == 1)
        {
            response =  await nipOutwardDebitProcessorService.ProcessTransaction(nipOutwardTransaction);
            inboundLog.OutboundLogs.AddRange(nipOutwardDebitProcessorService.GetOutboundLogs());
        }
        else
        {
            response = await nipOutwardDebitProducerService.PublishTransaction(createTransactionResult.Content);
            inboundLog.OutboundLogs.AddRange(nipOutwardDebitProducerService.GetOutboundLogs());
        }           
        
        response.SessionID = nipOutwardTransaction.SessionID;

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

    public async Task<FundsTransferResult<NIPOutwardTransaction>> GenerateFundsTransferSessionId(NIPOutwardTransaction transaction)
    {
        FundsTransferResult<NIPOutwardTransaction> result = new FundsTransferResult<NIPOutwardTransaction>();
        OutboundLog outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.GenerateFundsTransferSessionId)}";
        result.IsSuccess = false;

        try
       { 
            #region GenerateFTSessionId
                transaction.SessionID = utilityHelper.GenerateFundsTransferSessionId(transaction.ID);
                var recordsUpdated = await nipOutwardTransactionService.Update(transaction);

                if (recordsUpdated < 1)
                {
                    outboundLog.ExceptionDetails =  "Unable to update FT SessionId for transaction with RefId " + transaction.ID ;
                    inboundLog.OutboundLogs.Add(outboundLog);
                    transaction.StatusFlag = 0;
                    await nipOutwardTransactionService.Update(transaction);
                    result.Message = "Internal Server Error";
                    result.IsSuccess = false;
                }
                else{
                    result.IsSuccess = true;
                    result.Content = transaction;
                }
                
                #endregion 

            result.Content = transaction;
            return result;
        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            result.Message = "Internal Server Error";
            var request = JsonConvert.SerializeObject(transaction);
            outboundLog.ExceptionDetails = $@"Error thrown, raw request: {request} 
            Exception Details: {ex.Message} {ex.StackTrace}";
            inboundLog.OutboundLogs.Add(outboundLog);
            return result;
        }
         
    }
}