namespace Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.FraudAnalytics;

public class FraudAnalyticsRequest
{
    public string AppId { get; set; }
    public string ReferenceId { get; set; }
    public string RequestTypeId { get; set; }
    public string FromAccount { get; set; }
    public string ToAccount { get; set; }
    public string Amount { get; set; }
    public int AccountCategory { get; set; }
    public string DestinationBankCode { get; set; }
    public string FromAccountName { get; set; }
    public string BeneficiaryName { get; set; }
    public string RequestXML { get; set; }
    public string TransTimestamp { get; set; }
    public string Email { get; set; }
}