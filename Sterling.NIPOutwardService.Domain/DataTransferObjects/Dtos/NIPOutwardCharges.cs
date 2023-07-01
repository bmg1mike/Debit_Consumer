namespace Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos;

public class NIPOutwardCharges 
{
    public decimal NIPFeeAmount { get; set; } 
    public decimal NIPVatAmount { get; set; } 
    public bool ChargesFound { get; set; } 
}