namespace Sterling.NIPOutwardService.Service.Services.Implementations;

public class NIPOutwardWalletTransactionService : INIPOutwardWalletTransactionService
{
    private readonly INIPOutwardWalletTransactionRepository nipOutwardWalletTransactionRepository;
    private List<OutboundLog> outboundLogs;
    private readonly AsyncRetryPolicy retryPolicy;
    private readonly IWalletFraudAnalyticsService walletFraudAnalyticsService;
    private readonly IWalletTransactionService walletTransactionService;
    private readonly AppSettings appSettings;
    public NIPOutwardWalletTransactionService(INIPOutwardWalletTransactionRepository nipOutwardWalletTransactionRepository,
    IWalletFraudAnalyticsService walletFraudAnalyticsService, IWalletTransactionService walletTransactionService,
    IOptions<AppSettings> appSettings)
    {
        this.nipOutwardWalletTransactionRepository = nipOutwardWalletTransactionRepository;
        this.outboundLogs = new List<OutboundLog>();
        this.walletFraudAnalyticsService = walletFraudAnalyticsService;
        this.walletTransactionService = walletTransactionService;
        this.appSettings = appSettings.Value;
        this.retryPolicy = Policy.Handle<Exception>()
        .WaitAndRetryAsync(new[]
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(4)
        }, (exception, timeSpan, retryCount, context) =>
        {
            var outboundLog = new OutboundLog  { OutboundLogId = ObjectId.GenerateNewId().ToString() };
            outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + "\r\n" + @$"Retrying due to {exception.GetType().Name}... Attempt {retryCount}
                Exception Details: {exception.Message} {exception.StackTrace} " ;
            outboundLogs.Add(outboundLog);
        });
    }
    public async Task<FundsTransferResult<string>> ProcessTransaction(NIPOutwardWalletTransaction request)
    {
        FundsTransferResult<string> result = new FundsTransferResult<string>();
        result.IsSuccess = false;
        OutboundLog outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.ProcessTransaction)}";

        try
        {
            request.StatusFlag = 3;
            request.DateAdded = DateTime.UtcNow.AddHours(1);
            await nipOutwardWalletTransactionRepository.Create(request);

            var payload = new WalletFraudAnalyticsRequestDto
            {
                AppId = request.AppId,
                ReferenceId = request.SessionID,
                FromAccount = request.DebitAccountNumber,
                ToAccount = request.CreditAccountNumber,
                SenderName = request.OriginatorName,
                BeneficiaryName = request.CreditAccountName,
                Amount = (float)(request.Amount),
                BeneficiaryBankCode = request.BeneficiaryBankCode,
                IsWalletOnly = appSettings.WalletFraudAnalyticsProperties.IsWalletOnly == false ? 0 : 1,
                TransactionType = appSettings.WalletFraudAnalyticsProperties.TransactionType,
                TransTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            var fraudScoreResult = await walletFraudAnalyticsService.GetFraudScore(payload);

            outboundLogs.Add(walletFraudAnalyticsService.GetOutboundLog());

            if(fraudScoreResult == null)
            {
                result.IsSuccess = false;
                result.ResponseTime = DateTime.UtcNow.AddHours(1);
                result.Message = "Transaction failed";
                result.ErrorMessage = "Error occurred while attempting to call Fraud API for Wallet";
                return result;
            }
            //logger.Info($"FraudApi Response- {JsonConvert.SerializeObject(fraudApiScore)} transaction ID - {item.ID}");

            if(fraudScoreResult.ResponseCode != "00")
            {
                request.FraudResponseCode = fraudScoreResult.ResponseCode;
                request.FraudResponseMessage = "An Error Occurred, check the logs to get the error";
                await Update(request);

                result.IsSuccess = false;
                result.ResponseTime = DateTime.UtcNow.AddHours(1);
                result.Message = "Transaction failed";
                result.ErrorMessage = "Call to Fraud API for Wallet failed";
                return result;
            }

            if(fraudScoreResult.ResponseCode == "00")
            {
                if(fraudScoreResult.FraudScore == "-1")
                {
                    request.FraudResponseCode = fraudScoreResult.ResponseCode;
                    request.FraudResponseMessage = "An Error Occurred, check the logs to get the error";
                    await Update(request);   

                    result.IsSuccess = false;
                    result.ResponseTime = DateTime.UtcNow.AddHours(1);
                    result.Message = "Transaction failed";
                    result.ErrorMessage = $"Call to Fraud API for Wallet returned {fraudScoreResult.FraudScore} fraud score";
                    return result;
                }

                if(fraudScoreResult.FraudScore == "1")
                {
                    request.FraudResponseCode = fraudScoreResult.ResponseCode;
                    request.FraudResponseMessage = fraudScoreResult.ErrorMessage ?? "Suspicious Transaction";
                    request.StatusFlag = 5;
                    await Update(request);   

                    result.IsSuccess = false;
                    result.ResponseTime = DateTime.UtcNow.AddHours(1);
                    result.Message = "Transaction failed";
                    result.ErrorMessage = "Suspicious Transaction";
                    return result;
                }

                if(fraudScoreResult.FraudScore == "0")
                {
                    request.FraudResponseCode = fraudScoreResult.ResponseCode;
                    request.FraudResponseMessage = fraudScoreResult.ErrorMessage ?? "Transaction should be processed";
                    await Update(request);

                    //call wallet To wallet first, if okay, then insert into Nip table
                    
                    var walletToWalletpayload = new WalletToWalletRequestDto
                    {
                        amt = request.Amount.ToString(),
                        channelID = request.AppId,
                        CURRENCYCODE = request.CurrencyCode,
                        frmacct = request.DebitAccountNumber,
                        paymentRef = request.SessionID,
                        remarks = request.PaymentReference,
                        toacct = appSettings.WalletTransactionServiceProperties.WalletPoolAccount, //this should be WalletPoolAccount
                        TransferType = 2 //doing this so on wallet's end the charge will be debited
                    };
                    
                    var walletToWalletResponse = await walletTransactionService.WalletToWalletTransfer(walletToWalletpayload);

                    outboundLogs.Add(walletTransactionService.GetOutboundLog());
                    if (walletToWalletResponse == null)
                    {
                        request.ResponseCode = string.Empty;
                        request.ResponseMessage = $"An exception occurred, pls check the logs - DateTime- {DateTime.UtcNow.AddHours(1)}";
                        request.StatusFlag = 7;
                        await Update(request);

                        result.IsSuccess = false;
                        result.ResponseTime = DateTime.UtcNow.AddHours(1);
                        result.Message = "Transaction failed";
                        result.ErrorMessage = "Call to Wallet Transfer API failed";
                        return result;
                    }

                    if(walletToWalletResponse?.response != "00")
                    {
                        request.ResponseCode = walletToWalletResponse.response;
                        request.ResponseMessage = $"{walletToWalletResponse.message}";
                        request.StatusFlag = 7;
                        await Update(request);

                        result.IsSuccess = false;
                        result.ResponseTime = DateTime.UtcNow.AddHours(1);
                        result.Message = "Transaction failed";
                        result.ErrorMessage = "Call to Wallet Transfer API did not return success response";
                        return result;
                    }
                    if(walletToWalletResponse?.response == "00")
                    {
                        request.ResponseCode = walletToWalletResponse.response;
                        request.ResponseMessage = $"{walletToWalletResponse.message}";
                        request.StatusFlag = 100;
                        await Update(request);

                        result.IsSuccess = true;
                        result.ResponseTime = DateTime.UtcNow.AddHours(1);
                        result.Message = "Transaction successful";
                        return result;
                    }
                }
                //continue;
            } 

            result.IsSuccess = false;
            result.ResponseTime = DateTime.UtcNow.AddHours(1);
            result.Message = "Transaction failed";
            result.ErrorMessage = "Something went wrong during Wallet processing";
            return result;              

        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            result.Message = "Transaction failed";
            outboundLog.ExceptionDetails = $@"Error thrown 
            Exception Details: {ex.Message} {ex.StackTrace}";
            outboundLogs.Add(outboundLog);
            return result;
        }
    }

    public async Task<int>  Update(NIPOutwardWalletTransaction request)
    {
        var recordsUpdated = 0;
        await retryPolicy.ExecuteAsync(async () =>
        {
            recordsUpdated = await nipOutwardWalletTransactionRepository.Update(request);
        });

        return recordsUpdated;
    }

    public List<OutboundLog> GetOutboundLogs()
    {
        var recordsToBeMoved = this.outboundLogs;
        this.outboundLogs = new List<OutboundLog>();
        return recordsToBeMoved;
    }
}