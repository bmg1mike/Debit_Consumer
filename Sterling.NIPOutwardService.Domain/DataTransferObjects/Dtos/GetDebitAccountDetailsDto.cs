using Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.VTeller;

namespace Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos;

public class GetDebitAccountDetailsDto 
{
    public DebitAccountDetails DebitAccountDetails { get; set; }
    public CreateVTellerTransactionDto CreateVTellerTransactionDto { get; set; }
    public int CustomerClass { get; set; }
}