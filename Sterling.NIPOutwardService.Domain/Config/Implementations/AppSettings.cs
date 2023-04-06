namespace Sterling.NIPOutwardService.Domain.Config.Implementations;

public class AppSettings 
{
    public string SqlServerDbConnectionString { get; set; }
    public string T24DbConnectionString { get; set; }
    public string NameEnquirySoapService { get; set; }
    public string SterlingBankCode { get; set; }
    public string SterlingProSuspenseAccount { get; set; }
    public string ProxySwitch { get; set; }
    public int ProxyPort { get; set; }
    public string ProxyHost { get; set; }
    public string ProxyUsername { get; set; }
    public string ProxyPassword { get; set; }
    public string FraudBaseUrl { get; set; }
    public string FraudAnalyticsRequest { get; set; }
    public decimal FraudMinimumAmount { get; set; }
    public string NIP_PL_ACCT_USSD { get; set; }
    public string NIP_PL_ACCT_CIB { get; set; }
    public string NIP_PL_ACCT_WHATSAPP { get; set; }
    public string NIP_PL_ACCT_CHATPAY { get; set; }
    public string NIP_PL_ACCT_OTHERS { get; set; }
    public decimal SWITCHNIPFEE { get; set; }
    public decimal FLUTTERWAVE_FEE { get; set; }
    public decimal ZDVANCE_FEE { get; set; }
    public decimal KUDI_FEE { get; set; }
    public string AesSecretKey { get; set; }
    public string AesInitializationVector { get; set; }
    public VtellerProperties VtellerProperties { get; set; }
}

public class VtellerProperties 
{
    public string ApiKey { get; set; }
    public string BaseUrl { get; set; }
    public string DebitRequest { get; set; }
}
