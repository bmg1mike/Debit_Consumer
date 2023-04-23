namespace Sterling.NIPOutwardService.Service.Services.Implementations;

public class TransactionAmountLimitService : ITransactionAmountLimitService
{
    private readonly IEFTTransactionAmountLimitRepository eftTransactionAmountLimitRepository;
    private readonly IConcessionTransactionAmountLimitRepository concessionTransactionAmountLimitRepository;
    private readonly ICBNTransactionAmountLimitRepository cbnTransactionAmountLimitRepository;
    private readonly AsyncRetryPolicy retryPolicy;
    private OutboundLog outboundLog;

    public TransactionAmountLimitService(IEFTTransactionAmountLimitRepository eftTransactionAmountLimitRepository,
    IConcessionTransactionAmountLimitRepository concessionTransactionAmountLimitRepository,
    ICBNTransactionAmountLimitRepository cbnTransactionAmountLimitRepository)
    {
        this.eftTransactionAmountLimitRepository = eftTransactionAmountLimitRepository;
        this.concessionTransactionAmountLimitRepository = concessionTransactionAmountLimitRepository;
        this.cbnTransactionAmountLimitRepository = cbnTransactionAmountLimitRepository;
        this.outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
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
    
    public async Task<ConcessionTransactionAmountLimit> GetConcessionLimitByDebitAccount(string debitAccountNumber)
    {
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.GetConcessionLimitByDebitAccount)}";
        outboundLog.RequestDetails = $"{debitAccountNumber}";

        ConcessionTransactionAmountLimit concessionTransactionAmountLimit = new();
        await retryPolicy.ExecuteAsync(async () =>
        {
            concessionTransactionAmountLimit = await concessionTransactionAmountLimitRepository.GetByDebitAccount(debitAccountNumber);
        });

        outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.ResponseDetails = JsonConvert.SerializeObject(concessionTransactionAmountLimit);

        return concessionTransactionAmountLimit;
    }

    public async Task<CBNTransactionAmountLimit> GetCBNLimitByCustomerClass(int customerClass)
    {
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.GetCBNLimitByCustomerClass)}";
        outboundLog.RequestDetails = $"customer class: {customerClass}";

        CBNTransactionAmountLimit cbnTransactionAmountLimit = new();
        await retryPolicy.ExecuteAsync(async () =>
        {
            cbnTransactionAmountLimit = await cbnTransactionAmountLimitRepository.GetByCustomerClass(customerClass);
        });

        outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.ResponseDetails = JsonConvert.SerializeObject(cbnTransactionAmountLimit);

        return cbnTransactionAmountLimit;
    }

    public async Task<EFTTransactionAmountLimit> GetEFTLimit()
    {
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.GetEFTLimit)}";

        EFTTransactionAmountLimit eftTransactionAmountLimit = new();
        await retryPolicy.ExecuteAsync(async () =>
        {
            eftTransactionAmountLimit = await eftTransactionAmountLimitRepository.GetFirstRecord();
        });

        outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.ResponseDetails = JsonConvert.SerializeObject(eftTransactionAmountLimit);

        return eftTransactionAmountLimit;
    }

    public OutboundLog GetOutboundLog()
    {
        var recordToBeMoved = this.outboundLog;
        this.outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString()};
        return recordToBeMoved;
    }
}