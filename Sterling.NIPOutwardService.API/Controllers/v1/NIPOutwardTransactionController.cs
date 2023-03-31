namespace Sterling.NIPOutwardService.API.Controllers.v1;

[Authorize(AuthenticationSchemes = "Bearer")]
public partial class NIPOutwardTransactionController : BaseController 
{
    private readonly INIPOutwardDebitService nipOutwardDebitService;
    public NIPOutwardTransactionController(INIPOutwardDebitService nipOutwardDebitService)
    {
        this.nipOutwardDebitService = nipOutwardDebitService;
    }
    [HttpPost]
    [Route("FundsTransfer")]
    public async Task<ActionResult> Transfer([FromBody] CreateNIPOutwardTransactionDto request)
    {
        var result = new FundsTransferResult<string>();
        result.RequestTime = DateTime.UtcNow.AddHours(1);
        var response = new FundsTransferResult<string>();
        
        response =  await nipOutwardDebitService.ProcessTransaction(request);

        if(!response.IsSuccess)
        {
            response.ErrorMessage = response.Message;
        }
        response.Content = string.Empty;

        result = response;
        result.ResponseTime = DateTime.UtcNow.AddHours(1);
        result.PaymentReference = request.PaymentReference;
        return Ok(result);
    }
}