namespace Sterling.NIPOutwardService.Domain.Entities;

public class DebitAccountDetails
{
    public decimal UsableBalance { get; set; }
    public string T24_LED_CODE { get; set; }
    public string Email  { get; set; }
    public int CustomerStatusCode { get; set; }
    public string T24_BRA_CODE { get; set; }
}