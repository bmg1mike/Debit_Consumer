namespace Sterling.NIPOutwardService.Service.Services.Implementations.ExternalServices;

public class WalletTransactionService:IWalletTransactionService
{
    private readonly HttpClient httpClient;
    private readonly AppSettings appSettings;
    private OutboundLog outboundLog;
    private readonly IEncryption encryption;
    public WalletTransactionService(IOptions<AppSettings> appSettings, HttpClient httpClient, IEncryption encryption)
    {
        this.appSettings = appSettings.Value;
        this.encryption = encryption;
        this.httpClient = httpClient;
        this.httpClient.BaseAddress = new Uri(this.appSettings.WalletTransactionServiceProperties.BaseUrl);
        this.httpClient.Timeout = TimeSpan.FromMinutes(this.appSettings.WalletTransactionServiceProperties.TimeoutInMinutes);
        this.outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
    }

    public async Task<WalletToWalletResponseDto> WalletToWalletTransfer(WalletToWalletRequestDto request)
    {
        var rawRequest = JsonConvert.SerializeObject(request);
        try
        {
            outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
            outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.WalletToWalletTransfer)}";
            outboundLog.RequestDetails = "plaintext request:" + rawRequest;

            string encryptPayload = encryption.EncryptAes(rawRequest, 
            appSettings.WalletTransactionServiceProperties.SecretKey, appSettings.WalletTransactionServiceProperties.IV);

            var createWalletReq = new
            {
                data = encryptPayload
            };
            var encryptedRequest = JsonConvert.SerializeObject(createWalletReq);

            var requestPayload = new StringContent(
            encryptedRequest,
            Encoding.UTF8,
            Application.Json); // using static System.Net.Mime.MediaTypeNames;    
            httpClient.DefaultRequestHeaders.Clear(); 
            using var httpResponseMessage = await httpClient
            .PostAsync(appSettings.WalletTransactionServiceProperties.TransferRequest, requestPayload); 
            var response = httpResponseMessage.Content.ReadAsStringAsync().Result;
            outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            outboundLog.ResponseDetails = response;

            response = response.Replace("\"", "");

            var decryptedPayload = encryption.DecryptAes(response, 
            appSettings.WalletTransactionServiceProperties.SecretKey, appSettings.WalletTransactionServiceProperties.IV);

            outboundLog.ResponseDetails = outboundLog.ResponseDetails + $" Decrypted payload: {decryptedPayload}";
            
            return JsonConvert.DeserializeObject<WalletToWalletResponseDto>(decryptedPayload);

        }
        catch (Exception ex)
        {
            outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + 
            "\r\n" + $@"Exception Details: {ex.Message} {ex.StackTrace}";
            return null;
        }
    }

    public OutboundLog GetOutboundLog()
    {
        var recordToBeMoved = this.outboundLog;
        this.outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
        return recordToBeMoved;
    }

}