using Sterling.NIPOutwardService.Data.Repositories.Implementations.NIPOutwardLookup;
using Sterling.NIPOutwardService.Data.Repositories.Interfaces.NIPOutwardLookup;
using Sterling.NIPOutwardService.Domain.Entities.NIPOutwardLookup;

namespace Sterling.NIPOutwardService.Service.Services.Implementations.NIPOutwardLookup;
public class BaseLookupService <TEntity>  
    where TEntity : IBaseLookupRepository<BaseLookup> 
{
    private readonly TEntity lookupRepository;
    private OutboundLog outboundLog;
    public BaseLookupService(TEntity lookupRepository)
    {
        this.lookupRepository = lookupRepository;
        this.outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
    }
    public async Task<FundsTransferResult<string>> FindOrCreate(long ID){
        // var checkIfRecordExistsResult = await nipOutwardDebitLookupRepository.FindByNIPOutwardTransactionID(ID);

        // if (checkIfRecordExistsResult)
        // {

        // }
        FundsTransferResult<string> result = new FundsTransferResult<string>();
        result.IsSuccess = false;
        try
        {
            var nipOutwardDebitLookup = new BaseLookup {
                NIPOutwardTransactionID = ID,
                StatusFlag  = 3,
                DateProcessed = DateTime.UtcNow.AddHours(1),
            };

            await lookupRepository.Create(nipOutwardDebitLookup);

            result.IsSuccess = true;
        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            result.Message = "Internal Server Error";
            outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + 
            "\r\n" + $@"Raw Request {ID} Exception Details: {ex.Message} {ex.StackTrace}";
        }

        return result;
    }

    public OutboundLog GetOutboundLog()
    {
        var recordToBeMoved = this.outboundLog;
        this.outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString() };
        return recordToBeMoved;
    }
}