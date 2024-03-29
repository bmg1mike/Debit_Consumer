using System.Reflection;
namespace Sterling.NIPOutwardService.Service.Services.Implementations;

public partial class NIPOutwardTransactionService : INIPOutwardTransactionService
{
    private readonly INIPOutwardTransactionRepository repository;
    private readonly IMapper mapper;
    private readonly AsyncRetryPolicy retryPolicy;
    private OutboundLog outboundLog;
    private InboundLog inboundLog;
    private readonly IInboundLogService inboundLogService;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly AppSettings appSettings;
    public NIPOutwardTransactionService(INIPOutwardTransactionRepository repository, IMapper mapper, 
    IInboundLogService inboundLogService, IHttpContextAccessor httpContextAccessor, IOptions<AppSettings> appSettings)
    {
        this.outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString()};
        this.repository = repository;
        this.mapper = mapper;
        this.appSettings = appSettings.Value;
        this.inboundLog = new InboundLog {
            InboundLogId = ObjectId.GenerateNewId().ToString(), 
            OutboundLogs = new List<OutboundLog>(),
            };
        this.inboundLogService = inboundLogService;
        this.httpContextAccessor = httpContextAccessor;
        this.retryPolicy = Policy.Handle<Exception>()
        .WaitAndRetryAsync(new[]
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(4)
        }, (exception, timeSpan, retryCount, context) =>
        {
            outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + "\r\n" + @$"Retrying due to {exception.GetType().Name}... Attempt {retryCount}
             at {DateTime.UtcNow.AddHours(1).ToString()}   Exception Details: {exception.Message} {exception.StackTrace} " ;
        });
    }

    public async Task<FundsTransferResult<NIPOutwardTransaction>> Create(CreateNIPOutwardTransactionDto request)
    {
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.Create)}";
        FundsTransferResult<NIPOutwardTransaction> result = new FundsTransferResult<NIPOutwardTransaction>();
        result.IsSuccess = false;
        try
        {
            
            // var validationResult = ValidateCreateNIPOutwardTransactionDto(request);

            // if(!validationResult.IsSuccess){
            //     outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            //     outboundLog.ResponseDetails = validationResult.ErrorMessage;
            //     //inboundLog.OutboundLogs.Add(outboundLog);
            //     return validationResult;
            // }
            
            var model = mapper.Map<NIPOutwardTransaction>(request);
            model.StatusFlag = 3;
            model.DateAdded = DateTime.UtcNow.AddHours(1);

            if(request.IsWalletTransaction)
            {
                model.DebitAccountNumber = appSettings.OneBankWalletPoolAccount;
                model.WalletAccountNumber = request.DebitAccountNumber.Substring(1);
            }

            await retryPolicy.ExecuteAsync(async () =>
            {
                await repository.Create(model);
            });
            
            result.IsSuccess = true;
            result.Message = "Success";
            result.Content = model;

        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)  when (ex.InnerException.Message.Contains("duplicate key"))
        {
            var rawRequest = JsonConvert.SerializeObject(request);
            result.IsSuccess = false;
            result.Message = "Transaction failed. Duplicate request.";
            result.ErrorMessage = "Transaction failed. Duplicate request.";
            outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + 
            "\r\n" + $@"Raw Request {rawRequest} Exception Details: {ex.InnerException.Message} {ex.StackTrace}";
            
        }
        catch (System.Exception ex)
        {
            var rawRequest = JsonConvert.SerializeObject(request);
            result.IsSuccess = false;
            result.Message = "Transaction failed";
            result.ErrorMessage = "Internal server error";
            outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + 
            "\r\n" + $@"Raw Request {rawRequest} Exception Details: {ex.Message} {ex.StackTrace}";
            
        }
       
        outboundLog.ResponseDetails = result.Message;
        outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
        return result;
    }

    public async Task<int>  Update(NIPOutwardTransaction request)
    {
        var recordsUpdated = 0;
        await retryPolicy.ExecuteAsync(async () =>
        {
            recordsUpdated = await repository.Update(request);
        });

        return recordsUpdated;
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

    public async Task<Result<TransactionValidationResponseDto>> CheckIfTransactionIsSuccessful(TransactionValidationRequestDto request)
    {
        var response = new Result<TransactionValidationResponseDto>();
        try
        {
            var requestTime = DateTime.UtcNow.AddHours(1);
            inboundLog.RequestDateTime = requestTime;
            inboundLog.APICalled = "NIPOutwardService";
            inboundLog.APIMethod = "TransactionValidation";
            inboundLog.RequestDetails = JsonConvert.SerializeObject(request);
            inboundLog.RequestSystem = httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();

            response = await ProcessCheck(request);

            response.SessionID = request.SessionID;
            response.RequestTime = requestTime;
            response.ResponseTime = DateTime.UtcNow.AddHours(1);
            inboundLog.ResponseDetails = JsonConvert.SerializeObject(response);
            inboundLog.ResponseDateTime = response.ResponseTime;
            inboundLog.ImpactUniqueIdentifier = "Response Session ID";
            inboundLog.ImpactUniqueidentifierValue = response.SessionID;
            inboundLog.AlternateUniqueIdentifier = "Request Session ID";
            inboundLog.AlternateUniqueidentifierValue = request.SessionID;

            if (!string.IsNullOrEmpty(outboundLog.ExceptionDetails))
            {
                inboundLog.OutboundLogs.Add(outboundLog);
            }

            Task.Run(() => LogToMongoDb(inboundLog));
            //await inboundLogService.CreateInboundLog(inboundLog);
        }
        catch (System.Exception ex)
        {
            response.IsSuccess = false;
            response.ResponseTime = DateTime.UtcNow.AddHours(1);
            response.Content = null;
            response.Message = "Transaction failed";
            response.ErrorMessage = "Internal server error";
            Log.Information(ex, $"Error thrown, raw request: {JsonConvert.SerializeObject(request)} ");
        }
       
        return response;
    }

    public void LogToMongoDb(InboundLog log)
    {
        inboundLogService.CreateInboundLog(inboundLog);
        Thread.Sleep(1000);
    }

    public async Task<Result<TransactionValidationResponseDto>> ProcessCheck(TransactionValidationRequestDto request)
    {
        Result<TransactionValidationResponseDto> result = new Result<TransactionValidationResponseDto>();
        result.IsSuccess = false;
        try
        {
            var validationResult = ValidateTransactionValidationRequestDto(request);

            if(!validationResult.IsSuccess)
            {
                return validationResult;
            }

            NIPOutwardTransaction transaction = new();
            await retryPolicy.ExecuteAsync(async () =>
            {
                transaction = await repository.GetBySessionID(request.SessionID);
            });

            if(transaction == null)
            {
                result.Message = "Transaction not found";
                result.ErrorMessage = "Transaction not found";
                result.IsSuccess = false;
                return result;
            }
            
            result.Message = "Transaction found";
            result.IsSuccess = true;
            
            // If a transaction check failed
            if(transaction.DebitResponse == 0 && transaction.FundsTransferResponse == null && transaction.NIBSSResponse == null && transaction.KafkaStatus != "K1")
            {
                result.Content = new TransactionValidationResponseDto { Status = "F"};
            }
            // If transaction successful
            else if(transaction.NIBSSResponse == "00")
            {
                result.Content = new TransactionValidationResponseDto { Status = "S"};
            }
            // If Vteller(Debit service) returned error response
            else if(transaction.StatusFlag == 27 && transaction.DebitResponse == 2)
            {
                result.Content = new TransactionValidationResponseDto { Status = "F"};
            }
            // If Debit requery service returned 
            else if(!string.IsNullOrWhiteSpace(transaction.DebitRequeryStatus) && transaction.DebitRequeryStatus.Trim() != "PROCESSED")
            {
                result.Content = new TransactionValidationResponseDto { Status = "F"};
            }
            // If failed fraud check
            else if(transaction.StatusFlag == 11)
            {
                result.Content = new TransactionValidationResponseDto { Status = "F"};
            }
            // If failure from NIBSS
            else if(transaction.StatusFlag == 8)
            {
                result.Content = new TransactionValidationResponseDto { Status = "F"};
            }
            else if(transaction.KafkaStatus == "K2" && transaction.NIBSSResponse == null)
            {
                result.Content = new TransactionValidationResponseDto { Status = "P"};
            }
            else 
            {
                result.Message = "Transaction status unknown";
                result.ErrorMessage = "Transaction status unknown";
                result.IsSuccess = false;
            }
            
        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            result.Message = "Operation failed";
            result.ErrorMessage = "Internal Server Error";
            inboundLog.ExceptionDetails = $@"PaymentReference {request.SessionID} Exception Details: {ex.Message} {ex.StackTrace}";
            
        }
        
        return result;
    }

    

    public Result<TransactionValidationResponseDto> ValidateTransactionValidationRequestDto(TransactionValidationRequestDto request)
    {
        Result<TransactionValidationResponseDto> result = new Result<TransactionValidationResponseDto>();
        result.IsSuccess = false;

        TransactionValidationRequestDtoValidator validator = new TransactionValidationRequestDtoValidator();
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
            result.Message = "Invalid request";

           
        }
        else{
             result.IsSuccess = true;
        }

        return result;
    }

    public OutboundLog GetOutboundLog()
    {
        var recordToBeMoved = this.outboundLog;
        this.outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
        return recordToBeMoved;
    }
}