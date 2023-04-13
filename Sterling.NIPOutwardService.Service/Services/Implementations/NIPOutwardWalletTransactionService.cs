// namespace Sterling.NIPOutwardService.Service.Services.Implementations;

// public class NIPOutwardWalletTransactionService : INIPOutwardWalletTransactionService
// {
//     private readonly INIPOutwardWalletTransactionRepository nipOutwardWalletTransactionRepository;
//     private List<OutboundLog> outboundLogs;
//     private readonly AsyncRetryPolicy retryPolicy;
//     private readonly IWalletFraudAnalyticsService walletFraudAnalyticsService;
//     private readonly IWalletTransactionService walletTransactionService;
//     private readonly AppSettings appSettings;
//     public NIPOutwardWalletTransactionService(INIPOutwardWalletTransactionRepository nipOutwardWalletTransactionRepository,
//     IWalletFraudAnalyticsService walletFraudAnalyticsService, IWalletTransactionService walletTransactionService,
//     IOptions<AppSettings> appSettings)
//     {
//         this.nipOutwardWalletTransactionRepository = nipOutwardWalletTransactionRepository;
//         this.outboundLogs = new List<OutboundLog> ();
//         this.walletFraudAnalyticsService = walletFraudAnalyticsService;
//         this.walletTransactionService = walletTransactionService;
//         this.appSettings = appSettings.Value;
//         this.retryPolicy = Policy.Handle<Exception>()
//         .WaitAndRetryAsync(new[]
//         {
//             TimeSpan.FromSeconds(1),
//             TimeSpan.FromSeconds(2),
//             TimeSpan.FromSeconds(4)
//         }, (exception, timeSpan, retryCount, context) =>
//         {
//             var outboundLog = new OutboundLog  { OutboundLogId = ObjectId.GenerateNewId().ToString() };
//             outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + "\r\n" + @$"Retrying due to {exception.GetType().Name}... Attempt {retryCount}
//                 Exception Details: {exception.Message} {exception.StackTrace} " ;
//             outboundLogs.Add(outboundLog);
//         });
//     }
//     public async Task<Result<string>> ProcessTransaction(NIPOutwardWalletTransaction request)
//     {
//         try
//         {
//             request.StatusFlag = 3;
//             request.DateAdded = DateTime.UtcNow.AddHours(1);
//             await nipOutwardWalletTransactionRepository.Create(request);

//             var payload = new WalletFraudAnalyticsRequestDto
//             {
//                 AppId = request.AppId,
//                 ReferenceId = request.SessionID,
//                 FromAccount = request.DebitAccountNumber,
//                 ToAccount = request.CreditAccountNumber,
//                 SenderName = request.OriginatorName,
//                 BeneficiaryName = request.CreditAccountName,
//                 Amount = (float)(request.Amount),
//                 BeneficiaryBankCode = request.BeneficiaryBankCode,
//                 IsWalletOnly = appSettings.WalletFraudAnalyticsProperties.IsWalletOnly == false ? 0 : 1,
//                 TransactionType = appSettings.WalletFraudAnalyticsProperties.TransactionType,
//                 TransTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
//             };

//             var fraudScoreResult = await walletFraudAnalyticsService.GetFraudScore(payload);

//             if(fraudScoreResult == null)
//             {
//                 //logger.Error($"Seems an exception occured while calling FraudAPI to score transaction ...");
//                 //continue;
//             }
//             //logger.Info($"FraudApi Response- {JsonConvert.SerializeObject(fraudApiScore)} transaction ID - {item.ID}");

//             if(fraudScoreResult.ResponseCode != "00")
//             {
//                 request.FraudResponseCode = fraudScoreResult.ResponseCode;
//                 request.FraudResponseMessage = "An Error Occurred, check the logs to get the error";
//                 await Update(request);
//                 //logger.Info($"FraudApi scoring wasn't completed succesfully- updating fraudResponses on the table"); 
//             }

//             if(fraudScoreResult.ResponseCode == "00")
//             {
//                 if(fraudScoreResult.FraudScore == "-1")
//                 {
//                     request.FraudResponseCode = fraudScoreResult.ResponseCode;
//                     request.FraudResponseMessage = "An Error Occurred, check the logs to get the error";
//                     await Update(request);   
//                     //logger.Error($"An error occurred during scoring. updating fraudREsponses on the table");
//                 }

//                 if(fraudScoreResult.FraudScore == "1")
//                 {
//                     request.FraudResponseCode = fraudScoreResult.ResponseCode;
//                     request.FraudResponseMessage = fraudScoreResult.ErrorMessage ?? "Suspicious Transaction";
//                     request.StatusFlag = 5;
//                     await Update(request);   
//                     //logger.Info($"FraudAPI score transaction with ID {item.ID} as suspicious. updating status to 5 for further investigation");
//                 }

//                 if(fraudScoreResult.FraudScore == "0")
//                 {
//                     request.FraudResponseCode = fraudScoreResult.ResponseCode;
//                     request.FraudResponseMessage = fraudScoreResult.ErrorMessage ?? "Transaction should be processed";
//                     await Update(request);
//                     //logger.Info($"FraudAPI score transaction with ID {item.ID} as not fraudulent and transaction should be processed... ");
//                     //logger.Info($"update FraudAPi operation. Response- {updateFruadApiResponse}");

