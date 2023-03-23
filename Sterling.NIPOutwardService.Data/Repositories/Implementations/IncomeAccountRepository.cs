using Newtonsoft.Json;

namespace Sterling.NIPOutwardService.Data.Repositories.Implementations;

public class IncomeAccountRepository : IIncomeAccountRepository
{
    private readonly AppSettings appSettings;
    private OutboundLog outboundLog;
    private readonly AsyncRetryPolicy retryPolicy;
    public IncomeAccountRepository(IOptions<AppSettings> appSettings)
    {
        this.appSettings = appSettings.Value;
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
    public async Task <string?> GetExpcode()
    {
        var res = string.Empty;
        string sql = "spd_getExplcode";
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.GetExpcode)}";

        using (SqlConnection connection = new SqlConnection(appSettings.SqlServerDbConnectionString))
        {
            try
            {
                
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        connection.Open();
                         var dr = await command.ExecuteReaderAsync();
                        if (dr.HasRows)
                        {
                            while (dr.Read())
                            {
                                res = dr["expcodeVal"].ToString();
                                break;
                            }

                        }
                    });
                   
                }
            }
            catch (Exception ex)
            {
                outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + 
        "\r\n" + $@"Exception Details: {ex.Message} {ex.StackTrace}";
                //log.Error($"Error occured while calling getExpcode", ex);
            }
            connection.Close();
        }
        outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.ResponseDetails = res;
        return res;
    }

    public async Task<string?> GetCurrentTss()
    {
        var res = string.Empty;
        string sql = "spd_getCurrentTss";
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.GetCurrentTss)}";
        using (SqlConnection connection = new SqlConnection(appSettings.SqlServerDbConnectionString))
        {
            try
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        connection.Open();
                        var dr = await command.ExecuteReaderAsync();
                        if (dr.HasRows)
                        {
                            while (dr.Read())
                            {
                                res = dr["bra_code"].ToString();
                                break;
                            }

                        }
                    });
                    
                }
            }
            catch (Exception ex)
            {
                outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + 
                "\r\n" + $@"Exception Details: {ex.Message} {ex.StackTrace}";
                //log.Error($"Error occured while calling getCurrentTss", ex);
            }
            connection.Close();
        }
        outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.ResponseDetails = res;
        return res;
    }

    public async Task<Fee?> GetCurrentIncomeAcct()
    {
        Fee? res = new Fee();
        string sql = "spd_getCurrentIncomeAcct";
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.GetCurrentIncomeAcct)}";

        using (SqlConnection connection = new SqlConnection(appSettings.SqlServerDbConnectionString))
        {
            try
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        connection.Open();
                        var dr = await command.ExecuteReaderAsync();
                        if (dr.HasRows)
                        {
                            while (dr.Read())
                            {
                                res.bra_code = dr["bra_code"].ToString();
                                res.cusnum = dr["cusnum"].ToString();
                                res.curcode = dr["curcode"].ToString();
                                res.ledcode = dr["ledcode"].ToString();
                                res.subacctcode = dr["subacctcode"].ToString();

                                break;
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
        "\r\n" + $@"Exception Details: {ex.Message} {ex.StackTrace}";
                return null;
            }
            connection.Close();
        }
        outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.ResponseDetails = JsonConvert.SerializeObject(res);
        return res;
    }
    
    public async Task<string?> getCurrentTss2()
    {
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.getCurrentTss2)}";
        var res = string.Empty;
        string sql = "select cus_num as cusnum,cur_code as curcode,led_code as ledcode,sub_acct_code as subacctcode" +
                " from tbl_sett_branch_acct where statusflag=1 ";
        using (SqlConnection connection = new SqlConnection(appSettings.SqlServerDbConnectionString))
        {
            try
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.CommandType = CommandType.Text;

                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        connection.Open();
                        var dr = await command.ExecuteReaderAsync();
                        if (dr.HasRows)
                        {
                            while (dr.Read())
                            {
                                res = dr["cusnum"].ToString();
                                break;
                            }

                        }
                    });
                    
                }
            }
            catch (Exception ex)
            {
                outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + 
                "\r\n" + $@"Exception Details: {ex.Message} {ex.StackTrace}";
                return null;
            }
            connection.Close();
        }
        outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.ResponseDetails = res;
        return res;
    }

    public OutboundLog GetOutboundLog()
    {
        var recordToBeMoved = this.outboundLog;
        this.outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
        return recordToBeMoved;
    }
}