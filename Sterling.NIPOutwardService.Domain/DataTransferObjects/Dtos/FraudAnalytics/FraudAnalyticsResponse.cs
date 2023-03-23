namespace Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.FraudAnalytics;

public class FraudAnalyticsResponse 
{
    public int appId { get; set; }
    public string referenceId { get; set; }
    public string fraudScore { get; set; }
    public string responseCode { get; set; }
    public string errorMessage { get; set; }
}