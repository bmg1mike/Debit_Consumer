namespace Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.WalletFraudAnalytics;

public class WalletFraudAnalyticsResponseDto
{
    public int AppId { get; set; }
    public string ReferenceId { get; set; }
    public string FraudScore { get; set; }
    public string ResponseCode { get; set; }
    public string ErrorMessage { get; set; }
}