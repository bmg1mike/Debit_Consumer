using System.Net;
using Newtonsoft.Json;
using Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.FraudAnalytics;
using static System.Net.Mime.MediaTypeNames;

namespace Sterling.NIPOutwardService.Service.Services.Implementations;

public class FraudAnalyticsService : IFraudAnalyticsService 
{
    private readonly HttpClient httpClient;
    private readonly AppSettings appSettings;
    private OutboundLog outboundLog;


    public FraudAnalyticsService(IOptions<AppSettings> appSettings, HttpClient httpClient)
    {
        this.appSettings = appSettings.Value;
        this.httpClient = httpClient;
        //this.httpClient.BaseAddress = new Uri(this.appSettings.FraudBaseUrl);
        this.httpClient.Timeout = TimeSpan.FromMinutes(1);
        this.outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
    }
    // public async Task<FraudAnalyticsResponse> DoFraudAnalytics(string appId, string refId, string sessionId, 
    // string reqType, string fromAccount, string toAccount, string amount, string fromAcctName, string destAcctName, 
    // string destBankCode, string NEResponse, string paymentRef, string Email)
    // {
    //     FraudAnalyticsResponse fraudResponse = new FraudAnalyticsResponse();
    //     try
    //     {
    //         StringBuilder sb = new StringBuilder();
    //         sb.Append("<?xml version='1.0' encoding='utf - 8'?>");
    //         sb.Append("<IBSRequest>");
    //         sb.Append("<ReferenceID>" + refId + "</ReferenceID>");
    //         sb.Append("<RequestType>" + reqType + "</RequestType>");
    //         sb.Append("<SessionID>" + sessionId + "</SessionID>");
    //         sb.Append("<FromAccount>" + fromAccount + "</FromAccount>");
    //         sb.Append("<ToAccount>" + toAccount + "</ToAccount>");
    //         sb.Append("<Amount>" + amount + "</Amount>");
    //         sb.Append("<DestinationBankCode>" + destBankCode + "</DestinationBankCode>");
    //         sb.Append("<NEResponse>" + NEResponse + "</NEResponse>");
    //         sb.Append("<BenefiName>" + fromAcctName + "</BenefiName>");
    //         sb.Append("<PaymentReference>" + paymentRef + "</PaymentReference>");
    //         sb.Append("</IBSRequest>");
    //         var requestString = sb.ToString();
    //         var request = new FraudAnalyticsRequest
    //         {
    //             AppId = appId.ToString(),
    //             ReferenceId = sessionId,
    //             RequestTypeId = reqType,
    //             FromAccount = fromAccount,
    //             ToAccount = toAccount,
    //             Amount = amount,
    //             AccountCategory = 3013,
    //             DestinationBankCode = destBankCode,
    //             FromAccountName = fromAcctName,
    //             BeneficiaryName = destAcctName,
    //             RequestXML = requestString,
    //             TransTimestamp = DateTime.Now.ToString(),
    //             Email = Email
    //         };

    //         if (appSettings.ProxySwitch.ToUpper() == "ON")
    //         {
    //             WebRequest.DefaultWebProxy = new WebProxy(appSettings.ProxyHost, appSettings.ProxyPort)
    //             {
    //                 Credentials = new NetworkCredential(appSettings.ProxyUsername, appSettings.ProxyPassword)
    //             };
    //         }
    //         else
    //         {
    //             ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
    //             ServicePointManager.ServerCertificateValidationCallback +=
    //             (sender, cert, chain, sslPolicyErrors) => { return true; };
    //         }
    //         var rawRequest = JsonConvert.SerializeObject(request);

    //             outboundLog.APICalled = appSettings.FraudBaseUrl + appSettings.FraudAnalyticsRequest;
    //             outboundLog.APIMethod = appSettings.FraudBaseUrl + appSettings.FraudAnalyticsRequest;
    //             outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
    //             outboundLog.RequestDetails = rawRequest;
                
    //         var requestPayload = new StringContent(
    //         rawRequest,
    //         Encoding.UTF8,
    //         Application.Json); // using static System.Net.Mime.MediaTypeNames;                 
    //         using var httpResponseMessage = await httpClient.PostAsync(appSettings.FraudAnalyticsRequest, requestPayload); 
    //         var response = httpResponseMessage.Content.ReadAsStringAsync().Result;
    //         outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
    //         outboundLog.RequestDetails = response;
    //         return JsonConvert.DeserializeObject<FraudAnalyticsResponse>(response);

    //     }
    //     catch (Exception ex) 
    //     { 
    //         var rawRequest = $@"appId: {appId}, refId: {refId}, sessionId: {sessionId}, reqType: {reqType}, 
    //         fromAccount: {fromAccount}, toAccount: {toAccount}, amount: {amount}, fromAcctName: {fromAcctName}, 
    //         destAcctName: {destAcctName}, destBankCode: {destBankCode}, NEResponse: {NEResponse}, paymentRef: {paymentRef}, 
    //         Email: {Email}";
    //         outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + 
    //         "\r\n" + $@"Raw Request {rawRequest} Exception Details: {ex.Message} {ex.StackTrace}";
    //     }
    //     return fraudResponse;

    // }

    public OutboundLog GetOutboundLog()
    {
        var recordToBeMoved = this.outboundLog;
        this.outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
        return recordToBeMoved;
    }
}