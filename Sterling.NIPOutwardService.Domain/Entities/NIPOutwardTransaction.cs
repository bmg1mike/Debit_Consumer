namespace Sterling.NIPOutwardService.Domain.Entities;

public class NIPOutwardTransaction 
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public int ID { get; set; }
    public string SessionID { get; set; }
    public string NameEnquirySessionID { get; set; }
    public string TransactionCode { get; set; }
    public byte ChannelCode { get; set; }
    public string PaymentReference { get; set; }
    public decimal Amount { get; set; }
    public string CreditAccountName { get; set; }
    public string CreditAccountNumber { get; set; }
    public string OriginatorName { get; set; }
    public string BranchCode { get; set; }
    public string CustomerID { get; set; }
    public string CurrencyCode { get; set; }
    public string LedgerCode { get; set; }
    public string SubAccountCode { get; set; }
    public string FundsTransferResponse { get; set; }
    public DateTime? DateAdded { get; set; }
    public string DebitRequeryStatus { get; set; }
    public string NIBSSRequeryStatus { get; set; }
    public string NameResponse { get; set; }
    public DateTime? LastUpdate { get; set; }
    public string ReversalStatus { get; set; }
    public byte DebitResponse { get; set; }
    public int FTAdvice { get; set; }
    public DateTime? FTAdviceDate { get; set; }
    public string DebitAccountNumber { get; set; }
    public string BeneficiaryBankCode { get; set; }
    public string PrincipalResponse { get; set; }
    public string FeeResponse { get; set; }
    public string VatResponse { get; set; }
    public string AccountStatus { get; set; }
    public string Restriction { get; set; }
    public int StatusFlag { get; set; }
    public string FraudResponse { get; set; }
    public string FraudScore { get; set; }
    public string OriginatorBVN { get; set; }
    public string OriginatorEmail { get; set; }
    public string BeneficiaryBVN { get; set; }
    public string BeneficiaryKYCLevel { get; set; }
    public string OriginatorKYCLevel { get; set; }
    public string TransLocation { get; set; }
    public int AppId { get; set; }
    public DateTime? VtellerRequestTime { get; set; }
    public DateTime? VtellerResponseTime { get; set; }
    public string KafkaStatus { get; set; }
    public int PriorityLevel { get; set; }
    public string NIBSSResponse { get; set; }
    
}

