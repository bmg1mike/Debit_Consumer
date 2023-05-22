namespace Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.ImalTransaction;

public class ImalTransactionRequestDto 
{
    public string FromAccount { get; set; }
    public string ToAccount { get; set; }
    public int TransactionType { get; set; }
    public int DifferentTradeValueDate { get; set; }
    public decimal TransactionAmount { get; set; }
    public string CurrencyCode { get; set; }
    public string PaymentReference { get; set; }
    public string NarrationLine1 { get; set; }
    public string NarrationLine2 { get; set; }
    public string BeneficiaryName { get; set; }
    public string SenderName { get; set; }
    public string ValueDate { get; set; }
}
// public class ImalTransactionRequestDto
// {
//     public string destinationBankCode { get; set; }
//     public string channelCode { get; set; }
//     public string customerShowName { get; set; }
//     public string paymentReference { get; set; }
//     public string fromAccount { get; set; }
//     public string toAccount { get; set; }
//     public string amount { get; set; }
//     public string requestCode { get; set; }
//     public string principalIdentifier { get; set; }
//     public string referenceCode { get; set; }
//     public string beneficiaryName { get; set; }
//     public string nesid { get; set; }
//     public string nersp { get; set; }
//     public string beneBVN { get; set; }
//     public string beneKycLevel { get; set; }
// }


