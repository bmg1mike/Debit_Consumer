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
    public NIPOutwardTransactionService(INIPOutwardTransactionRepository repository, IMapper mapper, 
    IInboundLogService inboundLogService, IHttpContextAccessor httpContextAccessor)
    {
        this.outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString()};
        this.repository = repository;
        this.mapper = mapper;
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
            await repository.Create(model);

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

    public async Task<Result<string>> CheckIfTransactionIsSuccessful(TransactionValidationRequestDto request)
    {
        var response = new Result<string>();
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
            response.Content = string.Empty;
            inboundLog.ResponseDetails = JsonConvert.SerializeObject(response);
            inboundLog.ResponseDateTime = response.ResponseTime;

            if(!string.IsNullOrEmpty(outboundLog.ExceptionDetails))
            {
                inboundLog.OutboundLogs.Add(outboundLog);
            }

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

    public async Task<Result<string>> ProcessCheck(TransactionValidationRequestDto request)
    {
        Result<string> result = new Result<string>();
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
            }
            else if(transaction.NIBSSResponse == "00")
            {
                result.Message = "Transaction is successful";
                result.IsSuccess = true;
            }
            else 
            {
                result.Message = "Transaction processing";
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

    public Result<string> ValidateTransactionValidationRequestDto(TransactionValidationRequestDto request)
    {
        Result<string> result = new Result<string>();
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