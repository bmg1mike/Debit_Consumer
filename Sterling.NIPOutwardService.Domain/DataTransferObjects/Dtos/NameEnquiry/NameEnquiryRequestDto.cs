namespace Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.NameEnquiry;

public class NameEnquiryRequestDto 
{
    public string SessionID { get; set; }
    public string DestinationInstitutionCode { get; set; }
    public byte ChannelCode { get; set; }
    public string AccountNumber { get; set; }
}