namespace Sterling.NIPOutwardService.Service.Services.Interfaces.ExternalServices;

public interface IWalletFraudAnalyticsService 
{
    Task<WalletFraudAnalyticsResponseDto> GetFraudScore(WalletFraudAnalyticsRequestDto request);
    OutboundLog GetOutboundLog();
}