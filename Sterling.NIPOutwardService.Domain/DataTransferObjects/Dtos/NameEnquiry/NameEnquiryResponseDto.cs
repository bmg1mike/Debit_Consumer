namespace Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.NameEnquiry;

public class NameEnquiryResponseDto 
{
    public string SessionID { get; set; }
    public string DestinationInstitutionCode { get; set; }
    public byte ChannelCode { get; set; }
    public string AccountNumber { get; set; }
    public string AccountName { get; set; }
    public string BankVerificationNumber { get; set; }
    public string KYCLevel { get; set; }
    public string ResponseCode { get; set; }
}