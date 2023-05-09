namespace Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.ImalTransaction;

public class ImalTransactionRequestDto
{
    public string destinationBankCode { get; set; }
    public string channelCode { get; set; }
    public string customerShowName { get; set; }
    public string paymentReference { get; set; }
    public string fromAccount { get; set; }
    public string toAccount { get; set; }
    public string amount { get; set; }
    public string requestCode { get; set; }
    public string principalIdentifier { get; set; }
    public string referenceCode { get; set; }
    public string beneficiaryName { get; set; }
    public string nesid { get; set; }
    public string nersp { get; set; }
    public string beneBVN { get; set; }
    public string beneKycLevel { get; set; }
}
