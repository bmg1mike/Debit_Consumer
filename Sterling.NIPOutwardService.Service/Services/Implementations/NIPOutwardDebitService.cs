namespace Sterling.NIPOutwardService.Service.Services.Implementations;

public class NIPOutwardDebitService : INIPOutwardDebitService
{
    private readonly INIPOutwardDebitProcessorService nipOutwardDebitProcessorService;
    private readonly INIPOutwardWalletTransactionService nipOutwardWalletTransactionService;

    private readonly INIPOutwardDebitProducerService nipOutwardDebitProducerService;
    private readonly INIPOutwardTransactionService nipOutwardTransactionService;
    private InboundLog inboundLog;
    private readonly IInboundLogService inboundLogService;
    private readonly IMapper mapper;
    private readonly IUtilityHelper utilityHelper;
    private readonly IHttpContextAccessor httpContextAccessor;
    public NIPOutwardDebitService(INIPOutwardDebitProcessorService nipOutwardDebitProcessorService, 
    INIPOutwardDebitProducerService nipOutwardDebitProducerService, IInboundLogService inboundLogService,
    INIPOutwardTransactionService nipOutwardTransactionService, IMapper mapper, IUtilityHelper utilityHelper,
    IHttpContextAccessor httpContextAccessor, INIPOutwardWalletTransactionService nipOutwardWalletTransactionService)
    {
        this.nipOutwardWalletTransactionService = nipOutwardWalletTransactionService;
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
        this.httpContextAccessor = httpContextAccessor;
    }

    public async Task<FundsTransferResult<string>> ProcessTransaction(CreateNIPOutwardTransactionDto request)
    {
        var response = new FundsTransferResult<string>();
        try
        {
            var requestTime = DateTime.UtcNow.AddHours(1);
            inboundLog.RequestDateTime = requestTime;
            inboundLog.APICalled = "NIPOutwardService";
            inboundLog.APIMethod = "FundsTransfer";
            inboundLog.RequestSystem = httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
            inboundLog.RequestDetails = JsonConvert.SerializeObject(request);

            response = await Process(request);

            response.RequestTime = requestTime;
            response.PaymentReference = request.PaymentReference;
            response.ResponseTime = DateTime.UtcNow.AddHours(1);
            response.Content = string.Empty;
            inboundLog.ResponseDetails = JsonConvert.SerializeObject(response);
            inboundLog.ResponseDateTime = response.ResponseTime;
            await inboundLogService.CreateInboundLog(inboundLog);
        }
        catch (System.Exception ex)
        {
            response.IsSuccess = false;
            response.ResponseTime = DateTime.UtcNow.AddHours(1);
            response.Content = string.Empty;
            response.Message = "Transaction failed";
            response.ErrorMessage = "Internal server error";
            Log.Information(ex, $"Error thrown, raw request: {JsonConvert.SerializeObject(request)} ");
        }
        
        return response;
    }

    public async Task<FundsTransferResult<string>> Process(CreateNIPOutwardTransactionDto request)
    {
        var response = new FundsTransferResult<string>();

        var validationResult = ValidateCreateNIPOutwardTransactionDto(request);

        if(!validationResult.IsSuccess)
        {
            response = mapper.Map<FundsTransferResult<string>>(validationResult);
            return response;
        }

        if(request.IsWalletTransaction)
        {
             var callWalletResult = await CallWalletService(request);

            if(!callWalletResult.IsSuccess)
            {
                response = mapper.Map<FundsTransferResult<string>>(callWalletResult);
                return response;
            }
        }
       
        
        var createTransactionResult = await CreateTransaction(request);
        
        if(!createTransactionResult.IsSuccess)
        {
            response = mapper.Map<FundsTransferResult<string>>(createTransactionResult);
            return response;
        }

        NIPOutwardTransaction nipOutwardTransaction = createTransactionResult.Content;

        var generateFundsTransferSessionIdResult = await GenerateFundsTransferSessionId(nipOutwardTransaction);

        if(!generateFundsTransferSessionIdResult.IsSuccess)
        {
            response = mapper.Map<FundsTransferResult<string>>(generateFundsTransferSessionIdResult);
            return response;
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
        
        return response;
    }

    public async Task<FundsTransferResult<string>> CallWalletService(CreateNIPOutwardTransactionDto request)
    {
        var nipOutwardWalletTransaction = mapper.Map<NIPOutwardWalletTransaction>(request);
        nipOutwardWalletTransaction.DateAdded = DateTime.UtcNow.AddHours(1);
        nipOutwardWalletTransaction.LastUpdate = DateTime.UtcNow.AddHours(1);
        
        var result = await nipOutwardWalletTransactionService.ProcessTransaction(nipOutwardWalletTransaction);
        inboundLog.OutboundLogs.AddRange(nipOutwardWalletTransactionService.GetOutboundLogs());
        return result;
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

    public FundsTransferResult<NIPOutwardTransaction> ValidateCreateNIPOutwardTransactionDto(CreateNIPOutwardTransactionDto request)
    {
        FundsTransferResult<NIPOutwardTransaction> result = new FundsTransferResult<NIPOutwardTransaction>();
        result.IsSuccess = false;

        CreateNIPOutwardTransactionDtoValidator validator = new CreateNIPOutwardTransactionDtoValidator();
        ValidationResult results = validator.Validate(request);

        if (!results.IsValid)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var failure in results.Errors)
            {
                sb.Append("Property " + failure.PropertyName + " failed validation. Error was: " + failure.ErrorMessage);
            }

            result.IsSuccess = false;
            result.ErrorMessage = sb.ToString();
            result.Message = "Invalid transaction";

        }
        else{
             result.IsSuccess = true;
        }

        return result;
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

                var updateLog = nipOutwardTransactionService.GetOutboundLog();
                
                if(!string.IsNullOrEmpty(updateLog.ExceptionDetails))
                {
                    inboundLog.OutboundLogs.Add(updateLog);
                }

                if (recordsUpdated < 1)
                {
                    outboundLog.ExceptionDetails =  "Unable to update Session ID for transaction";
                    
                    transaction.StatusFlag = 0;
                    await nipOutwardTransactionService.Update(transaction);
                    result.Message = "Internal Server Error";
                    result.ErrorMessage = "Unable to update Session ID for transaction";
                    result.IsSuccess = false;
                }
                else{
                    outboundLog.ResponseDetails = "Session ID successfully updated for transaction";
                    result.IsSuccess = true;
                    result.Content = transaction;
                }

                outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                inboundLog.OutboundLogs.Add(outboundLog);
                
                #endregion 

            result.Content = transaction;
            return result;
        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            result.Message = "Internal Server Error";
            result.ErrorMessage = "Unable to update Session ID for transaction";
            var request = JsonConvert.SerializeObject(transaction);
            outboundLog.ExceptionDetails = $@"Error thrown, raw request: {request} 
            Exception Details: {ex.Message} {ex.StackTrace}";
            inboundLog.OutboundLogs.Add(outboundLog);
            return result;
        }
         
    }
}