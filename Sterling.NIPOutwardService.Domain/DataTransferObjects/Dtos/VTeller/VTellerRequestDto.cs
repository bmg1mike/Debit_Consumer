namespace Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.VTeller;

public class VtellerRequestDto
{
    public string Xml { get; set; }
    public string TransactionType { get; set; }
    public string SessionId { get; set; }
}