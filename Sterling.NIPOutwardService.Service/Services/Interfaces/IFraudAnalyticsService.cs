namespace Sterling.NIPOutwardService.Service.Services.Interfaces;

public interface IFraudAnalyticsService {
    Task<FraudAnalyticsResponse> DoFraudAnalytics(string appId, string refId, string sessionId, 
    string reqType, string fromAccount, string toAccount, string amount, string fromAcctName, string destAcctName, 
    string destBankCode, string NEResponse, string paymentRef, string Email);
    OutboundLog GetOutboundLog();
}