//                     //call wallet To wallet first, if okay, then insert into Nip table
//                     //logger.Info($"WalletToOther bank operation: Calling WalletToWallet API ...");
//                     var walletToWalletpayload = new WalletToWalletRequestDto
//                     {
//                         amt = request.Amount.ToString(),
//                         channelID = request.AppId,
//                         CURRENCYCODE = request.CurrencyCode,
//                         frmacct = request.DebitAccountNumber,
//                         paymentRef = request.SessionID,
//                         remarks = request.PaymentReference,
//                         toacct = appSettings.WalletTransactionServiceProperties.WalletPoolAccount, //this should be WalletPoolAccount
//                         TransferType = 2 //doing this so on wallet's end the charge will be debited
//                     };
//                     //logger.Info($"WalletToOtherBank: wallet to Wallet with transaction ID - {item.ID} - payload - {JsonConvert.SerializeObject(walletToWalletpayload)}");
//                     var walletToWalletResponse = await walletTransactionService.WalletToWalletTransfer(walletToWalletpayload);

//                     //logger.Info($"wallet to wallet REsponse - {JsonConvert.SerializeObject(walletToWalletResponse)} - transaction ID- {item.ID}");
//                     if (walletToWalletResponse == null)
//                     {
//                         request.ResponseCode = string.Empty;
//                         request.ResponseMessage = $"An exception occurred, pls check the logs - DateTime- {DateTime.UtcNow.AddHours(1)}";
//                         request.StatusFlag = 7;
//                         await Update(request);
//                         //logger.Info($"seems an exception occured while Calling WalletToWallet API ...");
//                         //continue;
//                     }
//                     if(walletToWalletResponse !=null && walletToWalletResponse.response != "00")
//                     {
//                         request.ResponseCode = walletToWalletResponse.response;
//                         request.ResponseMessage = $"{walletToWalletResponse.message}";
//                         request.StatusFlag = 7;
//                         await Update(request);
//                         //logger.Error("WalletToOther bank Operation: wallet to walet operation failed, not inserting into nip table");
//                         //continue;
//                     }
//                     if(walletToWalletResponse !=null && walletToWalletResponse.response == "00")
//                     {
//                         //logger.Error("WalletToOther bank Operation: wallet to walet operation succesful, inserting into nip table");

//                         var nipPayload = new NipRequestVm
//                         {
//                             AppId = Convert.ToInt32(item.ChanelId),
//                             sessionid = item.SessionId,
//                             sessionidNE = item.NESessionid,
//                             transactioncode = null,
//                             channelCode = Convert.ToInt32("3"), //3
//                             paymentRef = item.Narration,
//                             amt = item.Amount,
//                             feecharge = 0.0m,
//                             vat = 0.0m,
//                             AccountName = item.CreditAccountName,
//                             AccountNumber = item.CreditAccountNumber,
//                             originatorname = item.DebitAccountName,
//                             bankcode = item.DestinationBankCode,
//                             StatusFlag = 50,
//                             BeneficiaryBVN = BeneficiaryBvn,
//                             BeneficiaryKYCLevel = item.BeneficiaryKYCLevel.ToString(),
//                             Nuban =item.DebitAccountNumber.Substring(1)
//                         };
//                         //logger.Info($"transaction with ID- {item.ID} - NIP paylod- {JsonConvert.SerializeObject(nipPayload)} - DateTime- {DateTime.Now}");

//                         var insertNipDataResp = transactionProcessor.InsertIntotbl_nibssmobile(nipPayload).GetAwaiter().GetResult();
//                         if (insertNipDataResp == null)
//                         {
//                             logger.Error($"Inserting into Nip table failed, updating statusFlag to 10 (which means inserting into NIP table failed). trasaction ID- {item.ID}");
                        
//                             //update responses
//                             logger.Error("WalletToOther bank Operation: wallet to walet operation succesful, updating responses");
//                             var updateWalletResponse = transactionProcessor.UpdateWalletResponseCodeAndMessg(walletToWalletResponse.response, $"{walletToWalletResponse.message}", item.ID).GetAwaiter().GetResult();
//                             var updateStatusFlag = transactionProcessor.UpdateStatusFlag("10", item.ID).GetAwaiter().GetResult();

//                             logger.Info($"update walletResponse -{updateWalletResponse} - update statusFlag response- {updateStatusFlag}");
//                             if (updateStatusFlag)
//                             {
//                                 logger.Info($"update status Flag to 10 operation sucesful . transaction ID {item.ID}");
//                                 continue;
//                             }

//                             continue;
//                         }
//                         if(insertNipDataResp == "Succes")
//                         {
//                             logger.Info($"inserting into nip Table for transaction with ID-{item.ID} succesfull. Response-{insertNipDataResp}");

//                             logger.Error("WalletToOther bank Operation: updating responses");
//                             var updateWalletResponse = transactionProcessor.UpdateWalletResponseCodeAndMessg(walletToWalletResponse.response, $"{walletToWalletResponse.message}", item.ID).GetAwaiter().GetResult();
//                             var updateStatusFlag = transactionProcessor.UpdateStatusFlag("100", item.ID).GetAwaiter().GetResult();

//                             logger.Info($"update walletResponse -{updateWalletResponse} - update statusFlag response- {updateStatusFlag}");
//                             if (updateStatusFlag)
//                             {
//                                 //logger.Info($"update status Flag to 100 operation sucesful . transaction ID {item.ID}");
//                                 //continue;
//                             }
//                             //continue;
//                         }
//                         //continue;
//                     }
//                 }
//                 //continue;
//             }               

//         }
//         catch (System.Exception)
//         {
            
            
//         }
//     }

//     public async Task<int>  Update(NIPOutwardWalletTransaction request)
//     {
//         var recordsUpdated = 0;
//         await retryPolicy.ExecuteAsync(async () =>
//         {
//             recordsUpdated = await nipOutwardWalletTransactionRepository.Update(request);
//         });

//         return recordsUpdated;
//     }
// }