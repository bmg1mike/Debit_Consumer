namespace Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.ImalTransaction;

public class ImalTransactionResponseDto 
{
    public string statusCode { get; set; }
    public string statusDesc { get; set; }
    public string transactionNumber { get; set; }
    public string transactionType { get; set; }
}
// public class ImalTransactionResponseDto 
// {
//     public string responseCode { get; set; }
//     public string errorCode { get; set; }
//     public string skipProcessing { get; set; }
//     public string originalResponseCode { get; set; }
//     public string skipLog { get; set; }
// }