namespace Sterling.NIPOutwardService.Service.Services.Implementations;

public partial class InboundLogService : IInboundLogService
{
    private readonly IInboundLogRepository inboundLogRepository;
    private readonly ILogger logger;


    public InboundLogService(IInboundLogRepository inboundLogRepository, ILogger logger)
    {
        this.inboundLogRepository = inboundLogRepository;
        this.logger = logger;

    }

    public async Task<FundsTransferResult<string>> CreateInboundLog(InboundLog inboundLog)
    {
        FundsTransferResult<string> result = new FundsTransferResult<string>();
        result.IsSuccess = false;
        try
        {
           var response = await inboundLogRepository.CreateInboundLog(inboundLog);
           result.Content = response;  
           if (response == "")
           {
             result.IsSuccess = false;
             result.ErrorMessage = "InboundLog not created";
             result.Message = "InboundLog not created";
           }
           else
           {
             result.IsSuccess = true;
             result.ErrorMessage = "";
             result.Message = $"InboundLog with Id {response} Created Successfully !";
           }
        }
        catch(Exception ex)
        {   
            var rawLog = JsonConvert.SerializeObject(inboundLog);       
            logger.Error(ex,$"Error while creating InboundLog. Raw Log: {rawLog}");
            result.ErrorMessage = ex.ToString();
            result.Message = "Error while creating InboundLog";
            result.IsSuccess = false;
        }
        return result;
    }

    public async Task<FundsTransferResult<List<InboundLog>>> GetInboundLogs()
    {
        FundsTransferResult<List<InboundLog>> result = new FundsTransferResult<List<InboundLog>>();
        result.IsSuccess = false;
        try
        {
          var response = await inboundLogRepository.GetInboundLogs();
          result.Content = response.ToList();
          result.IsSuccess = true;
          result.Message = "Retrieved Successfully.";
          result.ErrorMessage = "";
        }
        catch(Exception ex)
        {
            logger.Error(ex,"Error while retrieving InboundLogs");
            result.ErrorMessage = ex.ToString();
            result.Message = "Error while retrieving InboundLogs";
            result.IsSuccess = false;
        }
        return result;
    }

    //GetList by any Field Name template. Uncomment If Needed. Remember to add to your IInboundLogService.cs
    /* public async Task<Result<List<InboundLog>>> GetByFieldName(string fieldName)
    {
        Result<List<InboundLog>> result = new Result<List<InboundLog>>();
        result.IsSuccess = false;
        try
        {
         var response = await inboundLogRepository.GetByFieldNameInboundLogs();
         result.Content = response.ToList();
         result.IsSuccess = true;
         result.ErrorMessage = "";
         result.Message = "Retrieved Successfully.";
        }
        catch(Exception ex)
        {
            logger.Error(ex,"Error while retrieving InboundLogs");
            result.ErrorMessage = ex.ToString();
            result.Message = "Error while retrieving InboundLogs";
            result.IsSuccess = false;
        }
    } */

    public async Task<FundsTransferResult<InboundLog>> GetInboundLog(string inboundLogId)
    {
        FundsTransferResult<InboundLog> result = new FundsTransferResult<InboundLog>();
        result.IsSuccess = false;
        try
        {
            var response = await inboundLogRepository.GetInboundLog(inboundLogId);
            result.Content = response;
            result.IsSuccess = true;
            result.Message = "Retrieved Successfully.";
            result.ErrorMessage = "";
        }
        catch(Exception ex)
        {
            logger.Error(ex,"Error while retrieving InboundLog");
            result.ErrorMessage = ex.ToString();
            result.Message = "Error while retrieving InboundLog";
            result.IsSuccess = false;
        }
        return result;
    }

    public async Task<FundsTransferResult<bool>> RemoveInboundLog(string inboundLogId)
    {
        FundsTransferResult<bool> result = new FundsTransferResult<bool>();
        result.IsSuccess = false;

        try
        {
            var response = await inboundLogRepository.RemoveInboundLog(inboundLogId);
            result.Content = response;

           if (!response)
           {
             result.IsSuccess = false;
             result.ErrorMessage = "InboundLog not deleted";
             result.Message = "InboundLog with Id {inboundLogId} not deleted";
           }
           else
           {
             result.IsSuccess = true;
             result.ErrorMessage = "";
             result.Message = $"InboundLog with Id {inboundLogId} deleted Successfully !";
           }
        }
        catch(Exception ex)
        {
            logger.Error(ex,"Error while removing InboundLog");
            result.ErrorMessage = ex.ToString();
            result.Message = "Error while removing InboundLog";
            result.IsSuccess = false;
        }
        return result;
    }

    public async Task<FundsTransferResult<bool>> UpdateInboundLog(string inboundLogId, InboundLog inboundLog)
    {
        FundsTransferResult<bool> result = new FundsTransferResult<bool>();
        result.IsSuccess = false;
        try
        {
          var existingInboundLog = await inboundLogRepository.GetInboundLog(inboundLogId);
      
          if (existingInboundLog == null)
           {
             result.IsSuccess = false;
             result.ErrorMessage = "InboundLog not updated";
             result.Message = "InboundLog with Id {inboundLogId} not updated";
           }

          var response = await inboundLogRepository.UpdateInboundLog(inboundLogId,inboundLog); 
          if (!response)
          {
             result.IsSuccess = false;
             result.ErrorMessage = "InboundLog not updated";
             result.Message = "InboundLog with Id {inboundLogId} not updated";
          }
          else
          {
             result.IsSuccess = true;
             result.ErrorMessage = "";
             result.Message = "InboundLog with Id {inboundLogId} updated Successfully.";

          }
          
          result.Content = response;
   
        }
        catch(Exception ex)
        {
            logger.Error(ex,"Error while Updating InboundLog");
            result.ErrorMessage = ex.ToString();
            result.Message = "Error while Updating InboundLog";
            result.IsSuccess = false;
        }   
        return result;
    }

    //Update Specific Fields Template. Uncomment If Needed. Remember to add to your IInboundLogService.cs

    /* public async Task<Result<bool>> UpdateSpecificFields(string inboundLogId, InboundLog inboundLog)
    {
        Result<bool> result = new Result<bool>();
        result.IsSuccess = false;
        try
        {
            var filter = Builders<InboundLog>.Filter.Eq(m => m.InboundLogId, inboundLogId);
            var update = Builders<InboundLog>.Update
            .Set(m => m.Field1, inboundLog.Field1)
            .Set(m => m.Field2, inboundLog.Field2)
            .Set(m => m.Field3, inboundLog.Field3);
        
            var response = await context.InboundLogs.UpdateOneAsync(filter, update);
            result.Content = response.ModifiedCount == 1;
            result.IsSuccess = true;
            result.ErrorMessage = "";
        }
        catch(Exception ex)
        {
            logger.Error(ex,"Error while updating InboundLog");
            result.ErrorMessage = ex.ToString();
            result.Message = "Error while Updating InboundLog";
            result.IsSuccess = false;
        }
        return result;
    } */
}



