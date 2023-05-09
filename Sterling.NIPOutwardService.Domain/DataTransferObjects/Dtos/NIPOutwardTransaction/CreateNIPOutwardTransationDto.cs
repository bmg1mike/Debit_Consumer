using System.ComponentModel;
using System.Runtime.Serialization;
using Sterling.NIPOutwardService.Domain.Entities;

namespace Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos;


[AutoMap(typeof(NIPOutwardTransaction))]
public class CreateNIPOutwardTransactionDto 
{
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
    public string NameEnquiryResponse { get; set; }
    public string DebitAccountNumber { get; set; }
    public string BeneficiaryBankCode { get; set; }
    public string OriginatorBVN { get; set; }
    public string BeneficiaryBVN { get; set; }
    public string BeneficiaryKYCLevel { get; set; }
    public string OriginatorKYCLevel { get; set; }
    public string TransactionLocation { get; set; }
    public int AppId { get; set; }
    public int PriorityLevel { get; set; }
    public bool IsWalletTransaction { get; set; }
    public bool IsImalTransaction { get; set; }
    
}
