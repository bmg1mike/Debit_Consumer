using System.Reflection;
using Polly;
using Polly.Retry;

namespace Sterling.NIPOutwardService.Service.Services.Implementations.NIPOutwardLookup;

public class NIPOutwardDebitLookupService:INIPOutwardDebitLookupService
{
    private OutboundLog outboundLog;
    private readonly INIPOutwardDebitLookupRepository nipOutwardLookupRepository;
    private readonly AsyncRetryPolicy retryPolicy;
    
    public NIPOutwardDebitLookupService(INIPOutwardDebitLookupRepository nipOutwardLookupRepository)
    {
        this.nipOutwardLookupRepository = nipOutwardLookupRepository;
        this.outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString()};
         this.retryPolicy = Policy.Handle<Exception>()
    .WaitAndRetryAsync(new[]
    {
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4)
    }, (exception, timeSpan, retryCount, context) =>
    {
        outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + "\r\n" + @$"Retrying due to {exception.GetType().Name}... Attempt {retryCount}
            Exception Details: {exception.Message} {exception.StackTrace} " ;
    });
    }
    public async Task<FundsTransferResult<NIPOutwardDebitLookup>> FindOrCreate(long ID)
    {
        // var checkIfRecordExistsResult = await nipOutwardDebitLookupRepository.FindByNIPOutwardTransactionID(ID);

        // if (checkIfRecordExistsResult)
        // {

        // }
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.FindOrCreate)}";
        outboundLog.RequestDetails = $"{ID}";

        FundsTransferResult<NIPOutwardDebitLookup> result = new FundsTransferResult<NIPOutwardDebitLookup>();
        result.IsSuccess = false;
        try
        {
            // var checkIfRecordExistsResult = await nipOutwardLookupRepository.FindByNIPOutwardTransactionID(ID);

            // if (checkIfRecordExistsResult)
            // {
            //     result.IsSuccess = false;
            //     result.Message = "Transaction failed. Duplicate Record.";
            //     outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + $"Duplicate record in look up table. ID: {ID}";
            //     outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
            //     return result;
            // }

            var nipOutwardDebitLookup = new NIPOutwardDebitLookup {
                NIPOutwardTransactionID = ID,
                StatusFlag  = 3,
                DateProcessed = DateTime.UtcNow.AddHours(1),
            };

            var recordCreated = await nipOutwardLookupRepository.Create(nipOutwardDebitLookup);

            if (recordCreated == 1)
            {
                result.IsSuccess = true;
            }
            else
            {
                result.IsSuccess = false;
                result.Message = "Could not process transaction";
            }

            
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)  when (ex.InnerException.Message.Contains("duplicate key"))
        {
            result.IsSuccess = false;
            result.Message = "Transaction failed. Duplicate request.";
            outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + 
            "\r\n" + $@"NIP Outward Transaction ID: {ID} Exception Details: {ex.Message} {ex.StackTrace}";
            
        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            result.Message = "Internal Server Error";
            outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + 
            "\r\n" + $@"Raw Request {ID} Exception Details: {ex.Message} {ex.StackTrace}";
        }
        outboundLog.ResponseDetails = result.Message;
        outboundLog.ResponseDateTime = DateTime.UtcNow.AddHours(1);
        return result;
    }

    public async Task<int> Delete(NIPOutwardDebitLookup nipOutwardDebitLookup)
    {
        var recordsDeleted = 0;
        await retryPolicy.ExecuteAsync(async () =>
        {
            recordsDeleted = await nipOutwardLookupRepository.Delete(nipOutwardDebitLookup);
        });

        return recordsDeleted;
    }

    public OutboundLog GetOutboundLog()
    {
        var recordToBeMoved = this.outboundLog;
        this.outboundLog = new OutboundLog { OutboundLogId = ObjectId.GenerateNewId().ToString()};
        return recordToBeMoved;
    }

}