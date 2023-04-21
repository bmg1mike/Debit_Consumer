namespace Sterling.NIPOutwardService.Domain.Entities;

public class NIPOutwardNameEnquiry
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public long ID { get; set; }
    public string ResponseCode { get; set; }
    public string SessionID { get; set; }
    public string AccountName { get; set; }
    public string BVN { get; set; }
    public string KYCLevel { get; set; }
    public string AccountNumber { get; set; }
    public string DestinationInstitutionCode { get; set; }
    public byte ChannelCode { get; set; }
    public DateTime DateAdded { get; set; }
}