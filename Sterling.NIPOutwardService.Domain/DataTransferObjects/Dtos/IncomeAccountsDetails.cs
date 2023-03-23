namespace Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos;

public class IncomeAccountsDetails
{
    public string ExpCode { get; set; }
    public string Tss { get; set; }
    public string Tss2 { get; set; }
    public Fee Fee { get; set; }
}

public class Fee
{
    public string bra_code { get; set; }
    public string cusnum { get; set; }
    public string curcode { get; set; }
    public string ledcode { get; set; }
    public string subacctcode { get; set; }
    
}