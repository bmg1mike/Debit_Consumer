using System.Reflection;

namespace Sterling.NIPOutwardService.Service.Services.Implementations;

public partial class NIPOutwardTransactionService : INIPOutwardTransactionService
{
    private readonly INIPOutwardTransactionRepository repository;
    private readonly IMapper mapper;
    private readonly AsyncRetryPolicy retryPolicy;
    private OutboundLog outboundLog;
    public NIPOutwardTransactionService(INIPOutwardTransactionRepository repository, IMapper mapper)
    {
        this.outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString()};
        this.repository = repository;
        this.mapper = mapper;
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

    public async Task<Result<NIPOutwardTransaction>> Create(CreateNIPOutwardTransactionDto request)
    {
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.Create)}";
        Result<NIPOutwardTransaction> result = new Result<NIPOutwardTransaction>();
        result.IsSuccess = false;
        try
        {
            
            var validationResult = ValidateCreateNIPOutwardTransactionDto(request);

            if(!validationResult.IsSuccess){
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

    // public async Task<Result<string>> Create(NIPOutwardTransaction request)
    // {
    //     Result<NIPOutwardTransaction> result = new Result<NIPOutwardTransaction>();
    //     result.IsSuccess = false;
    //     try
    //     {
    //         var model = mapper.Map<NIPOutwardTransaction>(request);
    //         await repository.Create(model);

    //         result.IsSuccess = true;
    //         result.Message = "Success";
    //         result.Content = model;

    //     }
    //     catch (System.Exception ex)
    //     {
    //         var rawRequest = JsonConvert.SerializeObject(request);
    //         result.IsSuccess = false;
    //         result.Message = "Internal Server Error";
    //         outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + 
    //         "\r\n" + $@"Raw Request {rawRequest} Exception Details: {ex.Message} {ex.StackTrace}";
            
    //     }

    //     return result;
    // }

    public async Task<int> Update(NIPOutwardTransaction request)
    {
        var recordsUpdated = 0;
        await retryPolicy.ExecuteAsync(async () =>
        {
            recordsUpdated = await repository.Update(request);
        });

        return recordsUpdated;
    }


    public Result<NIPOutwardTransaction> ValidateCreateNIPOutwardTransactionDto(CreateNIPOutwardTransactionDto request)
    {
        Result<NIPOutwardTransaction> result = new Result<NIPOutwardTransaction>();
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

    public OutboundLog GetOutboundLog()
    {
        var recordToBeMoved = this.outboundLog;
        this.outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
        return recordToBeMoved;
    }
}