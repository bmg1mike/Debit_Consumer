using Oracle.ManagedDataAccess.Client;

namespace Sterling.NIPOutwardService.Data.Repositories.Implementations;

public class DebitAccountRepository : IDebitAccountRepository
{
    private readonly AppSettings appSettings;
    private readonly IUtilityHelper utilityHelper;
    private OutboundLog outboundLog;
    private readonly AsyncRetryPolicy retryPolicy;

    public DebitAccountRepository(IOptions<AppSettings> appSettings, IUtilityHelper utilityHelper)
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
                Exception Details: {exception.Message} {exception.StackTrace} " ;
        });
    }
    
    public async Task<DebitAccountDetails?> GetDebitAccountDetails(string AccountNumber)
    {
        //
        // DebitAccountDetails? accountDetails = new DebitAccountDetails
        // {
        //     T24_LED_CODE = "1006",
        //     UsableBalance = 100000,
        //     Email = "goz@gmail.com",
        //     CustomerStatusCode = 6,
        //     T24_BRA_CODE = "NG0020006"
        // };

        // return accountDetails;
        //
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        DebitAccountDetails? accountDetails = null;
        using (OracleConnection connection = new(appSettings.T24DbConnectionString))
        {
            using (OracleCommand cmd = connection.CreateCommand())
            {
                
                try
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = @"DECLARE PACCT VARCHAR2(200); PREFCUR SYS_REFCURSOR; BEGIN PACCT := :pAcct;
                    STAFJ.GET_FULL_ACCOUNT_INFO(PACCT => PACCT,PREFCUR => PREFCUR);:PREFCUR:= PREFCUR;END;";
                    cmd.Parameters.Add(":pACCT", OracleDbType.Varchar2).Value = AccountNumber;
                    cmd.Parameters.Add(":PREFCUR", OracleDbType.RefCursor).Direction = ParameterDirection.Output;
                    
                    //await retryPolicy.ExecuteAsync(async () =>
                    //{
                        await connection.OpenAsync();
                        OracleDataReader dr = cmd.ExecuteReader();
                        if (dr.HasRows)
                        {
                            while (dr.Read())
                            {
                                accountDetails = new DebitAccountDetails();
                                accountDetails.T24_LED_CODE = dr["ACCOUNTCATEGORY"]?.ToString();
                                accountDetails.UsableBalance = Convert.ToDecimal(dr["USABLE_BALANCE"]?.ToString());
                                accountDetails.Email = dr["EMAIL"]?.ToString();
                                int customerStatusCode;
                                bool isValidInt = int.TryParse(dr["CUSTOMER_STATUS"]?.ToString(), out customerStatusCode);
                                accountDetails.CustomerStatusCode = isValidInt ? customerStatusCode : 2;
                                accountDetails.T24_BRA_CODE = dr["BRA_CODE"]?.ToString();
                            
                            }
                            
                        }
                            else
                        {
                            accountDetails = null;
                            outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
                            outboundLog.ResponseDetails = "no data returned for record";
                        }
                        await connection.CloseAsync();
                    //});
                    
                }
                catch (Exception ex)
                {
                    accountDetails = null;
                    outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + 
                    "\r\n" + $@"Account number: {AccountNumber} Exception Details: {ex.Message} {ex.StackTrace}";
                }
                            
            }
        }
        return accountDetails;
    }

    public OutboundLog GetOutboundLog()
    {
        var recordToBeMoved = this.outboundLog;
        this.outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
        return recordToBeMoved;
    }

}