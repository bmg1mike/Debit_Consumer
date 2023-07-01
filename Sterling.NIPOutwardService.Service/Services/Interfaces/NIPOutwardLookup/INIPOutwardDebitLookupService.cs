namespace Sterling.NIPOutwardService.Service.Services.Interfaces.NIPOutwardLookup;

public interface INIPOutwardDebitLookupService 
{
    Task<FundsTransferResult<NIPOutwardDebitLookup>> FindOrCreate(long ID);
    OutboundLog GetOutboundLog();
    Task<int> Delete(NIPOutwardDebitLookup nipOutwardDebitLookup);

}