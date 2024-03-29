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
        outboundLog.RequestDateTime = DateTime.UtcNow.AddHours(1);
        outboundLog.APIMethod = $"{this.ToString()}.{nameof(this.FindOrCreate)}";
        outboundLog.RequestDetails = $"NIP outward transaction ID: {ID}";

        FundsTransferResult<NIPOutwardDebitLookup> result = new FundsTransferResult<NIPOutwardDebitLookup>();
        result.IsSuccess = false;
        try
        {
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

            result.Content = nipOutwardDebitLookup;
            
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)  when (ex.InnerException.Message.Contains("duplicate key"))
        {
            result.IsSuccess = false;
            result.Message = "Transaction failed. Duplicate request.";
            outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + 
            "\r\n" + $@"NIP Outward Transaction ID: {ID} Exception Details: {ex.InnerException.Message} {ex.StackTrace}";
            
        }
        catch (System.Exception ex)
        {
            result.IsSuccess = false;
            result.Message = "Transaction failed";
            result.ErrorMessage = "Internal Server Error";
            outboundLog.ExceptionDetails = outboundLog.ExceptionDetails + 
            "\r\n" + $@"Raw Request {ID} Exception Details: {ex.Message} {ex.StackTrace}";
        }
        outboundLog.ResponseDetails = $"Successfully added record to look up table: {result.IsSuccess}";
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