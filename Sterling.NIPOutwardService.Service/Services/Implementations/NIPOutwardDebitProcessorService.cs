namespace Sterling.NIPOutwardService.Service.Services.Implementations;

public class NIPOutwardDebitProcessorService : INIPOutwardDebitProcessorService
{
    private readonly INIPOutwardTransactionService nipOutwardTransactionService;
    private readonly IInboundLogService inboundLogService;
    private readonly AppSettings appSettings;
    private readonly IMapper mapper;
    private readonly INIPOutwardDebitLookupService nipOutwardDebitLookupService;
    private readonly ITransactionDetailsRepository transactionDetailsRepository;
    private List<OutboundLog> outboundLogs;
    private readonly IUtilityHelper utilityHelper;
    private readonly AsyncRetryPolicy retryPolicy;
    private readonly IFraudAnalyticsService fraudAnalyticsService;
    private readonly ITransactionAmountLimitService transactionAmountLimitService;
    private readonly IDebitAccountRepository debitAccountRepository;
    private readonly IIncomeAccountRepository incomeAccountRepository;
    private readonly IVtellerService vtellerService;
    private readonly INIPOutwardSendToNIBSSProducerService nipOutwardSendToNIBSSProducerService;

    public NIPOutwardDebitProcessorService(INIPOutwardTransactionService nipOutwardTransactionService, 
    IInboundLogService inboundLogService, IOptions<AppSettings> appSettings, IMapper mapper,
    INIPOutwardDebitLookupService nipOutwardDebitLookupService, ITransactionDetailsRepository transactionDetailsRepository,
    IUtilityHelper utilityHelper, IFraudAnalyticsService fraudAnalyticsService, 
    ITransactionAmountLimitService transactionAmountLimitService, IDebitAccountRepository debitAccountRepository,
    IIncomeAccountRepository incomeAccountRepository, IVtellerService vtellerService,
    INIPOutwardSendToNIBSSProducerService nipOutwardSendToNIBSSProducerService)
    {
        this.nipOutwardTransactionService = nipOutwardTransactionService;
        this.inboundLogService = inboundLogService;
        this.appSettings = appSettings.Value;
        this.nipOutwardDebitLookupService = nipOutwardDebitLookupService;
        this.mapper = mapper;
        this.transactionDetailsRepository = transactionDetailsRepository;
        this.outboundLogs = new List<OutboundLog> ();
        this.utilityHelper = utilityHelper;
        this.fraudAnalyticsService = fraudAnalyticsService;
        this.transactionAmountLimitService = transactionAmountLimitService;
        this.debitAccountRepository = debitAccountRepository;
        this.incomeAccountRepository = incomeAccountRepository;
        this.vtellerService = vtellerService;
        this.nipOutwardSendToNIBSSProducerService = nipOutwardSendToNIBSSProducerService;
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

    public List<OutboundLog> GetOutboundLogs()
    {
        var recordsToBeMoved = this.outboundLogs;
        this.outboundLogs = new List<OutboundLog>();
        return recordsToBeMoved;
    }

    public async Task<FundsTransferResult<string>> ProcessTransaction(NIPOutwardTransaction nipOutwardTransaction)
    {
        var response = new FundsTransferResult<string>();
        
        response = await DebitAccount(nipOutwardTransaction);
        response.Content = string.Empty;

        if(!response.IsSuccess)
        {
            response.ErrorMessage = response.Message;
        }
        
        return response;
        
    }

    public async Task<FundsTransferResult<string>> DebitAccount(NIPOutwardTransaction nipOutwardTransaction)
    {
        FundsTransferResult<string> result = new FundsTransferResult<string>();
        result.IsSuccess = false;
        OutboundLog outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.DebitAccount)}";
        try
        {
            var checkLookupResult = await nipOutwardDebitLookupService.FindOrCreate(nipOutwardTransaction.ID);
            outboundLogs.Add(nipOutwardDebitLookupService.GetOutboundLog());

            if(!checkLookupResult.IsSuccess)
            {
                return mapper.Map<FundsTransferResult<string>>(checkLookupResult);
            }

            var generateSessionIdResult = await GenerateSessionId(nipOutwardTransaction, checkLookupResult.Content);
            
            if (!generateSessionIdResult.IsSuccess)
            {
                return mapper.Map<FundsTransferResult<string>>(generateSessionIdResult);
            }

            nipOutwardTransaction = generateSessionIdResult.Content;

            var vTellerTransactionDto = mapper.Map<CreateVTellerTransactionDto>(nipOutwardTransaction);
            vTellerTransactionDto.transactionCode = utilityHelper.GenerateTransactionReference(vTellerTransactionDto.BranchCode);
            var getDebitAccountDetailsResult = await GetDebitAccountDetails(vTellerTransactionDto);

            if (!getDebitAccountDetailsResult.IsSuccess)
            {
                return mapper.Map<FundsTransferResult<string>>(getDebitAccountDetailsResult);
            }

            nipOutwardTransaction.OriginatorEmail = getDebitAccountDetailsResult.Content.DebitAccountDetails.Email;
            vTellerTransactionDto = getDebitAccountDetailsResult.Content.CreateVTellerTransactionDto;

            var doFraudCheckResult = await DoFraudCheck(nipOutwardTransaction, checkLookupResult.Content);

             if (!doFraudCheckResult.IsSuccess)
            {
                return mapper.Map<FundsTransferResult<string>>(doFraudCheckResult);
            }

            nipOutwardTransaction = doFraudCheckResult.Content;

            var appIDCheckResult = await AppIDCheck(nipOutwardTransaction);

            if (!appIDCheckResult.IsSuccess)
            {
                return mapper.Map<FundsTransferResult<string>>(appIDCheckResult);
            }

            var customerStatusCode = getDebitAccountDetailsResult.Content.DebitAccountDetails.CustomerStatusCode;
            var CheckIfDateIsHolidayResult = await CheckIfDateIsHoliday(nipOutwardTransaction, customerStatusCode);

            if (!CheckIfDateIsHolidayResult.IsSuccess)
            {
                return mapper.Map<FundsTransferResult<string>>(CheckIfDateIsHolidayResult);
            }

            var HasConcessionPerTransPerday = false;
            var concessionTransactionAmountLimit = await transactionAmountLimitService.GetConcessionLimitByDebitAccount(nipOutwardTransaction.DebitAccountNumber);
            
            outboundLogs.Add(transactionAmountLimitService.GetOutboundLog());
            if (concessionTransactionAmountLimit != null)
            {
                HasConcessionPerTransPerday = true;
            }

            var customerClass = getDebitAccountDetailsResult.Content.CustomerClass;

            if (customerClass == 1 && nipOutwardTransaction.Amount > 1000000 && !HasConcessionPerTransPerday)
            {
                //log.Info("Customer with account " + transaction.nuban + " does not have concession and the txnamt " + transaction.amt + " is higher than the max per tran");
                nipOutwardTransaction.StatusFlag = 17;
                await nipOutwardTransactionService.Update(nipOutwardTransaction);
                result.IsSuccess = false;
                result.Message = "Transaction not allowed";
                return result;
            }

            var computeVatAndFeeResult = await ComputeVatAndFee(vTellerTransactionDto, nipOutwardTransaction);

            if(!computeVatAndFeeResult.IsSuccess)
            {
                return mapper.Map<FundsTransferResult<string>>(computeVatAndFeeResult);
            }

            var usableBalance = getDebitAccountDetailsResult.Content.DebitAccountDetails.UsableBalance;
            var totalTransactionAmount = nipOutwardTransaction.Amount + vTellerTransactionDto.feecharge + vTellerTransactionDto.vat;

            if(totalTransactionAmount >  usableBalance)
            {
                result.IsSuccess = false;
                result.Message = "Insufficient balance";
                return result;
            }

            var isLedgerNotAllowedResult = await transactionDetailsRepository.isLedgerNotAllowed(nipOutwardTransaction.LedgerCode);
            
            outboundLogs.Add(transactionDetailsRepository.GetOutboundLog());
            if(isLedgerNotAllowedResult) 
            {
                result.IsSuccess = false;
                result.Message = "Transaction not allowed";
                return result;
            }

            var debitAccountResult = await DebitAccount(vTellerTransactionDto, nipOutwardTransaction, 
            concessionTransactionAmountLimit, CheckIfDateIsHolidayResult.Content, customerStatusCode, customerClass);
            
            if(debitAccountResult.IsSuccess)
            {
                await nipOutwardSendToNIBSSProducerService.PublishTransaction(debitAccountResult.Content);
            }

            outboundLogs.Add(nipOutwardSendToNIBSSProducerService.GetOutboundLog());
           // await inboundLogService.CreateInboundLog(inboundLog);
            return mapper.Map<FundsTransferResult<string>>(debitAccountResult);
            

        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            result.Message = "Internal Server Error";
            outboundLog.ExceptionDetails = $@"Error thrown, raw request: {nipOutwardTransaction} 
            Exception Details: {ex.Message} {ex.StackTrace}";
            outboundLogs.Add(outboundLog);
            return result;
        }
        
