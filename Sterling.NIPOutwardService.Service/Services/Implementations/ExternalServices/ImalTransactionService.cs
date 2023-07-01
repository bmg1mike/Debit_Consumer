namespace Sterling.NIPOutwardService.Service.Services.Implementations.ExternalServices;


public class ImalTransactionService:IImalTransactionService
{
    private readonly HttpClient httpClient;
    private readonly AppSettings appSettings;
    private OutboundLog outboundLog;
    public ImalTransactionService(IOptions<AppSettings> appSettings, HttpClient httpClient)
    {
        this.appSettings = appSettings.Value;
        this.httpClient = httpClient;
        this.httpClient.BaseAddress = new Uri(this.appSettings.ImalProperties.ImalTransactionServiceProperties.BaseUrl);
        this.httpClient.Timeout = TimeSpan.FromMinutes(this.appSettings.ImalProperties.ImalTransactionServiceProperties.TimeoutInMinutes);
        this.outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
    }

    public async Task<ImalTransactionResponseDto> NipFundsTransfer(ImalTransactionRequestDto request)
    {
        var rawRequest = JsonConvert.SerializeObject(request);
        try
        {
            outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
            outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.NipFundsTransfer)}";
            outboundLog.RequestDetails = rawRequest;

            var requestPayload = new StringContent(
            rawRequest,
            Encoding.UTF8,
            Application.Json); // using static System.Net.Mime.MediaTypeNames;    
            httpClient.DefaultRequestHeaders.Clear(); 
            using var httpResponseMessage = await httpClient
            .PostAsync(appSettings.ImalProperties.ImalTransactionServiceProperties.TransferRequest, requestPayload); 
            var response = httpResponseMessage.Content.ReadAsStringAsync().Result;
            outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            outboundLog.ResponseDetails = response;
            return JsonConvert.DeserializeObject<ImalTransactionResponseDto>(response);
            
        }
        catch(Exception ex)
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