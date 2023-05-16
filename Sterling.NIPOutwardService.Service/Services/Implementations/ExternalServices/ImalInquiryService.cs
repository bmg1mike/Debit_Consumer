using Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.ImalTransaction;

namespace Sterling.NIPOutwardService.Service.Services.Implementations.ExternalServices;


public class ImalInquiryService:IImalInquiryService
{
    private readonly HttpClient httpClient;
    private readonly AppSettings appSettings;
    private OutboundLog outboundLog;
    public ImalInquiryService(IOptions<AppSettings> appSettings, HttpClient httpClient)
    {
        this.appSettings = appSettings.Value;
        this.httpClient = httpClient;
        this.httpClient.BaseAddress = new Uri(this.appSettings.ImalProperties.ImalInquiryServiceProperties.BaseUrl);
        this.httpClient.Timeout = TimeSpan.FromMinutes(this.appSettings.ImalProperties.ImalInquiryServiceProperties.TimeoutInMinutes);
        this.outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
    }

    public async Task<ImalGetAccountDetailsResponse> GetAccountDetailsByNuban(string nuban)
    {
        try
        {
            outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
            outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.GetAccountDetailsByNuban)}";
            outboundLog.RequestDetails = nuban;

            var request = appSettings.ImalProperties.ImalInquiryServiceProperties.GetAccountDetailsByNubanRequest + nuban;

            var httpResponseMessage =
            await httpClient.GetAsync(request);

            var response = await httpResponseMessage.Content.ReadAsStringAsync();

            outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            outboundLog.ResponseDetails = response;

            return JsonConvert.DeserializeObject<ImalGetAccountDetailsResponse>(response);
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