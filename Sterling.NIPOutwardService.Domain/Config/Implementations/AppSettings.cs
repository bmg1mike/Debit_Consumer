namespace Sterling.NIPOutwardService.Domain.Config.Implementations;

public class AppSettings 
{
    public string SqlServerDbConnectionString { get; set; }
    public string T24DbConnectionString { get; set; }
    public string SterlingBankCode { get; set; }
    public string SterlingProSuspenseAccount { get; set; }
    public string OneBankWalletPoolAccount { get; set; }
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
    public int InMemoryCacheDurationInHours { get; set; }
    public VtellerProperties VtellerProperties { get; set; }
    public WalletFraudAnalyticsProperties WalletFraudAnalyticsProperties { get; set; }
    public WalletTransactionServiceProperties WalletTransactionServiceProperties { get; set; }
    public NibssNipServiceProperties NibbsNipServiceProperties { get; set; }
    public ImalProperties ImalProperties { get; set; }
}

public class VtellerProperties 
{
    public string ApiKey { get; set; }
    public string BaseUrl { get; set; }
    public string DebitRequest { get; set; }
    public int TimeoutInMinutes { get; set; }
}

public class WalletFraudAnalyticsProperties 
{
    public string BaseUrl { get; set; }
    public string GetScoreRequest { get; set; }
    public int TransactionType { get; set; }
    public bool IsWalletOnly { get; set; }
    public int TimeoutInMinutes { get; set; }
}

public class WalletTransactionServiceProperties
{
    public string BaseUrl { get; set; }
    public string TransferRequest { get; set; }
    public string SecretKey { get; set; }
    public string IV { get; set; }
    public string WalletPoolAccount { get; set; }
    public int TimeoutInMinutes { get; set; }
}

public class NibssNipServiceProperties
{
    //public string NIPEncryptionSocketIP { get; set; }
    //public int NIPEncryptionSocketPort { get; set; }
    //public string NIPEncryptionSocketPassword { get; set; }
    public string NIPNIBSSService { get; set; }
    public int NIBSSNIPServiceCloseTimeoutInMinutes { get; set; }
    public int NIBSSNIPServiceOpenTimeoutInMinutes { get; set; }
    public int NIBSSNIPServiceReceiveTimeoutInMinutes { get; set; }
    public int NIBSSNIPServiceSendTimeoutInMinutes { get; set; }
    public int NIBSSNIPServiceMaxBufferPoolSize { get; set; }
    public int NIBSSNIPServiceMaxReceivedMessageSize { get; set; }
    public string NIBSSPublicKeyPath { get; set; }
    public string NIBSSPrivateKeyPath { get; set; }
    public string NIBSSPrivateKeyPassword { get; set; }
}



public class ImalProperties 
{
    public ImalTransactionServiceProperties ImalTransactionServiceProperties { get; set; }
    public ImalInquiryServiceProperties ImalInquiryServiceProperties { get; set; }
}

public class ImalTransactionServiceProperties
{
    public string BaseUrl { get; set; }
    public string TransferRequest { get; set; }
    public string CurrencyCode { get; set; }
    public int PrincipalTransactionType { get; set; }
    public int FeeTransactionType { get; set; }
    public int VatTransactionType { get; set; }
    public string PrincipalTssAccount { get; set; }
    public string VatTssAccount { get; set; }
    public Dictionary<string,string> FeeTssAccounts { get; set; }
    public string FeeDefaultTssAccount { get; set; }
    public int TimeoutInMinutes { get; set; }
}

public class ImalInquiryServiceProperties
{
    public string BaseUrl { get; set; }
    public string GetAccountDetailsByNubanRequest { get; set; }
    public string GetAccountSuccessMessage { get; set; }
    public int TimeoutInMinutes { get; set; }
}