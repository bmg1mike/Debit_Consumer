namespace Sterling.NIPOutwardService.Domain.Entities;

public class NIPOutwardWalletTransaction 
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public long ID { get; set; }
    public string DebitAccountNumber { get; set; }
    public decimal Amount { get; set; }
    public string CreditAccountNumber { get; set; }
    public string CreditAccountName { get; set; }
    public string BeneficiaryBankCode { get; set; }
    public string PaymentReference { get; set; }
    public string SessionID { get; set; }
    public string ResponseCode { get; set; }
    public string ResponseMessage { get; set; }
    public string FraudResponseCode { get; set; }
    public string FraudResponseMessage { get; set; }
    public string RequeryResponseCode { get; set; }
    public string RequeryResponseMessage { get; set; }
    public int AppId { get; set; }
    public DateTime? DateAdded { get; set; }
    public DateTime? LastUpdate { get; set; }
    public string CurrencyCode { get; set; }
    public string OriginatorName { get; set; }
    public int StatusFlag { get; set; }
    public string NameEnquirySessionID { get; set; }
    public string BeneficiaryKYCLevel { get; set; }
    public string BeneficiaryBVN { get; set; }
}