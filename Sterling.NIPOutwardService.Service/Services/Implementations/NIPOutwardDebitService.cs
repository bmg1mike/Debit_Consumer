using Microsoft.AspNetCore.Http;

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
    private readonly IHttpContextAccessor httpContextAccessor;
    public NIPOutwardDebitService(INIPOutwardDebitProcessorService nipOutwardDebitProcessorService, 
    INIPOutwardDebitProducerService nipOutwardDebitProducerService, IInboundLogService inboundLogService,
    INIPOutwardTransactionService nipOutwardTransactionService, IMapper mapper, IUtilityHelper utilityHelper,
    IHttpContextAccessor httpContextAccessor)
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
        this.httpContextAccessor = httpContextAccessor;
    }

    public async Task<FundsTransferResult<string>> ProcessTransaction(CreateNIPOutwardTransactionDto request)
    {
        var response = new FundsTransferResult<string>();
        response.RequestTime = DateTime.UtcNow.AddHours(1);
        response.PaymentReference = request.PaymentReference;

        inboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        inboundLog.APICalled = "NIPOutwardService";
        inboundLog.APIMethod = "FundsTransfer";
        inboundLog.RequestSystem = httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();

        inboundLog.RequestDetails = JsonConvert.SerializeObject(request);
        
        var createTransactionResult = await CreateTransaction(request);
        
        if(!createTransactionResult.IsSuccess)
        {
            response = mapper.Map<FundsTransferResult<string>>(createTransactionResult);
            response.ResponseTime = DateTime.UtcNow.AddHours(1);
            response.Content = string.Empty;
            inboundLog.ResponseDetails = JsonConvert.SerializeObject(response);
            inboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            await inboundLogService.CreateInboundLog(inboundLog);
            return response;
        }

        NIPOutwardTransaction nipOutwardTransaction = createTransactionResult.Content;

        var generateFundsTransferSessionIdResult = await GenerateFundsTransferSessionId(nipOutwardTransaction);

        if(!generateFundsTransferSessionIdResult.IsSuccess)
        {
            response = mapper.Map<FundsTransferResult<string>>(generateFundsTransferSessionIdResult);
            response.ResponseTime = DateTime.UtcNow.AddHours(1);
            response.Content = string.Empty;
            inboundLog.ResponseDetails = JsonConvert.SerializeObject(response);
            inboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            await inboundLogService.CreateInboundLog(inboundLog);
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
        response.ResponseTime = DateTime.UtcNow.AddHours(1);
        response.Content = string.Empty;
        response.PaymentReference = request.PaymentReference;
        inboundLog.ResponseDetails = JsonConvert.SerializeObject(response);
        inboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
        
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