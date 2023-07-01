namespace Sterling.NIPOutwardService.API.Controllers.v1;

[Authorize(AuthenticationSchemes = "Bearer")]
public partial class InboundLogsController : BaseController
{
    private readonly IInboundLogService inboundLogService;

    public InboundLogsController(IInboundLogService inboundLogService)
    {
        this.inboundLogService = inboundLogService;
    }

    [HttpPost]
    [Route("GetInboundLogs/")]
    public async Task<ActionResult> GetInboundLogs()
    {
        var result = new FundsTransferResult<List<InboundLog>>();
        result.RequestTime = DateTime.UtcNow;

        var response = await inboundLogService.GetInboundLogs();
        result = response;
        result.ResponseTime = DateTime.UtcNow;
        return Ok(result);
    }

    [HttpPost]
    [Route("GetInboundLog/")]
    public async Task<ActionResult> GetInboundLog(string id)
    {
        var result = new FundsTransferResult<InboundLog>();
        result.RequestTime = DateTime.UtcNow;

       /*if (!ModelState.IsValid)
        {
            result.Error =  PopulateError(500,$"invalid InboundLog model","Invalid Model State");
            return BadRequest(result);
        }*/

        var response =  await inboundLogService.GetInboundLog(id);
        result = response;
        result.ResponseTime = DateTime.UtcNow;
        return Ok(result);
    }

    // [HttpPost]
    // [Route("CreateInboundLog/")]
    // public async Task<ActionResult> CreateInboundLog([FromBody] InboundLog inboundLog)
    // {
    //     var result = new Result<string>();
    //     result.RequestTime = DateTime.UtcNow;

    //     var response =  await inboundLogService.CreateInboundLog(inboundLog);
    //     result = response;
    //     result.ResponseTime = DateTime.UtcNow;
    //     return Ok(result);
    // }

    // [HttpPost]
    // [Route("UpdateInboundLog/")]
    // public async Task<ActionResult>UpdateInboundLog(string id, [FromBody] InboundLog inboundLog)
    // {
    //     var result = new Result<bool>();
    //     result.RequestTime = DateTime.UtcNow;

    //     /*if (!ModelState.IsValid)
    //     {
    //         result.Error =  PopulateError(500,$"invalid InboundLog model","Invalid Model State");
    //         return BadRequest(result);
    //     } */
        
    //     var response = await inboundLogService.UpdateInboundLog(id, inboundLog);
    //     result = response;
    //     result.ResponseTime = DateTime.UtcNow;
    //     return Ok(result);
    // }

    // [HttpPost]
    // [Route("RemoveInboundLog")]
    // public async Task<ActionResult>RemoveInboundLog(string id)
    // {
    //     var result = new Result<bool>();
    //     result.RequestTime = DateTime.UtcNow;

    //    /* if (inboundLog == null)
    //     {
    //         result.Error =  PopulateError(400,$"InboundLog with Id = {id} not found","Not Found");
    //         return BadRequest(result);

    //     }*/

    //     var response =  await inboundLogService.RemoveInboundLog(id);
    //     result =  response;
    //     result.ResponseTime = DateTime.UtcNow;
    //     return Ok(result);
    // }
}
