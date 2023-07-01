using Newtonsoft.Json;

namespace Sterling.NIPOutwardService.Data.Repositories.Implementations;

public class TransactionDetailsRepository:ITransactionDetailsRepository
{
    private readonly AppSettings appSettings;
    private readonly IUtilityHelper utilityHelper;
    private OutboundLog outboundLog;
    private readonly AsyncRetryPolicy retryPolicy;


    public TransactionDetailsRepository(IOptions<AppSettings> appSettings, IUtilityHelper utilityHelper)
    {
        this.appSettings = appSettings.Value;
        this.utilityHelper = utilityHelper;
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
                at {DateTime.UtcNow.AddHours(1)} Exception Details: {exception.Message} {exception.StackTrace} " ;
        });
    }
    public async Task<string> GenerateNameEnquirySessionId(string OldNameEnquirySessionId)
    {
        string Sql = "select NameEnquirySessionID  from tbl_NIPOutwardTransactions where NameEnquirySessionID=@NameEnquirySessionID";
        string NameEnquirySessionId = OldNameEnquirySessionId;
            using (SqlConnection connection = new SqlConnection(appSettings.SqlServerDbConnectionString))
            {
                try
                {
                    bool isUnique = false;
                    NameEnquirySessionId = appSettings.SterlingBankCode + DateTime.Now.ToString("yyMMddHHmmss") + utilityHelper.GenerateRandomNumber(12);
                    while (!isUnique)
                    {
                        using (SqlCommand command = new SqlCommand(Sql, connection))
                        {
                            command.CommandType = CommandType.Text;
                            command.Parameters.AddWithValue("@NameEnquirySessionID", NameEnquirySessionId);
                            connection.Open();

                            await retryPolicy.ExecuteAsync(async () =>
                            {
                                SqlDataReader record;

                                
                                record = await command.ExecuteReaderAsync();

                                if (record.HasRows)
                                {
                                    NameEnquirySessionId = appSettings.SterlingBankCode + DateTime.Now.ToString("yyMMddHHmmss") + utilityHelper.GenerateRandomNumber(12);
                                    isUnique = false;
                                }
                                else
                                {
                                    isUnique = true;
                                }
                            });
                            
                        }
                    }
                }
                catch (Exception ex)
                {
                     outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + 
            "\r\n" + $@"Raw Request {OldNameEnquirySessionId} Exception Details: {ex.Message} {ex.StackTrace}";
                }
            }
            return NameEnquirySessionId;
        }

    public async Task<NIPOutwardCharges> GetNIPFee(decimal amt)
    {
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.GetNIPFee)}";
        outboundLog.RequestDetails = $@"Amount: {amt}";
        var nipOutwardCharges = new NIPOutwardCharges
        {
            ChargesFound = false
        };
        string sql = "spd_getNIPFeeCharge";
        using (SqlConnection connection = new SqlConnection(appSettings.SqlServerDbConnectionString))
        {
            try
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@amt", amt);
                    connection.Open();
                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        
                        var dr = await command.ExecuteReaderAsync();
                        if (dr.HasRows)
                        {
                            while (dr.Read())
                            {
                                nipOutwardCharges.NIPFeeAmount = Convert.ToDecimal(dr["feeAmount"]?.ToString());
                                nipOutwardCharges.NIPVatAmount = Convert.ToDecimal(dr["vat"]?.ToString());
                                nipOutwardCharges.ChargesFound = true;
                                break;
                            }

                        }
                    });
                    
                }
            }
            catch (Exception ex)
            {
                outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + 
        "\r\n" + $@"Raw Request {amt} Exception Details: {ex.Message} {ex.StackTrace}";
            }
            connection.Close();
        }
        outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.ResponseDetails = JsonConvert.SerializeObject(nipOutwardCharges);
        return nipOutwardCharges;
    }

    public async Task<TotalTransactionDonePerDay> GetTotalTransDonePerday(decimal transactionAmount, string debitAccountNumber)
    {
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.GetTotalTransDonePerday)}";
        outboundLog.RequestDetails = $@"Raw Request: transactionAmount: {transactionAmount}, debitAccountNumber: {debitAccountNumber}";

        var res = new TotalTransactionDonePerDay();
        string sql = "select ISNULL(SUM(amount),0) as totalTOday,ISNULL(count(amount),0) as count from tbl_NIPOutwardTransactions with (nolock) " +
            " where DebitAccountNumber =@DebitAccountNumber" +
            " and CONVERT(Varchar(20), dateadded,102) = CONVERT(Varchar(20), GETDATE(),102) and DebitResponse=1 and FundsTransferResponse='00'";
        using (SqlConnection connection = new SqlConnection(appSettings.SqlServerDbConnectionString))
        {
            try
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@DebitAccountNumber", debitAccountNumber);
                    connection.Open();
                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        
                        var dr = await command.ExecuteReaderAsync();
                        if (dr.HasRows)
                        {
                            while (dr.Read())
                            {
                                
                                res.TotalDone = Convert.ToDecimal(dr["totalTOday"]?.ToString());
                                res.TotalCount = Convert.ToInt32(dr["count"]?.ToString());
                            }

                        }
                    });
                    
                }
            }
            catch (Exception ex)
            {
                outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + 
        "\r\n" + $@"Raw Request: transactionAmount: {transactionAmount}, debitAccountNumber: {debitAccountNumber} Exception Details: {ex.Message} {ex.StackTrace}";
            }
            connection.Close();
        }
        outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.ResponseDetails = JsonConvert.SerializeObject(res);
        return res;
    }

    public async Task<bool> isDateHoliday(DateTime dt)
    {
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.isDateHoliday)}";
        outboundLog.RequestDetails = $@"Date: {dt.ToString()}";

        bool found = false;
        string sql = @"select 1 from tbl_public_holiday where holiday =@dt";
        using (SqlConnection connection = new SqlConnection(appSettings.SqlServerDbConnectionString))
        {
            try
            {
                
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@dt", dt);
                    connection.Open();
                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        
                        var dr = await command.ExecuteReaderAsync();
                        if (dr.HasRows)
                        {
                            found = true;
                        }
                    });
                    
                }
            }
            catch (Exception ex)
            {
                outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + 
        "\r\n" + $@"Raw Request {dt.ToString()} Exception Details: {ex.Message} {ex.StackTrace}";
            }
            connection.Close();
        }
        outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.ResponseDetails = $"Is date holiday response: {found}";
        return found;
    }

    public async Task<bool> isBankCodeFound(string bankCode)
    {
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.isBankCodeFound)}";
        outboundLog.RequestDetails = $"{bankCode}";
        bool found = false;
        string sql = @"SELECT T24_BRACODE from tbl_sbpbankcodes where T24_BRACODE =@bankCode and statusflag =1";
        using (SqlConnection connection = new SqlConnection(appSettings.SqlServerDbConnectionString))
        {
            try
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@bankCode", bankCode);
                    connection.Open();
                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        
                        var dr = await command.ExecuteReaderAsync();
                        if (dr.HasRows)
                        {
                            found = true;
                        };
                    });
                    
                }
            }
            catch (Exception ex)
            {
                outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + 
        "\r\n" + $@"Raw Request {bankCode} Exception Details: {ex.Message} {ex.StackTrace}";
            }
            connection.Close();
        }
        outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.ResponseDetails = $"found: {found}";
        return found;
    }

    public async Task<bool> isLedgerNotAllowed(string ledgerCode)
    {
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.isLedgerNotAllowed)}";
        outboundLog.RequestDetails = $"{ledgerCode}";
        bool found = false;
        string sql = @"select * from tbl_sbpLedcodenotallowed where led_code=@ledgerCode and statusflag =1";
        using (SqlConnection connection = new SqlConnection(appSettings.SqlServerDbConnectionString))
        {
            try
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@ledgerCode", ledgerCode);
                    connection.Open();
                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        
                        var dr = await command.ExecuteReaderAsync();
                        if (dr.HasRows)
                        {
                            found = true;
                        }
                    });
                    
                }
            }
            catch (Exception ex)
            {
                outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + 
        "\r\n" + $@"Raw Request {ledgerCode} Exception Details: {ex.Message} {ex.StackTrace}";
            }
            connection.Close();
        }
        outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.ResponseDetails = $"found: {found}";
        return found;
    }

    public async Task<bool> isLedgerFound(string led_code)
    {
        bool found = false;
        string sql = @"select led_code from tbl_savingsLedcode where led_code =@lc";
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.isLedgerFound)}";
        outboundLog.RequestDetails = $"{led_code}";
        using (SqlConnection connection = new SqlConnection(appSettings.SqlServerDbConnectionString))
        {
            try
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@lc", led_code);
                    connection.Open();
                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        
                        var dr = await command.ExecuteReaderAsync();
                        if (dr.HasRows)
                        {
                            found = true;
                        }
                    });
                    
                }
            }
            catch (Exception ex)
            {
                outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + 
        "\r\n" + $@"Raw Request {led_code} Exception Details: {ex.Message} {ex.StackTrace}";
            }
            connection.Close();
        }
        outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.ResponseDetails = $"is Ledger Found in savings table{found}";
        return found;
    }

    public async Task<FreeFeeAccount?> GetFreeFeeAccount(string accountNo)
    {
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.GetFreeFeeAccount)}";
        outboundLog.RequestDetails = $"{accountNo}";

        FreeFeeAccount? res = null;
        string sql = "select top 1 nuban,feeAccount,VatAccount,AddedBy from tbl_freebanking where nuban = @accountNo";
        using (SqlConnection connection = new SqlConnection(appSettings.SqlServerDbConnectionString))
        {
            try
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.CommandType = CommandType.Text;
                    command.Parameters.AddWithValue("@accountNo", accountNo);
                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        
                        var dr = await command.ExecuteReaderAsync();
                        if (dr.HasRows)
                        {
                            while (dr.Read())
                            {
                                res = new FreeFeeAccount();
                                res.feeAccount = dr["feeAccount"].ToString();
                                res.VatAccount = dr["VatAccount"].ToString();
                            }
                            
                        }
                        else
                        {
                            res = null;
                        }
                    });
                    
                }
            }
            catch (Exception ex)
            {
                outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + 
                "\r\n" + $@"Account number {accountNo} Exception Details: {ex.Message} {ex.StackTrace}";
                outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                return null;

            }
            connection.Close();
        }
        outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.ResponseDetails = JsonConvert.SerializeObject(res);
        return res;
    }
    public OutboundLog GetOutboundLog()
    {
        var recordToBeMoved = this.outboundLog;
        this.outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
        return recordToBeMoved;
    }
}