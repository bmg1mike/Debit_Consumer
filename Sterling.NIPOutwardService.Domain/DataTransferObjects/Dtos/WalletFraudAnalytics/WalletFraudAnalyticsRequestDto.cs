namespace Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.WalletFraudAnalytics;

public class WalletFraudAnalyticsRequestDto
{
    public int AppId { get; set; }
    public string ReferenceId { get; set; }
    public string FromAccount { get; set; }
    public string ToAccount { get; set; }
    public string SenderName { get; set; }
    public string BeneficiaryName { get; set; }
    public float Amount { get; set; }
    public string BeneficiaryBankCode { get; set; }
    public string TransTimestamp { get; set; }
    public int IsWalletOnly { get; set; } //0 - false, 1 - true
    public int TransactionType { get; set; }

}