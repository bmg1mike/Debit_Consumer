namespace Sterling.NIPOutwardService.Service.Services.Implementations.ExternalServices;

public class WalletFraudAnalyticsService:IWalletFraudAnalyticsService
{
    private readonly HttpClient httpClient;
    private readonly AppSettings appSettings;
    private OutboundLog outboundLog;
    public WalletFraudAnalyticsService(IOptions<AppSettings> appSettings, HttpClient httpClient)
    {
        this.appSettings = appSettings.Value;
        this.httpClient = httpClient;
        this.httpClient.BaseAddress = new Uri(this.appSettings.WalletFraudAnalyticsProperties.BaseUrl);
        this.httpClient.Timeout = TimeSpan.FromMinutes(this.appSettings.WalletFraudAnalyticsProperties.TimeoutInMinutes);
        this.outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
    }

    public async Task<WalletFraudAnalyticsResponseDto> GetFraudScore(WalletFraudAnalyticsRequestDto request)
    {
        var rawRequest = JsonConvert.SerializeObject(request);
        try
        {
            outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
            outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.GetFraudScore)}";
            outboundLog.RequestDetails = rawRequest;

            var requestPayload = new StringContent(
            rawRequest,
            Encoding.UTF8,
            Application.Json); // using static System.Net.Mime.MediaTypeNames;    
            httpClient.DefaultRequestHeaders.Clear(); 
            using var httpResponseMessage = await httpClient
            .PostAsync(appSettings.WalletFraudAnalyticsProperties.GetScoreRequest, requestPayload); 
            var response = httpResponseMessage.Content.ReadAsStringAsync().Result;
            outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            outboundLog.ResponseDetails = response;
            return JsonConvert.DeserializeObject<WalletFraudAnalyticsResponseDto>(response);
            
        }
        catch(Exception ex)
        {
            outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + 
            "\r\n" + $@"Exception Details: {ex.Message} {ex.StackTrace}";
            //logger.Error($"Exception occured during FraudAPIScore call. DateTime: {DateTime.Now}  Messg: {ex.Message} StackTrace: {ex.StackTrace} ");
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