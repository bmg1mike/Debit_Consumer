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
    public NIPOutwardTransactionService(INIPOutwardTransactionRepository repository, IMapper mapper, 
    IInboundLogService inboundLogService)
    {
        this.outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString()};
        this.repository = repository;
        this.mapper = mapper;
        this.inboundLog = new InboundLog {
            InboundLogId = ObjectId.GenerateNewId().ToString(), 
            OutboundLogs = new List<OutboundLog>(),
            };
        this.inboundLogService = inboundLogService;
        this.retryPolicy = Policy.Handle<Exception>()
        .WaitAndRetryAsync(new[]
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(4)
        }, (exception, timeSpan, retryCount, context) =>
        {
            outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + "\r\n" + @$"Retrying due to {exception.GetType().Name}... Attempt {retryCount}
                Exception Details: {exception.Message} {exception.StackTrace} " ;
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
            
            var validationResult = ValidateCreateNIPOutwardTransactionDto(request);

            if(!validationResult.IsSuccess){
                outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                outboundLog.ResponseDetails = validationResult.ErrorMessage;
                inboundLog.OutboundLogs.Add(outboundLog);
                return validationResult;
            }
            
            var model = mapper.Map<NIPOutwardTransaction>(request);
            model.StatusFlag = 3;
            model.DateAdded = DateTime.UtcNow.AddHours(1);
            await repository.Create(model);

            result.IsSuccess = true;
            result.Message = "Success";
            result.Content = model;

        }
        catch (System.Exception ex)
        {
            var rawRequest = JsonConvert.SerializeObject(request);
            result.IsSuccess = false;
            result.Message = "Internal Server Error";
            outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + 
            "\r\n" + $@"Raw Request {rawRequest} Exception Details: {ex.Message} {ex.StackTrace}";
            
        }
        outboundLog.ResponseDetails = result.Message;
        outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
        return result;
    }

    public async Task<int> Update(NIPOutwardTransaction request)
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
            result.Message = sb.ToString();

           
        }
        else{
             result.IsSuccess = true;
        }

        return result;
    }

    public async Task<FundsTransferResult<string>> CheckIfTransactionIsSuccesful(TransactionValidationRequestDto request)
    {
        inboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        inboundLog.APIMethod = $"{this.ToString()}.{nameof(this.CheckIfTransactionIsSuccesful)}";
        inboundLog.RequestDetails = $@"PaymentReference {request.PaymentReference}";

        FundsTransferResult<string> result = new FundsTransferResult<string>();
        result.IsSuccess = false;
        try
        {
            var validationResult = ValidateTransactionValidationRequestDto(request);

            if(!validationResult.IsSuccess)
            {
                inboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                inboundLog.ResponseDetails = validationResult.ErrorMessage;
                await inboundLogService.CreateInboundLog(inboundLog);
                return validationResult;
            }

            var checkIfTransactionIsSuccessfulResult = false;
            await retryPolicy.ExecuteAsync(async () =>
            {
                checkIfTransactionIsSuccessfulResult = await repository.CheckIfTransactionIsSuccessful(request.PaymentReference);

            });

            result.IsSuccess = checkIfTransactionIsSuccessfulResult;
            inboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            inboundLog.ResponseDetails = $"Transaction successful: {checkIfTransactionIsSuccessfulResult}";

            if (checkIfTransactionIsSuccessfulResult)
            {
                result.Message = "Transaction is successful";
            }
            else{
                result.Message = "Transaction processing";
            } 
            
        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            result.Message = "Internal Server Error";
            inboundLog.ExceptionDetails = $@"PaymentReference {request.PaymentReference} Exception Details: {ex.Message} {ex.StackTrace}";
            
        }
        inboundLog.OutboundLogs.Add(outboundLog);
        await inboundLogService.CreateInboundLog(inboundLog);
        return result;
    }

    public FundsTransferResult<string> ValidateTransactionValidationRequestDto(TransactionValidationRequestDto request)
    {
        FundsTransferResult<string> result = new FundsTransferResult<string>();
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
            result.Message = sb.ToString();

           
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