    //generate session id function
    //account full info function
    //fraud function
    //check app ids
    //check isDateHoliday function
    //check if customer has concession function
    //compute vat and fee function
    //check if ledger not allowed
    //do checks based on concession
    //ibs update
    //set values in obj
    //do debit fn
    //update based on vteller response
    //cbn part
    //cbn concession checks
    // ibs  update?
    // set values in obj?
    // amout val with ledger checks
    //do debit fn and update based on vteller response
    }

    public async Task<FundsTransferResult<NIPOutwardTransaction>> DebitAccount(CreateVTellerTransactionDto vTellerTransactionDto,
    NIPOutwardTransaction nipOutwardTransaction, ConcessionTransactionAmountLimit? concessionTransactionAmountLimit,
    bool holidayFound, int customerStatusCode, int customerClass )
    {
        FundsTransferResult<NIPOutwardTransaction> result = new FundsTransferResult<NIPOutwardTransaction>();
        OutboundLog outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.DebitAccount)}";
        result.IsSuccess = false;
        try
        {
            if(concessionTransactionAmountLimit != null)
            {
                
                var concessionChecksResult = await DoChecksBasedOnConcession(nipOutwardTransaction,  
                concessionTransactionAmountLimit);

                outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);

                if(!concessionChecksResult.IsSuccess)
                {
                    return concessionChecksResult;
                }

                //nipOutwardTransaction = concessionChecksResult.Content;

                return await CallVTellerToDebitAccount(vTellerTransactionDto, nipOutwardTransaction, vTellerTransactionDto.BranchCode);

            }
            else
            {
                var cbnOrEftChecksResult = await DoChecksBasedOnCBNorEFTLimit(nipOutwardTransaction, holidayFound, 
                 customerStatusCode, customerClass);

                if(!cbnOrEftChecksResult.IsSuccess)
                {
                    return cbnOrEftChecksResult;
                }

                return await CallVTellerToDebitAccount(vTellerTransactionDto, nipOutwardTransaction, "232");

            }
        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            result.Message = "Internal Server Error";
            var request = "Transaction object:" + JsonConvert.SerializeObject(vTellerTransactionDto) + 
            $@"  holidayFound: {holidayFound}, customerStatusCode: {customerStatusCode}, cus_class: {customerClass}";
            outboundLog.ExceptionDetails = $@"Error thrown, raw request: {request} 
            Exception Details: {ex.Message} {ex.StackTrace}";
            outboundLogs.Add(outboundLog);
            return result;
            
        }
    }

    public async Task<FundsTransferResult<NIPOutwardTransaction>> GenerateSessionId(NIPOutwardTransaction transaction, NIPOutwardDebitLookup nipOutwardDebitLookup)
    {
        FundsTransferResult<NIPOutwardTransaction> result = new FundsTransferResult<NIPOutwardTransaction>();
        OutboundLog outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.GenerateSessionId)}";
        result.IsSuccess = false;

        try
       { 
            if (transaction.DateAdded?.Date != DateTime.UtcNow.AddHours(1).Date)
            {
                transaction.NameEnquirySessionID = await transactionDetailsRepository.GenerateNameEnquirySessionId(transaction.NameEnquirySessionID);
                NameEnquiry.NewIBSSoapClient newibs = 
                new NameEnquiry.NewIBSSoapClient(NameEnquiry.NewIBSSoapClient.EndpointConfiguration.NewIBSSoap, appSettings.NameEnquirySoapService);
                //log.Info("calling  Name Enquiry for RefId " + transaction.Refid);
                NameEnquiryResponse nameEnquiryResponse = new NameEnquiryResponse();
                outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
                outboundLog.APICalled = "NameEnquiry";
                outboundLog.APIMethod = "NameEnquiry";
                outboundLog.RequestDetails = $@"SessionIdNE:{transaction.NameEnquirySessionID} 
                BankCode:{transaction.BeneficiaryBankCode} ChannelCode:{transaction.ChannelCode} AccountNumber:{transaction.CreditAccountNumber}";

                await retryPolicy.ExecuteAsync(async () =>
                {
                    nameEnquiryResponse = await newibs.NameEnquiryAsync(transaction.NameEnquirySessionID, transaction.BeneficiaryBankCode, 
                    transaction.ChannelCode.ToString(), transaction.CreditAccountNumber);
                });
                
                outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                outboundLog.ResponseDetails = nameEnquiryResponse?.Body?.NameEnquiryResult;
                outboundLogs.Add(outboundLog);
                //log.Info("The Response that came back for Name Enquiry is " + namersp + " for RefId " + transaction.Refid);
                var namerspArray = nameEnquiryResponse?.Body?.NameEnquiryResult?.Split(':'); 
                if (namerspArray?[0] == "00")
                {
                    transaction.NameEnquirySessionID = transaction.NameEnquirySessionID;
                    transaction.CreditAccountName = namerspArray[1];
                    transaction.BeneficiaryBVN = namerspArray[2];
                    transaction.BeneficiaryKYCLevel = namerspArray[3];
                    transaction.NameEnquiryResponse = namerspArray[0];
                }
                else
                {
                    result.IsSuccess = false;
                    result.Message = "Name enquiry failed";
                    return result;
                }                
                   
            }

            #region GenerateFTSessionId
                transaction.SessionID = utilityHelper.GenerateFundsTransferSessionId(transaction.ID);
                var recordsUpdated = await nipOutwardTransactionService.Update(transaction);

                if (recordsUpdated < 1)
                {
                    outboundLog.ExceptionDetails =  "Unable to update FT SessionId for transaction with RefId " + transaction.ID ;
                    var updateLog = nipOutwardDebitLookupService.GetOutboundLog();
                    outboundLogs.Add(outboundLog);
                    outboundLogs.Add(updateLog);
                    transaction.StatusFlag = 0;
                    await nipOutwardTransactionService.Update(transaction);
                    await nipOutwardDebitLookupService.Delete(nipOutwardDebitLookup);
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
            outboundLogs.Add(outboundLog);
            return result;
        }
         
    }

    public async Task<FundsTransferResult<NIPOutwardTransaction>> DoFraudCheck(NIPOutwardTransaction transaction, 
    NIPOutwardDebitLookup nipOutwardDebitLookup)
    {
        FundsTransferResult<NIPOutwardTransaction> result = new FundsTransferResult<NIPOutwardTransaction>();
        OutboundLog outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() }; 
        result.IsSuccess = false;
        try
        {
            if (transaction.DebitAccountNumber != appSettings.SterlingProSuspenseAccount)
            {
                if (string.IsNullOrWhiteSpace(transaction.FraudScore))
                {
                    #region DoFraudAnalysis
                    decimal FraudMinimumAmount = appSettings.FraudMinimumAmount;

                    if (transaction.Amount >= FraudMinimumAmount)
                    {
                        FraudAnalyticsResponse fraudrsp = await fraudAnalyticsService.DoFraudAnalytics(transaction.AppId.ToString(), 
                        transaction.ID.ToString(), transaction.SessionID, "101", transaction.DebitAccountNumber, 
                        transaction.CreditAccountNumber, transaction.Amount.ToString(), transaction.OriginatorName, 
                        transaction.CreditAccountName, transaction.BeneficiaryBankCode, "00", transaction.PaymentReference, transaction.OriginatorEmail);
                        
                        outboundLogs.Add(fraudAnalyticsService.GetOutboundLog());

                        if(fraudrsp != null)
                        {
                            var recordsUpdated = 0;
                            if (fraudrsp.fraudScore != "0")
                            {
                                transaction.StatusFlag = 11;
                                recordsUpdated = await nipOutwardTransactionService.Update(transaction);
                                if (recordsUpdated > 0)
                                {
                                    await nipOutwardDebitLookupService.Delete(nipOutwardDebitLookup);
                                }
                                result.IsSuccess = false;
                                result.Message = "Fraud Check failed";
                                return result;
                            }
                            transaction.FraudResponse = fraudrsp.responseCode;
                            transaction.FraudScore = fraudrsp.fraudScore;
                            recordsUpdated = await nipOutwardTransactionService.Update(transaction);
                            
                        }

                    }

                    #endregion
                }
            }

            result.IsSuccess = true;
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
            outboundLogs.Add(outboundLog);
            return result;
        }
        
    }

    public async Task<FundsTransferResult<string>> AppIDCheck(NIPOutwardTransaction transaction)
    {
        FundsTransferResult<string> result = new FundsTransferResult<string>();
        OutboundLog outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
        result.IsSuccess = false;
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.AppIDCheck)}";

        try
        {
            if (transaction.AppId == 5 || transaction.AppId == 26 || transaction.AppId == 17)
                {
                    //Kia kia- 6009 (Transfer Limit - N3000.00)
                    if (transaction.Amount > 3000 && transaction.LedgerCode == "6009")
                    {
                        transaction.StatusFlag = 12;
                        result.IsSuccess = false;
                        result.Message = "Transaction not allowed";
                    }
                    //Kia kia plus-6010 (Transfer Limit - N10,000.00)
                    else if (transaction.Amount > 10000 && transaction.LedgerCode == "6010")
                    {
                        transaction.StatusFlag = 13;
                        result.IsSuccess = false;
                        result.Message = "Transaction not allowed";
                    }
                    //Sterling Fanatic Kick off – 6011 (Transfer Limit - N3000.00)
                    else if (transaction.Amount > 3000 && transaction.LedgerCode == "6011")
                    {
                        transaction.StatusFlag = 14;
                        result.IsSuccess = false;
                        result.Message = "Transaction not allowed";
                    }
                    //Sterling Fanatic Kick off – 6012 (Transfer Limit - N10000.00)
                    else if (transaction.Amount > 10000 && transaction.LedgerCode == "6012")
                    {
                        transaction.StatusFlag = 14;
                        result.IsSuccess = false;
                        result.Message = "Transaction not allowed";
                    }
                    else{
                        result.IsSuccess = true;
                        outboundLog.ResponseDetails = $"Is success: {result.IsSuccess.ToString()}";
                        outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                        outboundLogs.Add(outboundLog);
                        return result;
                    }
                    await nipOutwardTransactionService.Update(transaction);
                }
                else{
                    result.IsSuccess = true;
                }
                outboundLog.ResponseDetails = $"Is success: {result.IsSuccess.ToString()}";
                outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                outboundLogs.Add(outboundLog);
                return result;
        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            result.Message = "Internal Server Error";
            var request = JsonConvert.SerializeObject(transaction);
            outboundLog.ExceptionDetails = $@"Error thrown, raw request: {request} 
            Exception Details: {ex.Message} {ex.StackTrace}";
            outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            outboundLogs.Add(outboundLog);
            return result;
        }
    }

    public async Task<FundsTransferResult<bool>> CheckIfDateIsHoliday(NIPOutwardTransaction transaction, int CustomerStatusCode)
    {
        FundsTransferResult<bool> result = new FundsTransferResult<bool>();
        OutboundLog outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() }; 
        result.IsSuccess = false;
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.CheckIfDateIsHoliday)}";

        try
        {
            DateTime dt = DateTime.Today;
            bool found_hol = await transactionDetailsRepository.isDateHoliday(dt);
            result.Content = found_hol;
            outboundLogs.Add(transactionDetailsRepository.GetOutboundLog());

            //check if the account is corporate and bounce the account tbl_public_holiday
            if (CustomerStatusCode == 2)
            {
                //check the holiday table 
                if (found_hol)
                {
                    outboundLog.ExceptionDetails = @$"57:Transaction not permitted for customer with nuban for corporate on weekend " + transaction.DebitAccountNumber + " because account is corporate";
                    outboundLogs.Add(outboundLog);
                    transaction.StatusFlag = 16;
                    await nipOutwardTransactionService.Update(transaction);
                    result.Message = "Transaction not allowed";
                    result.IsSuccess = false;
                    outboundLog.ResponseDetails = $"Is success: {result.IsSuccess.ToString()}";
                    outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                    outboundLogs.Add(outboundLog);
                    return result;
                }
            }
            result.IsSuccess = true;
            outboundLog.ResponseDetails = $"Is success: {result.IsSuccess.ToString()}";
            outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            outboundLogs.Add(outboundLog);
            return result;
        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            result.Message = "Internal Server Error";
            var request = JsonConvert.SerializeObject(transaction);
            outboundLog.ExceptionDetails = $@"Error thrown, raw request: {request} 
            Exception Details: {ex.Message} {ex.StackTrace}";
            outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            outboundLogs.Add(outboundLog);
            return result;
        }
    }

    public async Task<FundsTransferResult<CreateVTellerTransactionDto>> ComputeVatAndFee(CreateVTellerTransactionDto createVTellerTransactionDto, 
    NIPOutwardTransaction model)
    {
        FundsTransferResult<CreateVTellerTransactionDto> result = new FundsTransferResult<CreateVTellerTransactionDto>();
        OutboundLog outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() }; 
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.ComputeVatAndFee)}";
        
        result.IsSuccess = false;
        try
        {
            bool foundval = await transactionDetailsRepository.isBankCodeFound(createVTellerTransactionDto.BranchCode);
            outboundLogs.Add(transactionDetailsRepository.GetOutboundLog());

            var nipCharges = await transactionDetailsRepository.GetNIPFee(createVTellerTransactionDto.Amount);
            outboundLogs.Add(transactionDetailsRepository.GetOutboundLog());

            if (nipCharges.ChargesFound)
            {
                createVTellerTransactionDto.feecharge = nipCharges.NIPFeeAmount;
                createVTellerTransactionDto.vat = nipCharges.NIPVatAmount;
                createVTellerTransactionDto.VAT_bra_code = "NG0020001";
                createVTellerTransactionDto.VAT_cur_code = "NGN";
                createVTellerTransactionDto.VAT_led_code = "17201";
                var Last4 = int.Parse(createVTellerTransactionDto.VAT_bra_code.Substring(6, 3)) + 2000;
                var TSSAcct = createVTellerTransactionDto.VAT_cur_code + createVTellerTransactionDto.VAT_led_code + "0001" + Last4.ToString();
                createVTellerTransactionDto.VAT_sub_acct_code = TSSAcct;

                if (createVTellerTransactionDto.VAT_sub_acct_code == "")
                {
                    
                    model.StatusFlag = 18;
                    await nipOutwardTransactionService.Update(model);
                    result.Message = "Transaction failed";
                    result.IsSuccess = false;
                    outboundLog.ResponseDetails = "Unable to form the VAT account for ledcode 17201";
                    outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                    outboundLogs.Add(outboundLog);
                    return result;
                }
                
                outboundLog.ResponseDetails = "Vat account formed for Branch " + createVTellerTransactionDto.VAT_bra_code + 
                " is " + createVTellerTransactionDto.VAT_sub_acct_code;
                outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                outboundLogs.Add(outboundLog);
            }
            else
            {
                //msg = "Error: Unable to compute VAT and Fee for account " + acct;
                //log.Info(msg);
                model.StatusFlag = 18;
                await nipOutwardTransactionService.Update(model);
                result.Message = "Transaction failed";
                result.IsSuccess = false;

                outboundLog.ResponseDetails = "Error: Unable to compute VAT and Fee for account";
                outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                outboundLogs.Add(outboundLog);

                return result;
            }

            if (foundval)
            {
                createVTellerTransactionDto.VAT_cus_num = "";
                
            }
            else
            {
                createVTellerTransactionDto.VAT_cus_num = "0";
            }
            
            result.IsSuccess = true;
            outboundLog.ResponseDetails = $"Is success: {result.IsSuccess.ToString()}";
            outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            outboundLogs.Add(outboundLog);
            return result;
        }
        catch (Exception ex)
        {
            //new ErrorLog("Error occured " + ex);
            //log.Error("Error occured ", ex);
            //msg = "Error: Unable to compute VAT and Fee for account " + acct;
            //log.Info(msg);
            model.StatusFlag = 19;
            await nipOutwardTransactionService.Update(model);
            
            result.IsSuccess = false;
            result.Message = "Internal Server Error";
            var request = JsonConvert.SerializeObject(createVTellerTransactionDto);
            outboundLog.ExceptionDetails = $@"Error thrown, raw request: {request} 
            Exception Details: {ex.Message} {ex.StackTrace}";
            outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            outboundLogs.Add(outboundLog);
            return result;
            
        }
    }

     public async Task<FundsTransferResult<NIPOutwardTransaction>> DoChecksBasedOnConcession(NIPOutwardTransaction transaction, 
     ConcessionTransactionAmountLimit concessionTransactionAmountLimit)
    {
        FundsTransferResult<NIPOutwardTransaction> result = new FundsTransferResult<NIPOutwardTransaction>();
        OutboundLog outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
        result.IsSuccess = false;
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.DoChecksBasedOnConcession)}";
        try
        {        
            //log.Info("MaxperTrans returned " + maxPerTrans.ToString() + " maxPerday " + maxPerday.ToString() + " for customer " + transaction.nuban);
            TotalTransactionDonePerDay totalTransactionsPerDay = await transactionDetailsRepository.GetTotalTransDonePerday(concessionTransactionAmountLimit.MaximumAmountPerDay,
                transaction.Amount, transaction.DebitAccountNumber); 

            outboundLogs.Add(transactionDetailsRepository.GetOutboundLog());

            bool totalfound = totalTransactionsPerDay.TransactionOk;// Tdp1.GetTotalTransDonePerday(transaction.bra_code, transaction.cus_num, transaction.cur_code, transaction.led_code, transaction.sub_acct_code, maxPerday, transaction.amt, transaction.nuban);

            if (transaction.Amount > concessionTransactionAmountLimit.MaximumAmountPerTransaction)
            {
                outboundLog.ResponseDetails = "Customer with account " + transaction.DebitAccountNumber + " has concession and the transaction amount " + transaction.Amount + " is higher than the max per tran" + concessionTransactionAmountLimit.MaximumAmountPerTransaction ;
                outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                outboundLogs.Add(outboundLog);
                transaction.FundsTransferResponse = "09x";    
                transaction.StatusFlag = 23;    
                await nipOutwardTransactionService.Update(transaction);
                result.Content = transaction;
                result.Message = "Transaction not allowed";
                result.IsSuccess = false;
                return result;
            }

            if (transaction.Amount + totalTransactionsPerDay.TotalDone > concessionTransactionAmountLimit.MaximumAmountPerDay)
            {
                decimal my1sum = 0;
                my1sum = transaction.Amount + totalTransactionsPerDay.TotalDone;
                outboundLog.ResponseDetails = "Customer with account " + transaction.DebitAccountNumber + " has concession and the transaction amount plus the totalNipdone " + my1sum.ToString() + "is higher than the max per tran" + concessionTransactionAmountLimit.MaximumAmountPerTransaction.ToString() ;
                outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                outboundLogs.Add(outboundLog);

                transaction.FundsTransferResponse = "09x";
                transaction.StatusFlag = 24;
                await nipOutwardTransactionService.Update(transaction);
                result.Content = transaction;
                result.Message = "Transaction not allowed";
                result.IsSuccess = false;
                return result;
            }

            if (transaction.LedgerCode == "6009")
            {
                if (transaction.Amount > 20000)
                {
                    outboundLog.ResponseDetails = "Transaction with account " + transaction.DebitAccountNumber + 
                    $" and ledger code 6009 has transaction amount {transaction.Amount} greater than 20000 ";
                    outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                    outboundLogs.Add(outboundLog);

                    transaction.FundsTransferResponse = "13";
                    transaction.StatusFlag = 25;
                    await nipOutwardTransactionService.Update(transaction);
                    result.Content = transaction;
                    result.Message = "Transaction not allowed";
                    result.IsSuccess = false;
                    return result;
                }
            }

            //transaction.FundsTransferResponse = "00";
           
            result.Content = transaction;
            result.IsSuccess = true;
            outboundLog.ResponseDetails = $"Is Success {result.IsSuccess}";
            outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            outboundLogs.Add(outboundLog);
            return result;
        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            result.Content = transaction;
            result.Message = "Internal Server Error";
            var request = "Transaction object:" + JsonConvert.SerializeObject(transaction) + 
            " Concession Transaction Amount Limit object:" + JsonConvert.SerializeObject(concessionTransactionAmountLimit);
            outboundLog.ExceptionDetails = $@"Error thrown, raw request: {request} 
            Exception Details: {ex.Message} {ex.StackTrace}";
            outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            outboundLogs.Add(outboundLog);
            return result;
        }
    }

    public async Task<FundsTransferResult<NIPOutwardTransaction>> DoChecksBasedOnCBNorEFTLimit(NIPOutwardTransaction transaction, bool holidayFound, 
    int customerStatusCode, int cus_class)
    {
        FundsTransferResult<NIPOutwardTransaction> result = new FundsTransferResult<NIPOutwardTransaction>();
        OutboundLog outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() }; 
        result.IsSuccess = false;

        try
        {
            decimal maxPerTrans = 0;
            decimal maxPerday = 0;
            var totalTransactionsPerDay = await transactionDetailsRepository.GetTotalTransDonePerday(maxPerday, transaction.Amount, transaction.DebitAccountNumber);
            outboundLogs.Add(transactionDetailsRepository.GetOutboundLog());

            if (holidayFound)
            {
                if (customerStatusCode == 1 || customerStatusCode == 6)
                {
                    if (totalTransactionsPerDay.TotalCount >= 3)
                    {
                        transaction.FundsTransferResponse = "65";
                        transaction.StatusFlag = 28;
                        await nipOutwardTransactionService.Update(transaction);
                        result.Content = transaction;
                        result.Message = "Transaction not allowed";
                        result.IsSuccess = false;
                        return result;
                    }

                    if (totalTransactionsPerDay.TotalDone + transaction.Amount > 200000)
                    {                        
                        transaction.FundsTransferResponse = "65";
                        transaction.StatusFlag = 24;
                        await nipOutwardTransactionService.Update(transaction);
                        result.Content = transaction;
                        result.Message = "Transaction not allowed";
                        result.IsSuccess = false;
                        return result;
                    }
                }
            }

            CBNTransactionAmountLimit dsl = await transactionAmountLimitService.GetCBNLimitByCustomerClass(cus_class);
            outboundLogs.Add(transactionAmountLimitService.GetOutboundLog());

            if (cus_class == 1)
            {
                if (dsl != null)
                {
                    maxPerTrans = dsl.MaximumAmountPerTransaction;
                    maxPerday = dsl.MaximumAmountPerDay;
                }

                //check if the ledger is for savings
                
                bool isFound = await transactionDetailsRepository.isLedgerFound(transaction.LedgerCode);
                outboundLogs.Add(transactionAmountLimitService.GetOutboundLog());

                if (isFound)
                {
                    var eftAmountLimit = await transactionAmountLimitService.GetEFTLimit();
                    outboundLogs.Add(transactionAmountLimitService.GetOutboundLog());

                    maxPerTrans = eftAmountLimit.MaximumAmountPerTransaction;
                    maxPerday = eftAmountLimit.MaximumAmountPerDay;
                }
            }
            else if (cus_class == 2 || cus_class == 3)
            {
                if (dsl != null)
                {
                    maxPerTrans = dsl.MaximumAmountPerTransaction;
                    maxPerday = dsl.MaximumAmountPerDay;
                }
            
            }    

            if (maxPerTrans == 0 || maxPerday == 0)
            {
                outboundLog.ResponseDetails = "Unable to get the maximum amount per day/transaction for account";
                outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                outboundLogs.Add(outboundLog);
                
                transaction.FundsTransferResponse = "57";
                transaction.StatusFlag = 29;
                await nipOutwardTransactionService.Update(transaction);
                result.Content = transaction;
                result.Message = "Transaction not allowed";
                result.IsSuccess = false;
                return result;
            }

            //this is to ensure that customers will not exceed the daily cbn limit
            if (transaction.Amount <= maxPerTrans)
            {
                if (transaction.Amount + totalTransactionsPerDay.TotalDone > maxPerday)
                {
                    outboundLog.ResponseDetails = "transaction amount plus total done for the day greater than CBN maximum amount per day";
                    outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                    outboundLogs.Add(outboundLog);

                    transaction.FundsTransferResponse = "09x";
                    transaction.StatusFlag = 24;
                    await nipOutwardTransactionService.Update(transaction);
                    result.Content = transaction;
                    result.Message = "Transaction not allowed";
                    result.IsSuccess = false;
                    return result;
                }
            }
            else
            {
                outboundLog.ResponseDetails = "transaction amount greater than CBN maximum amount per transaction";
                outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                outboundLogs.Add(outboundLog);

                transaction.FundsTransferResponse = "61";
                transaction.StatusFlag = 23;
                await nipOutwardTransactionService.Update(transaction);
                result.Content = transaction;
                result.Message = "Transaction not allowed";
                result.IsSuccess = false;
                return result;
            }

            result.IsSuccess = true;
            outboundLog.ResponseDetails = $"is success: {result.IsSuccess}";
            outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            outboundLogs.Add(outboundLog);

            return result;
        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            result.Message = "Internal Server Error";
            var request = "Transaction object:" + JsonConvert.SerializeObject(transaction) + 
            $@"  holidayFound: {holidayFound}, customerStatusCode: {customerStatusCode}, cus_class: {cus_class}";
            outboundLog.ExceptionDetails = $@"Error thrown, raw request: {request} 
            Exception Details: {ex.Message} {ex.StackTrace}";
            outboundLogs.Add(outboundLog);
            return result;
            
        }
       
            
    }

    public async Task<FundsTransferResult<NIPOutwardTransaction>> CallVTellerToDebitAccount(CreateVTellerTransactionDto createVTellerTransactionDto, 
        NIPOutwardTransaction model, string branchCode)
    {
        FundsTransferResult<NIPOutwardTransaction> result = new FundsTransferResult<NIPOutwardTransaction>();
        OutboundLog outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() }; 
        result.IsSuccess = false;
        try
        {
            createVTellerTransactionDto.tellerID = "9990";
        
            createVTellerTransactionDto.transactionCode = utilityHelper.GenerateTransactionReference(branchCode);

            createVTellerTransactionDto.origin_branch = createVTellerTransactionDto.BranchCode;
            createVTellerTransactionDto.inCust.cus_sho_name = createVTellerTransactionDto.OriginatorName;
            createVTellerTransactionDto.outCust.cusname = utilityHelper.RemoveSpecialCharacters(model.CreditAccountName) + "/" + utilityHelper.RemoveSpecialCharacters(model.PaymentReference);

            createVTellerTransactionDto.inCust.bra_code = createVTellerTransactionDto.BranchCode;
            createVTellerTransactionDto.inCust.cus_num = createVTellerTransactionDto.CustomerID;
            createVTellerTransactionDto.inCust.cur_code = createVTellerTransactionDto.CurrencyCode;
            createVTellerTransactionDto.inCust.led_code = createVTellerTransactionDto.LedgerCode;
            createVTellerTransactionDto.inCust.sub_acct_code = createVTellerTransactionDto.DebitAccountNumber;

        
            var incomeAccountsDetailsResult = await GetIncomeAccounts();
           

            if(!incomeAccountsDetailsResult.IsSuccess)
            {
                result.IsSuccess = false;
                result.Message = "Transaction failed";
                result.ErrorMessage = "Unable to fetch fee and TSS accounts";
                return result;
            }

            #region DoDebitCredit
            
            DateTime vtellerRequestTime = DateTime.UtcNow.AddHours(1);
            var vTellerResponse = await vtellerService.authorizeIBSTrnxFromSterling(createVTellerTransactionDto, incomeAccountsDetailsResult.Content);
            DateTime vtellerResponseTime = DateTime.UtcNow.AddHours(1);

            outboundLogs.AddRange(vtellerService.GetOutboundLogs());
            //log.Info("Finished debiting for transaction with RefId " + transaction.Refid + " and account number " + transaction.nuban +" and response from Vteller is " + acs.Respreturnedcode1);
            //g.updateVtellerRequestResponseTime(transaction.Refid, vtellerRequestTime, vtellerResponseTime);
            
            model.VtellerRequestTime = vtellerRequestTime;
            model.VtellerResponseTime = vtellerResponseTime;

            return await UpdateVTellerResponse(model, vTellerResponse);

            //result.IsSuccess = vTellerResponse.Respreturnedcode1 == "0" ? true : false;

            
            #endregion
        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            result.Message = "Internal Server Error";
            var request = "Transaction object:" + JsonConvert.SerializeObject(createVTellerTransactionDto) + 
            $@"  branch code: {branchCode}";
            outboundLog.ExceptionDetails = $@"Error thrown, raw request: {request} 
            Exception Details: {ex.Message} {ex.StackTrace}";
            outboundLogs.Add(outboundLog);
            return result;
            
        }
        
    }

    public async Task<FundsTransferResult<NIPOutwardTransaction>> UpdateVTellerResponse(NIPOutwardTransaction transaction, 
        VTellerResponseDto response)
    {
        FundsTransferResult<NIPOutwardTransaction> result = new FundsTransferResult<NIPOutwardTransaction>();
        OutboundLog outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() }; 
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.UpdateVTellerResponse)}";
        result.IsSuccess = false;
        try
        {
            if (response.Respreturnedcode1 == "1x")
            {
            
                //log.Info("The Response from Banks is " + response.Respreturnedcode1 + "  and hence, vTeller logs it as 3 For SessionID " + transaction.SessionID + " T24 msg ==> " + response.error_text);
                transaction.DebitResponse = 3;
                transaction.LastUpdate = DateTime.UtcNow.AddHours(1);
                transaction.StatusFlag = 26;
                transaction.FundsTransferResponse = "1x";
                await nipOutwardTransactionService.Update(transaction);

                result.IsSuccess = false;
                result.Message = "Transaction Failed";
                
                outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                outboundLog.ResponseDetails = $"response:{response.Respreturnedcode1} error text: {response.error_text}";
                outboundLogs.Add(outboundLog);

                return result;
            }

            if (response.Respreturnedcode1 != "0")
            {
                //log.Info("The Response from Banks is " + response.Respreturnedcode1 + "  and hence, vTeller logs it as 2. For SessionID " + transaction.SessionID + " T24 msg ==> " + response.error_text);
                //msg = "Error: Unable to debit customer's account for Principal";
                string Respval = "";

                if (response.error_text.Contains("Post No Debits"))
                {
                    Respval = "x1000"; //Post No Debits (Override ID - POSTING.RESTRICT)
                }
                else if (response.error_text.Contains("Incomplete Documentation"))
                {
                    Respval = "x1001"; //Incomplete Documentation (Override ID - POSTING.RESTRICT)
                }
                else if (response.error_text.Contains("BVN"))
                {
                    Respval = "x1002"; //BVN (Override ID - POSTING.RESTRICT)
                }
                else if (response.error_text.Contains("Dormant Account Restriction"))
                {
                    Respval = "x1003"; //Dormant Account Restriction (Override ID - POSTING.RESTRICT)
                }
                else if (response.error_text.Contains("Failed Address Verification"))
                {
                    Respval = "x1004"; //Dormant Account Restriction (Override ID - POSTING.RESTRICT)
                }
                else if (response.error_text.Contains("Unauthorised overdraft"))
                {
                    Respval = "x1005"; //Unauthorised overdraft of NGN 10 on account
                }
                else if (response.error_text.Contains("Inactive Account Restriction"))
                {
                    Respval = "x1006"; //Inactive Account Restriction (Override ID - POSTING.RESTRICT)
                }
                else if (response.error_text.Contains("INVALID SWIFT CHAR"))
                {
                    Respval = "x1007"; //INVALID SWIFT CHAR
                }
                else if (response.error_text.Contains(" REJECTED"))
                {
                    Respval = "x1008"; //VALIDATION ERROR - REJECTED
                }
                else if (response.error_text.Contains("is inactive"))
                {
                    Respval = "x1009"; //is inactive
                }
                else if (response.error_text.Contains("Connection refused: connect"))
                {
                    Respval = "x1010"; //Connection refused: connect
                }
                else if (response.error_text.Contains("Account has a short fall of balance"))
                {
                    Respval = "x1011"; //Account has a short fall of balance
                }
                else if (response.error_text.Contains("Below minimum value"))
                {
                    Respval = "x1012"; //Below minimum value
                }
                else if (response.error_text.Contains("TOO MANY DECIMALS"))
                {
                    Respval = "x1013"; //TOO MANY DECIMALS
                }
                else if (response.error_text == "Account " + transaction.DebitAccountNumber + " - Account Upgrade Required (Override ID - POSTING.RESTRICT)")
                {
                    Respval = "x1014"; //Account Upgrade Required (Override ID - POSTING.RESTRICT)
                }
                else if (response.error_text.Contains("Customer Address Verification"))
                {
                    Respval = "x1015"; //Customer Address Verification
                }
                else
                {
                    Respval = "x03";
                    //log.Info("x03 response text " + response.error_text + "THIS WAS ADDED TO CONFIRM");
                }

                transaction.DebitResponse = 2;
                transaction.LastUpdate = DateTime.UtcNow.AddHours(1);
                transaction.StatusFlag = 27;
                transaction.FundsTransferResponse = Respval;
                await nipOutwardTransactionService.Update(transaction);

                result.IsSuccess = false;
                result.Message = "Transaction Failed";

                outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                outboundLog.ResponseDetails = $"response:{response.Respreturnedcode1} error text: {response.error_text}";
                outboundLogs.Add(outboundLog);

                return result;
            }

            //msg = "Customer has been debited!";
            //log.Info("Customer has been debited! for transaction with RefId " + transaction.Refid);                    

            result.IsSuccess = true;
            result.Message = "Customer has been debited successfully";

            transaction.DebitResponse = 1;
            transaction.StatusFlag = 9;
            transaction.PrincipalResponse = response.Prin_Rsp;
            transaction.FeeResponse = response.Fee_Rsp;
            transaction.VatResponse = response.Vat_Rsp;
            transaction.KafkaStatus = "K2";
            await nipOutwardTransactionService.Update(transaction);
            result.Content = transaction;

            outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            outboundLog.ResponseDetails = $"response:{response.Respreturnedcode1} error text: {response.error_text}";
            outboundLogs.Add(outboundLog);

            return result;

        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            result.Message = "Internal Server Error";
            result.Content = transaction;
            var request = "Transaction object:" + JsonConvert.SerializeObject(transaction) + 
            " vTellerResponse: " + JsonConvert.SerializeObject(transaction) +
            $@"  branch code: {response}";
            outboundLog.ExceptionDetails = $@"Error thrown, raw request: {request} 
            Exception Details: {ex.Message} {ex.StackTrace}";
            outboundLogs.Add(outboundLog);
            return result;
        }
         
    }

    public async Task<FundsTransferResult<IncomeAccountsDetails>> GetIncomeAccounts()
    {
        FundsTransferResult<IncomeAccountsDetails> result = new FundsTransferResult<IncomeAccountsDetails>();
        OutboundLog outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
        result.IsSuccess = false;
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.GetIncomeAccounts)}";
        try
        {
            var incomeAccountsDetails = new IncomeAccountsDetails();
            incomeAccountsDetails.Tss = await incomeAccountRepository.GetCurrentTss();
            outboundLogs.Add(incomeAccountRepository.GetOutboundLog());

            incomeAccountsDetails.Tss2 = await incomeAccountRepository.getCurrentTss2();
            outboundLogs.Add(incomeAccountRepository.GetOutboundLog());

            incomeAccountsDetails.ExpCode = await incomeAccountRepository.GetExpcode();
            outboundLogs.Add(incomeAccountRepository.GetOutboundLog());

            incomeAccountsDetails.Fee = await incomeAccountRepository.GetCurrentIncomeAcct();
            outboundLogs.Add(incomeAccountRepository.GetOutboundLog());

            result.IsSuccess = true;
            result.Content = incomeAccountsDetails;
            outboundLog.ResponseDetails = $"is success: {result.IsSuccess}";
        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            result.Message = "Internal server error";
            outboundLog.ExceptionDetails = $@"Error thrown,
            Exception Details: {ex.Message} {ex.StackTrace}";
            outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            outboundLogs.Add(outboundLog);
            
        }
        var incomeAccountRepositoryLog = incomeAccountRepository.GetOutboundLog();
        outboundLogs.Add(incomeAccountRepositoryLog);
        return result;
        
    }

    public async Task<FundsTransferResult<GetDebitAccountDetailsDto>> GetDebitAccountDetails(CreateVTellerTransactionDto vTellerTransactionDto)
    {
        FundsTransferResult<GetDebitAccountDetailsDto> result = new FundsTransferResult<GetDebitAccountDetailsDto>();
        OutboundLog outboundLog = new OutboundLog  { OutboundLogId = ObjectId.GenerateNewId().ToString() }; 
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.GetDebitAccountDetails)}";
        
        result.IsSuccess = false;
        try
        {
            result.Content = new GetDebitAccountDetailsDto();
            var accountDetails = await debitAccountRepository.GetDebitAccountDetails(vTellerTransactionDto.DebitAccountNumber);

            if (accountDetails ==  null)
            {
                outboundLogs.Add( debitAccountRepository.GetOutboundLog());
                result.IsSuccess = false;
                result.Message = "Failed to fetch account details";
                return result;
            }
            
            int customerClass = 0;
            vTellerTransactionDto.LedgerCode = accountDetails.T24_LED_CODE;

            vTellerTransactionDto.BranchCode = accountDetails.T24_BRA_CODE;

            //if (cusclassval == "Individual Customer")
            if (accountDetails.CustomerStatusCode == 1 || accountDetails.CustomerStatusCode == 6)
            {
                customerClass = 1;
            }
            else
            {
                customerClass = 2;
            }

            result.Content.CustomerClass = customerClass;
            result.Content.DebitAccountDetails = accountDetails;
            result.Content.CreateVTellerTransactionDto = vTellerTransactionDto;
            result.IsSuccess = true;
            outboundLog.RequestDetails = "Successfully fetched account details";
            outboundLogs.Add(outboundLog);
            return result;
        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            var rawRequest = "Request object:" + JsonConvert.SerializeObject(vTellerTransactionDto); 
            outboundLog.ExceptionDetails = $@"Error thrown, Raw Request: {rawRequest}
            Exception Details: {ex.Message} {ex.StackTrace}";
            outboundLogs.Add(outboundLog);
            result.Message = "Internal Server Error";
            return result;
        }
    }
}