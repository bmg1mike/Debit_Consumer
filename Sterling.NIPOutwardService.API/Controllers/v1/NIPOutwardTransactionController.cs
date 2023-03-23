using Sterling.NIPOutwardService.Service.Services.Interfaces.Kafka;

namespace Sterling.NIPOutwardService.API.Controllers.v1;

[Authorize(AuthenticationSchemes = "Bearer")]
public partial class NIPOutwardTransactionController : BaseController 
{
    private readonly INIPOutwardDebitService nipOutwardDebitService;
    private readonly INIPOutwardDebitProducerService nipOutwardDebitProducerService;
    public NIPOutwardTransactionController(INIPOutwardDebitService nipOutwardDebitService, 
    INIPOutwardDebitProducerService nipOutwardDebitProducerService)
    {
        this.nipOutwardDebitService = nipOutwardDebitService;
        this.nipOutwardDebitProducerService = nipOutwardDebitProducerService;
    }
    [HttpPost]
    [Route("FundsTransfer")]
    public async Task<ActionResult> Transfer([FromBody] CreateNIPOutwardTransactionDto request)
    {
        var result = new Result<string>();
        result.RequestTime = DateTime.UtcNow.AddHours(1);
        var response = new Result<string>();

        if(request.PriorityLevel == 1)
            response =  await nipOutwardDebitService.ProcessTransaction(request);
        else
            response = await nipOutwardDebitProducerService.PublishTransaction(request);

        result = response;
        result.ResponseTime = DateTime.UtcNow.AddHours(1);
        return Ok(result);
    }

}