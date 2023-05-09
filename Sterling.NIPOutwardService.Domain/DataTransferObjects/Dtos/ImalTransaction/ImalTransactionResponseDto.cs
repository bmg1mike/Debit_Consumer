namespace Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.ImalTransaction;

public class ImalTransactionResponseDto 
{
    public string responseCode { get; set; }
    public string errorCode { get; set; }
    public string skipProcessing { get; set; }
    public string originalResponseCode { get; set; }
    public string skipLog { get; set; }
}