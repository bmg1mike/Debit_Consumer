namespace Sterling.NIPOutwardService.Service.Services.Implementations;

public class NIPOutwardImalDebitProcessorService : INIPOutwardImalDebitProcessorService
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
    private readonly ITransactionAmountLimitService transactionAmountLimitService;
    private readonly INIPOutwardSendToNIBSSProducerService nipOutwardSendToNIBSSProducerService;
    private readonly INIPOutwardNameEnquiryService nipOutwardNameEnquiryService;
    private readonly IImalInquiryService imalInquiryService;
    private readonly IImalTransactionService imalTransactionService;

    public NIPOutwardImalDebitProcessorService(INIPOutwardTransactionService nipOutwardTransactionService, 
    IInboundLogService inboundLogService, IOptions<AppSettings> appSettings, IMapper mapper,
    INIPOutwardDebitLookupService nipOutwardDebitLookupService, ITransactionDetailsRepository transactionDetailsRepository,
    IUtilityHelper utilityHelper, ITransactionAmountLimitService transactionAmountLimitService,
    INIPOutwardSendToNIBSSProducerService nipOutwardSendToNIBSSProducerService, 
    INIPOutwardNameEnquiryService nipOutwardNameEnquiryService, IImalInquiryService imalInquiryService,
    IImalTransactionService imalTransactionService)
    {
        this.nipOutwardTransactionService = nipOutwardTransactionService;
        this.inboundLogService = inboundLogService;
        this.appSettings = appSettings.Value;
        this.nipOutwardDebitLookupService = nipOutwardDebitLookupService;
        this.mapper = mapper;
        this.transactionDetailsRepository = transactionDetailsRepository;
        this.outboundLogs = new List<OutboundLog> ();
        this.utilityHelper = utilityHelper;
        this.transactionAmountLimitService = transactionAmountLimitService;
        this.nipOutwardSendToNIBSSProducerService = nipOutwardSendToNIBSSProducerService;
        this.nipOutwardNameEnquiryService = nipOutwardNameEnquiryService;
        this.imalInquiryService = imalInquiryService;
        this.imalTransactionService = imalTransactionService;
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

    public async Task<FundsTransferResult<string>> ProcessTransaction(NIPOutwardTransaction nipOutwardTransaction)
    {

        var response = new FundsTransferResult<string>();

        response = await DebitImalAccount(nipOutwardTransaction);
        
        response.Content = string.Empty;

        if(!response.IsSuccess && string.IsNullOrWhiteSpace(response.ErrorMessage))
        {
            response.ErrorMessage = response.Message;
        }
        
        return response;
        
    }

    public List<OutboundLog> GetOutboundLogs()
    {
        var recordsToBeMoved = this.outboundLogs;
        this.outboundLogs = new List<OutboundLog>();
        return recordsToBeMoved;
    }

    public async Task<FundsTransferResult<string>> DebitImalAccount(NIPOutwardTransaction nipOutwardTransaction)
    {
        FundsTransferResult<string> result = new FundsTransferResult<string>();
        result.IsSuccess = false;
        OutboundLog outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.DebitImalAccount)}";
        try
        {
            var checkLookupResult = await nipOutwardDebitLookupService.FindOrCreate(nipOutwardTransaction.ID);
            outboundLogs.Add(nipOutwardDebitLookupService.GetOutboundLog());

            if(!checkLookupResult.IsSuccess)
            {
                return mapper.Map<FundsTransferResult<string>>(checkLookupResult);
            }

            var generateSessionIdResult = await GenerateNameEnquirySessionId(nipOutwardTransaction);
            
            if (!generateSessionIdResult.IsSuccess)
            {
                return mapper.Map<FundsTransferResult<string>>(generateSessionIdResult);
            }

            nipOutwardTransaction = generateSessionIdResult.Content;

           
            var getDebitAccountDetailsResult = await GetImalDebitAccountDetails(nipOutwardTransaction.DebitAccountNumber);

            if (!getDebitAccountDetailsResult.IsSuccess)
            {
                return mapper.Map<FundsTransferResult<string>>(getDebitAccountDetailsResult);
            }

            nipOutwardTransaction.OriginatorEmail = getDebitAccountDetailsResult.Content.DebitAccountDetails.Email;
            nipOutwardTransaction.LedgerCode = getDebitAccountDetailsResult.Content.DebitAccountDetails.T24_LED_CODE;
            nipOutwardTransaction.BranchCode = getDebitAccountDetailsResult.Content.DebitAccountDetails.T24_BRA_CODE;

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
                nipOutwardTransaction.StatusFlag = 17;
                await nipOutwardTransactionService.Update(nipOutwardTransaction);

                var updateLog = nipOutwardTransactionService.GetOutboundLog();
                
                if(!string.IsNullOrEmpty(updateLog.ExceptionDetails))
                {
                    outboundLogs.Add(updateLog);
                }

                result.IsSuccess = false;
                result.Message = "Transaction not allowed";
                result.ErrorMessage = @$"Customer of class {customerClass} with account {nipOutwardTransaction.DebitAccountNumber} 
                does not have concession and the transaction amount {nipOutwardTransaction.Amount} 
                is higher than the maximum amount per transaction";
                return result;
            }

            var nipCharges = await transactionDetailsRepository.GetNIPFee(nipOutwardTransaction.Amount);
            outboundLogs.Add(transactionDetailsRepository.GetOutboundLog());

            var usableBalance = getDebitAccountDetailsResult.Content.DebitAccountDetails.UsableBalance;
            var totalTransactionAmount = nipOutwardTransaction.Amount + nipCharges.NIPFeeAmount + nipCharges.NIPVatAmount;

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

            var debitAccountResult = await DebitImalAccount(nipOutwardTransaction, concessionTransactionAmountLimit, 
            CheckIfDateIsHolidayResult.Content, customerStatusCode, customerClass, nipCharges);
            
            if(debitAccountResult.IsSuccess)
            {
                await nipOutwardSendToNIBSSProducerService.PublishTransaction(debitAccountResult.Content);
                outboundLogs.Add(nipOutwardSendToNIBSSProducerService.GetOutboundLog());
            }

            return mapper.Map<FundsTransferResult<string>>(debitAccountResult);
        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            result.Message = "Transaction failed";
            result.ErrorMessage = "Internal Server Error";
            outboundLog.ExceptionDetails = $@"Error thrown, raw request: {nipOutwardTransaction} 
            Exception Details: {ex.Message} {ex.StackTrace}";
            outboundLogs.Add(outboundLog);
            return result;
        }
        
    }

    public async Task<FundsTransferResult<NIPOutwardTransaction>> DebitImalAccount(NIPOutwardTransaction nipOutwardTransaction, 
    ConcessionTransactionAmountLimit? concessionTransactionAmountLimit,
    bool holidayFound, int customerStatusCode, int customerClass, NIPOutwardCharges nIPOutwardCharges)
    {
        FundsTransferResult<NIPOutwardTransaction> result = new FundsTransferResult<NIPOutwardTransaction>();
        OutboundLog outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.DebitImalAccount)}";
        result.IsSuccess = false;
        try
        {
            if(concessionTransactionAmountLimit != null)
            {
                
                var concessionChecksResult = await DoChecksBasedOnConcession(nipOutwardTransaction,  
                concessionTransactionAmountLimit);

                if(!concessionChecksResult.IsSuccess)
                {
                    return concessionChecksResult;
                }

                return await CallImalToDebitAccount(nipOutwardTransaction, nIPOutwardCharges);
            }
            else
            {
                var cbnOrEftChecksResult = await DoChecksBasedOnCBNorEFTLimit(nipOutwardTransaction, holidayFound, 
                 customerStatusCode, customerClass);

                if(!cbnOrEftChecksResult.IsSuccess)
                {
                    return cbnOrEftChecksResult;
                }

                return await CallImalToDebitAccount(nipOutwardTransaction, nIPOutwardCharges);
            }
        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            result.Message = "Transaction failed";
            result.ErrorMessage = "Internal Server Error";
            var request = $@"  holidayFound: {holidayFound}, customerStatusCode: {customerStatusCode}, cus_class: {customerClass}";
            outboundLog.ExceptionDetails = $@"Error thrown, raw request: {request} 
            Exception Details: {ex.Message} {ex.StackTrace}";
            outboundLogs.Add(outboundLog);
            return result;
            
        }
    }

    public async Task<FundsTransferResult<NIPOutwardTransaction>> GenerateNameEnquirySessionId(NIPOutwardTransaction transaction)
    {
        FundsTransferResult<NIPOutwardTransaction> result = new FundsTransferResult<NIPOutwardTransaction>();
        OutboundLog outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.GenerateNameEnquirySessionId)}";
        result.IsSuccess = false;

        try
        { 
            if (transaction.DateAdded?.Date != DateTime.UtcNow.AddHours(1).Date)
            {
                transaction.NameEnquirySessionID = await transactionDetailsRepository.GenerateNameEnquirySessionId(transaction.NameEnquirySessionID);
                
                var generateNameEnquirySessionIdLog = transactionDetailsRepository.GetOutboundLog();
                
                if(!string.IsNullOrEmpty(generateNameEnquirySessionIdLog.ExceptionDetails))
                {
                    outboundLogs.Add(generateNameEnquirySessionIdLog);
                }

                NameEnquiryRequestDto nameEnquiryRequest = new NameEnquiryRequestDto
                {
                    SessionID = transaction.NameEnquirySessionID,
                    DestinationInstitutionCode = transaction.BeneficiaryBankCode,
                    ChannelCode = transaction.ChannelCode,
                    AccountNumber = transaction.CreditAccountNumber
                };

                var nameEnquiryResult = await nipOutwardNameEnquiryService.NameEnquiry(nameEnquiryRequest);
                outboundLogs.AddRange(nipOutwardNameEnquiryService.GetOutboundLogs());

                if(nameEnquiryResult.IsSuccess && nameEnquiryResult?.Content?.ResponseCode == "00")
                {
                    transaction.NameEnquirySessionID = transaction.NameEnquirySessionID;
                    transaction.CreditAccountName = nameEnquiryResult.Content.AccountNumber;
                    transaction.BeneficiaryBVN =  nameEnquiryResult.Content.BankVerificationNumber;
                    transaction.BeneficiaryKYCLevel =  nameEnquiryResult.Content.KYCLevel;
                    transaction.NameEnquiryResponse =  nameEnquiryResult.Content.ResponseCode;
                }
                else
                {
                    result.IsSuccess = false;
                    result.Message = "Name enquiry failed";
                    return result;
                }              
                   
            }

            result.IsSuccess = true;
            result.Content = transaction;
            return result;
        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            result.Message = "Transaction failed";
            result.ErrorMessage = "Internal Server Error";
            var request = JsonConvert.SerializeObject(transaction);
            outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
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
                        result.ErrorMessage = "Kia kia- 6009 (Transfer Limit - N3000.00)";
                    }
                    //Kia kia plus-6010 (Transfer Limit - N10,000.00)
                    else if (transaction.Amount > 10000 && transaction.LedgerCode == "6010")
                    {
                        transaction.StatusFlag = 13;
                        result.IsSuccess = false;
                        result.Message = "Transaction not allowed";
                        result.ErrorMessage = "Kia kia plus-6010 (Transfer Limit - N10,000.00)";
                    }
                    //Sterling Fanatic Kick off – 6011 (Transfer Limit - N3000.00)
                    else if (transaction.Amount > 3000 && transaction.LedgerCode == "6011")
                    {
                        transaction.StatusFlag = 14;
                        result.IsSuccess = false;
                        result.Message = "Transaction not allowed";
                        result.ErrorMessage = "Sterling Fanatic Kick off - 6011 (Transfer Limit - N3000.00)";
                    }
                    //Sterling Fanatic Kick off – 6012 (Transfer Limit - N10000.00)
                    else if (transaction.Amount > 10000 && transaction.LedgerCode == "6012")
                    {
                        transaction.StatusFlag = 14;
                        result.IsSuccess = false;
                        result.Message = "Transaction not allowed";
                        result.ErrorMessage = "Sterling Fanatic Kick off - 6012 (Transfer Limit - N10000.00)";
                    }
                    else{
                        result.IsSuccess = true;
                        outboundLog.ResponseDetails = $"Is success: {result.IsSuccess.ToString()}";
                        outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                        outboundLogs.Add(outboundLog);
                        return result;
                    }
                    await nipOutwardTransactionService.Update(transaction);
                    var updateLog = nipOutwardTransactionService.GetOutboundLog();
                
                    if(!string.IsNullOrEmpty(updateLog.ExceptionDetails))
                    {
                        outboundLogs.Add(updateLog);
                    }
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
            result.Message = "Transaction failed";
            result.ErrorMessage = "Internal Server Error";
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
                    var updateLog = nipOutwardTransactionService.GetOutboundLog();
                
                    if(!string.IsNullOrEmpty(updateLog.ExceptionDetails))
                    {
                        outboundLogs.Add(updateLog);
                    }
                    
                    result.Message = "Transaction not allowed";
                    result.IsSuccess = false;
                    outboundLog.ResponseDetails = $"Transaction allowed: {result.IsSuccess.ToString()}";
                    outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                    outboundLogs.Add(outboundLog);
                    return result;
                }
            }
            result.IsSuccess = true;
            outboundLog.ResponseDetails = $"Transaction allowed: {result.IsSuccess.ToString()}";
            outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            outboundLogs.Add(outboundLog);
            return result;
        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            result.Message = "Transaction failed";
            result.ErrorMessage = "Internal Server Error";
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
                    var updateLog = nipOutwardTransactionService.GetOutboundLog();
                
                    if(!string.IsNullOrEmpty(updateLog.ExceptionDetails))
                    {
                        outboundLogs.Add(updateLog);
                    }

                    result.Message = "Transaction failed";
                    result.ErrorMessage = "Unable to form the VAT account for ledger code 17201";
                    result.IsSuccess = false;
                    outboundLog.ResponseDetails = "Unable to form the VAT account for ledger code 17201";
                    outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                    outboundLogs.Add(outboundLog);
                    return result;
                }
                
                outboundLog.ResponseDetails = "Vat account formed for Branch " + createVTellerTransactionDto.VAT_bra_code + 
                " is " + createVTellerTransactionDto.VAT_sub_acct_code;
            }
            else
            {
                
                model.StatusFlag = 18;
                await nipOutwardTransactionService.Update(model);
                var updateLog = nipOutwardTransactionService.GetOutboundLog();
            
                if(!string.IsNullOrEmpty(updateLog.ExceptionDetails))
                {
                    outboundLogs.Add(updateLog);
                }

                result.Message = "Transaction failed";
                result.ErrorMessage = "Error: Unable to compute VAT and Fee for account";
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
            outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            outboundLogs.Add(outboundLog);
            return result;
        }
        catch (Exception ex)
        {
            model.StatusFlag = 19;
            await nipOutwardTransactionService.Update(model);
            var updateLog = nipOutwardTransactionService.GetOutboundLog();
                
            if(!string.IsNullOrEmpty(updateLog.ExceptionDetails))
            {
                outboundLogs.Add(updateLog);
            }
            
            result.IsSuccess = false;
            result.Message = "Transaction failed";
            result.ErrorMessage = "Error: Unable to compute VAT and Fee for account";
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
            TotalTransactionDonePerDay totalTransactionsPerDay = await transactionDetailsRepository.GetTotalTransDonePerday(transaction.Amount, transaction.DebitAccountNumber); 

            outboundLogs.Add(transactionDetailsRepository.GetOutboundLog());

            if (transaction.Amount > concessionTransactionAmountLimit.MaximumAmountPerTransaction)
            {
                outboundLog.ResponseDetails = "Customer with account " + transaction.DebitAccountNumber + 
                " has concession and the transaction amount " + transaction.Amount + 
                " is higher than the maximum per transaction" + concessionTransactionAmountLimit.MaximumAmountPerTransaction ;
                outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                outboundLogs.Add(outboundLog);
                transaction.FundsTransferResponse = "09x";    
                transaction.StatusFlag = 23;    
                await nipOutwardTransactionService.Update(transaction);
                var updateLog = nipOutwardTransactionService.GetOutboundLog();
                
                if(!string.IsNullOrEmpty(updateLog.ExceptionDetails))
                {
                    outboundLogs.Add(updateLog);
                }

                result.Content = transaction;
                result.Message = "Transaction not allowed: Transaction amount is greater than the maximum amount per transaction";
                result.ErrorMessage = outboundLog.ResponseDetails;
                result.IsSuccess = false;
                return result;
            }

            if (transaction.Amount + totalTransactionsPerDay.TotalDone > concessionTransactionAmountLimit.MaximumAmountPerDay)
            {
                decimal my1sum = 0;
                my1sum = transaction.Amount + totalTransactionsPerDay.TotalDone;
                outboundLog.ResponseDetails = "Customer with account " + transaction.DebitAccountNumber + 
                " has concession and the transaction amount plus the total NIP transactions done " + my1sum.ToString() + 
                " is higher than the maximum per transaction " + concessionTransactionAmountLimit.MaximumAmountPerTransaction.ToString() ;
                outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                outboundLogs.Add(outboundLog);

                transaction.FundsTransferResponse = "09x";
                transaction.StatusFlag = 24;
                await nipOutwardTransactionService.Update(transaction);
                var updateLog = nipOutwardTransactionService.GetOutboundLog();
            
                if(!string.IsNullOrEmpty(updateLog.ExceptionDetails))
                {
                    outboundLogs.Add(updateLog);
                }

                result.Content = transaction;
                result.Message = "Transaction not allowed. Limit exceeded for today";
                result.ErrorMessage = outboundLog.ResponseDetails;
                result.IsSuccess = false;
                return result;
            }

            if (transaction.LedgerCode == "6009" && transaction.Amount > 20000)
            {
                outboundLog.ResponseDetails = "Transaction with account " + transaction.DebitAccountNumber + 
                $" and ledger code 6009 has transaction amount {transaction.Amount} greater than 20000 ";
                outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                outboundLogs.Add(outboundLog);

                transaction.FundsTransferResponse = "13";
                transaction.StatusFlag = 25;
                await nipOutwardTransactionService.Update(transaction);
                var updateLog = nipOutwardTransactionService.GetOutboundLog();
            
                if(!string.IsNullOrEmpty(updateLog.ExceptionDetails))
                {
                    outboundLogs.Add(updateLog);
                }

                result.Content = transaction;
                result.Message = "Transaction not allowed: Transaction amount is greater than the maximum amount per transaction";
                result.ErrorMessage = outboundLog.ResponseDetails;
                result.IsSuccess = false;
                return result;
            }
           
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
            result.Message = "Transaction failed";
            result.ErrorMessage = "Internal Server Error";
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
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.DoChecksBasedOnCBNorEFTLimit)}";

        try
        {
            decimal maxPerTrans = 0;
            decimal maxPerday = 0;
            var totalTransactionsPerDay = await transactionDetailsRepository.GetTotalTransDonePerday(transaction.Amount, transaction.DebitAccountNumber);
            outboundLogs.Add(transactionDetailsRepository.GetOutboundLog());

            if (holidayFound)
            {
                if (customerStatusCode == 1 || customerStatusCode == 6)
                {
                    if (totalTransactionsPerDay.TotalCount >= 3)
                    {
                        outboundLog.ResponseDetails = @$"Customer with customer status code {customerStatusCode}
                        has exceeded the maximum count of transactions which is 3";
                        outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                        outboundLogs.Add(outboundLog);

                        transaction.FundsTransferResponse = "65";
                        transaction.StatusFlag = 28;
                        await nipOutwardTransactionService.Update(transaction);
                        var updateLog = nipOutwardTransactionService.GetOutboundLog();
            
                        if(!string.IsNullOrEmpty(updateLog.ExceptionDetails))
                        {
                            outboundLogs.Add(updateLog);
                        }

                        result.Content = transaction;
                        result.Message = "Transaction not allowed. Limit exceeded";
                        result.ErrorMessage = outboundLog.ResponseDetails;
                        result.IsSuccess = false;
                        return result;
                    }

                    if (totalTransactionsPerDay.TotalDone + transaction.Amount > 200000)
                    {  
                        outboundLog.ResponseDetails = @$"Customer with customer status code {customerStatusCode} and account " + transaction.DebitAccountNumber + 
                        " and the transaction amount plus the total NIP transactions done " + totalTransactionsPerDay.TotalDone + transaction.Amount + 
                        " is higher than the maximum per transaction " + 200000 ;
                        outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                        outboundLogs.Add(outboundLog);  

                        transaction.FundsTransferResponse = "65";
                        transaction.StatusFlag = 24;
                        await nipOutwardTransactionService.Update(transaction);

                        var updateLog = nipOutwardTransactionService.GetOutboundLog();
            
                        if(!string.IsNullOrEmpty(updateLog.ExceptionDetails))
                        {
                            outboundLogs.Add(updateLog);
                        }

                        result.Content = transaction;
                        result.Message = "Transaction not allowed. Limit exceeded for today.";
                        result.ErrorMessage = outboundLog.ResponseDetails;
                        result.IsSuccess = false;
                        return result;
                    }
                }
            }

            CBNTransactionAmountLimit cbnTransactionAmountLimit = await transactionAmountLimitService.GetCBNLimitByCustomerClass(cus_class);
            outboundLogs.Add(transactionAmountLimitService.GetOutboundLog());

            if (cus_class == 1)
            {
                if (cbnTransactionAmountLimit != null)
                {
                    maxPerTrans = cbnTransactionAmountLimit.MaximumAmountPerTransaction;
                    maxPerday = cbnTransactionAmountLimit.MaximumAmountPerDay;
                }

                //check if the ledger is for savings
                
                bool isFound = await transactionDetailsRepository.isLedgerFound(transaction.LedgerCode);
                outboundLogs.Add(transactionDetailsRepository.GetOutboundLog());

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
                if (cbnTransactionAmountLimit != null)
                {
                    maxPerTrans = cbnTransactionAmountLimit.MaximumAmountPerTransaction;
                    maxPerday = cbnTransactionAmountLimit.MaximumAmountPerDay;
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
                var updateLog = nipOutwardTransactionService.GetOutboundLog();
            
                if(!string.IsNullOrEmpty(updateLog.ExceptionDetails))
                {
                    outboundLogs.Add(updateLog);
                }

                result.Content = transaction;
                result.Message = "Transaction failed";
                result.ErrorMessage = outboundLog.ResponseDetails;
                result.IsSuccess = false;
                return result;
            }

            //this is to ensure that customers will not exceed the daily cbn limit
            if (transaction.Amount <= maxPerTrans)
            {
                if (transaction.Amount + totalTransactionsPerDay.TotalDone > maxPerday)
                {
                    outboundLog.ResponseDetails = @$"transaction amount plus total done for the day 
                    {transaction.Amount + totalTransactionsPerDay.TotalDone} greater than CBN maximum amount per day
                    {maxPerday}";
                    outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                    outboundLogs.Add(outboundLog);

                    transaction.FundsTransferResponse = "09x";
                    transaction.StatusFlag = 24;
                    await nipOutwardTransactionService.Update(transaction);
                    var updateLog = nipOutwardTransactionService.GetOutboundLog();
            
                    if(!string.IsNullOrEmpty(updateLog.ExceptionDetails))
                    {
                        outboundLogs.Add(updateLog);
                    }

                    result.Content = transaction;
                    result.Message = "Transaction not allowed. Limit exceeded for today.";
                    result.ErrorMessage = outboundLog.ResponseDetails;
                    result.IsSuccess = false;
                    return result;
                }
            }
            else
            {
                outboundLog.ResponseDetails = @$"transaction amount {transaction.Amount} greater than 
                CBN maximum amount per transaction {maxPerTrans}";
                outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                outboundLogs.Add(outboundLog);

                transaction.FundsTransferResponse = "61";
                transaction.StatusFlag = 23;
                await nipOutwardTransactionService.Update(transaction);
                var updateLog = nipOutwardTransactionService.GetOutboundLog();
            
                if(!string.IsNullOrEmpty(updateLog.ExceptionDetails))
                {
                    outboundLogs.Add(updateLog);
                }

                result.Content = transaction;
                result.Message = "Transaction not allowed: Transaction amount is greater than the maximum amount per transaction";
                result.ErrorMessage = outboundLog.ResponseDetails;
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
            result.Message = "Transaction failed";
            result.ErrorMessage = "Internal Server Error";
            var request = "Transaction object:" + JsonConvert.SerializeObject(transaction) + 
            $@"  holidayFound: {holidayFound}, customerStatusCode: {customerStatusCode}, cus_class: {cus_class}";
            outboundLog.ExceptionDetails = $@"Error thrown, raw request: {request} 
            Exception Details: {ex.Message} {ex.StackTrace}";
            outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            outboundLogs.Add(outboundLog);
            return result;  
        }
       
            
    }

    public async Task<FundsTransferResult<NIPOutwardTransaction>> CallImalToDebitAccount(NIPOutwardTransaction nipOutwardTransaction, 
    NIPOutwardCharges nIPOutwardCharges)
    {
        FundsTransferResult<NIPOutwardTransaction> result = new FundsTransferResult<NIPOutwardTransaction>();
        OutboundLog outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() }; 
        result.IsSuccess = false;
        try
        {         
            #region DoDebitCredit
            var imalPrincipalTransferRequest = CreateImalFundsTransferRequest(nipOutwardTransaction, 
            nipOutwardTransaction.Amount,
            appSettings.ImalProperties.ImalTransactionServiceProperties.PrincipalTssAccount, 
            appSettings.ImalProperties.ImalTransactionServiceProperties.PrincipalTransactionType);
            
            DateTime debitServiceRequestTime = DateTime.UtcNow.AddHours(1);
            var imalPrincipalTransferResponse = await imalTransactionService.NipFundsTransfer(imalPrincipalTransferRequest);
            DateTime debitServiceResponseTime = DateTime.UtcNow.AddHours(1);
            outboundLogs.Add(imalTransactionService.GetOutboundLog());

            var ftReference = new FTReference();

            if(imalPrincipalTransferResponse?.statusCode != "0")
            {
                return await UpdateImalResponse(nipOutwardTransaction, imalPrincipalTransferResponse, ftReference);
            }
            ftReference.Principal = $"{imalPrincipalTransferResponse.transactionNumber}";

            var imalFeeTssAccounts = appSettings.ImalProperties.ImalTransactionServiceProperties.FeeTssAccounts;
            var defaultTssAccount = appSettings.ImalProperties.ImalTransactionServiceProperties.FeeDefaultTssAccount;
            
            var  imalFeeTssAccount = imalFeeTssAccounts.GetValueOrDefault($"{nipOutwardTransaction.ChannelCode}", defaultTssAccount);
            
            var imalFeeTransferRequest = CreateImalFundsTransferRequest(nipOutwardTransaction, nIPOutwardCharges.NIPFeeAmount,
            imalFeeTssAccount, 
            appSettings.ImalProperties.ImalTransactionServiceProperties.FeeTransactionType);

            var imalFeeTransferResponse = await imalTransactionService.NipFundsTransfer(imalFeeTransferRequest);
            outboundLogs.Add(imalTransactionService.GetOutboundLog());
            ftReference.Fee = imalFeeTransferResponse?.transactionNumber;

            var imalVatTransferRequest = CreateImalFundsTransferRequest(nipOutwardTransaction, nIPOutwardCharges.NIPVatAmount,
            appSettings.ImalProperties.ImalTransactionServiceProperties.VatTssAccount, 
            appSettings.ImalProperties.ImalTransactionServiceProperties.VatTransactionType);

            var imalVatTransferResponse = await imalTransactionService.NipFundsTransfer(imalVatTransferRequest);
            outboundLogs.Add(imalTransactionService.GetOutboundLog());
            ftReference.Vat = imalVatTransferResponse?.transactionNumber;

            debitServiceResponseTime = DateTime.UtcNow.AddHours(1);
            
            nipOutwardTransaction.DebitServiceRequestTime = debitServiceRequestTime;
            nipOutwardTransaction.DebitServiceResponseTime = debitServiceResponseTime;

            return await UpdateImalResponse(nipOutwardTransaction, imalPrincipalTransferResponse, ftReference);

            #endregion
        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            result.Message = "Transaction failed";
            result.ErrorMessage = "Internal Server Error";
            var request = "Transaction object:" + JsonConvert.SerializeObject(nipOutwardTransaction);
            outboundLog.ExceptionDetails = $@"Error thrown, raw request: {request} 
            Exception Details: {ex.Message} {ex.StackTrace}";
            outboundLogs.Add(outboundLog);
            return result;
            
        }
        
    }

    public ImalTransactionRequestDto CreateImalFundsTransferRequest(NIPOutwardTransaction transaction, 
    decimal amount, string creditAccountNumber, int transactionType)
    {
        return new ImalTransactionRequestDto 
            {
                FromAccount = transaction.DebitAccountNumber,
                ToAccount = creditAccountNumber,
                TransactionType = transactionType,
                DifferentTradeValueDate = 0,
                TransactionAmount = amount,
                CurrencyCode = appSettings.ImalProperties.ImalTransactionServiceProperties.CurrencyCode,
                PaymentReference = transaction.PaymentReference,
                NarrationLine1 = transaction.PaymentReference,
                NarrationLine2 = transaction.SessionID,
                BeneficiaryName = transaction.CreditAccountName,
                SenderName = transaction.OriginatorName,
                ValueDate = DateTime.UtcNow.AddHours(1).ToString("yyyy-MM-dd HH:mm:ss.ffffff")
            };   
    }

    public async Task<FundsTransferResult<NIPOutwardTransaction>> UpdateImalResponse(NIPOutwardTransaction transaction, 
    ImalTransactionResponseDto? response, FTReference ftReference)
    {
        FundsTransferResult<NIPOutwardTransaction> result = new FundsTransferResult<NIPOutwardTransaction>();
        OutboundLog outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() }; 
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.UpdateImalResponse)}";
        result.IsSuccess = false;
        try
        {
            if (response == null)
            {
            
                //log.Info("The Response from Banks is " + response.Respreturnedcode1 + "  and hence, vTeller logs it as 3 For SessionID " + transaction.SessionID + " T24 msg ==> " + response.error_text);
                transaction.DebitResponse = 3;
                transaction.LastUpdate = DateTime.UtcNow.AddHours(1);
                transaction.StatusFlag = 26;
                transaction.FundsTransferResponse = "1x";
                await nipOutwardTransactionService.Update(transaction);
                var systemErrorUpdateLog = nipOutwardTransactionService.GetOutboundLog();
            
                if(!string.IsNullOrEmpty(systemErrorUpdateLog.ExceptionDetails))
                {
                    outboundLogs.Add(systemErrorUpdateLog);
                }

                result.IsSuccess = false;
                result.Message = "Transaction Failed";
                result.ErrorMessage = "Internal server error";
                
                outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                outboundLog.ResponseDetails = $"Internal server error";
                outboundLogs.Add(outboundLog);

                return result;
            }

            if(response.statusCode == "0")
            {                 

                result.IsSuccess = true;
                result.Message = "Customer has been debited successfully";

                transaction.DebitResponse = 1;
                transaction.StatusFlag = 9;
                transaction.KafkaStatus = "K2";
                transaction.AppsTransactionType = 1;
                transaction.OutwardTransactionType = 1;
                transaction.PrincipalResponse = ftReference.Principal;
                transaction.FeeResponse = ftReference.Fee;
                transaction.VatResponse = ftReference.Vat;

                await nipOutwardTransactionService.Update(transaction);
                var successUpdateLog = nipOutwardTransactionService.GetOutboundLog();
                
                if(!string.IsNullOrEmpty(successUpdateLog.ExceptionDetails))
                {
                    outboundLogs.Add(successUpdateLog);
                }

                result.Content = transaction;

                outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                outboundLog.ResponseDetails = $"Customer has been debited successfully";
                outboundLogs.Add(outboundLog);

            }
            else
            {
               
                result.Message = "Transaction Failed ";

                result.ErrorMessage = response.statusDesc;
                transaction.DebitResponse = 2;
                transaction.LastUpdate = DateTime.UtcNow.AddHours(1);
                transaction.StatusFlag = 27;
                transaction.FundsTransferResponse = response.statusCode;
                
                await nipOutwardTransactionService.Update(transaction);
                var failureUpdateLog = nipOutwardTransactionService.GetOutboundLog();
            
                if(!string.IsNullOrEmpty(failureUpdateLog.ExceptionDetails))
                {
                    outboundLogs.Add(failureUpdateLog);
                }

                result.IsSuccess = false;
                

                outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                outboundLog.ResponseDetails = $"Transaction Failed: status code: {response.statusCode} status description {response.statusDesc}";
                outboundLogs.Add(outboundLog);

                return result;
            }

            return result;

        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            result.Message = "Transaction failed";
            result.ErrorMessage = "Internal Server Error";
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
  

    public async Task<FundsTransferResult<GetDebitAccountDetailsDto>> GetImalDebitAccountDetails(string debitAccountNumber)
    {
        FundsTransferResult<GetDebitAccountDetailsDto> result = new FundsTransferResult<GetDebitAccountDetailsDto>();
        OutboundLog outboundLog = new OutboundLog  { OutboundLogId = ObjectId.GenerateNewId().ToString() }; 
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.GetImalDebitAccountDetails)}";
        
        result.IsSuccess = false;
        try
        {
            result.Content = new GetDebitAccountDetailsDto();
            var accountDetails = await imalInquiryService.GetAccountDetailsByNuban(debitAccountNumber);

            outboundLogs.Add(imalInquiryService.GetOutboundLog());

            if (accountDetails ==  null || accountDetails.Message.Trim().ToUpper() != 
            appSettings.ImalProperties.ImalInquiryServiceProperties.GetAccountSuccessMessage.ToUpper().Trim())
            {
                result.IsSuccess = false;
                result.Message = "Transaction failed";
                result.ErrorMessage = "Failed to fetch Imal account details";
                return result;
            }
            
            int customerClass = 0;

            int customerStatusCode = 0;

            //if (cusclassval == "Individual Customer")
            if (accountDetails.GetAccounts.CUST_TYPE.ToUpper().Trim() == "INDIVIDUAL")
            {
                customerClass = 1;
                customerStatusCode = 1;
            }
            else if (accountDetails.GetAccounts.CUST_TYPE.ToUpper().Trim() == "CORPORATE")
            {
                customerClass = 1;
                customerStatusCode = 6;
            }
            else
            {
                customerClass = 2;
                customerStatusCode = 2;
            }

            //
            var debitAccountDetails = new DebitAccountDetails();
            debitAccountDetails.T24_LED_CODE = accountDetails.GetAccounts.GL_CODE.ToString();
            debitAccountDetails.UsableBalance = Convert.ToDecimal(accountDetails.GetAccounts.Aval_Balance);
            debitAccountDetails.Email = accountDetails.GetAccounts.EMAIL;
            debitAccountDetails.T24_BRA_CODE = accountDetails.GetAccounts.BRANCH_CODE;
            debitAccountDetails.CustomerStatusCode = customerStatusCode;
            //
            
            result.Content.CustomerClass = customerClass;
            result.Content.DebitAccountDetails = debitAccountDetails;
            result.IsSuccess = true;
            outboundLog.RequestDetails = "Successfully fetched imal account details";
            outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            outboundLogs.Add(outboundLog);
            return result;
        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            var rawRequest = debitAccountNumber; 
            outboundLog.ExceptionDetails = $@"Error thrown, Raw Request: {rawRequest}
            Exception Details: {ex.Message} {ex.StackTrace}";
            outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            outboundLogs.Add(outboundLog);
            result.Message = "Transaction failed";
            result.ErrorMessage = "Internal Server Error";
            return result;
        }
    }
}