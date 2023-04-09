using System.ComponentModel.DataAnnotations;
using Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.NameEnquiry;

namespace Sterling.NIPOutwardService.API.Controllers.v1;

[Authorize(AuthenticationSchemes = "Bearer")]
public partial class NIPOutwardTransactionController : BaseController 
{
    private readonly INIPOutwardDebitService nipOutwardDebitService;
    private readonly INIPOutwardTransactionService nipOutwardTransactionService;
    private readonly INIPOutwardNameEnquiryService nipOutwardNameEnquiryService;

    public NIPOutwardTransactionController(INIPOutwardDebitService nipOutwardDebitService,
    INIPOutwardTransactionService nipOutwardTransactionService, INIPOutwardNameEnquiryService nipOutwardNameEnquiryService)
    {
        this.nipOutwardDebitService = nipOutwardDebitService;
        this.nipOutwardTransactionService = nipOutwardTransactionService;
        this.nipOutwardNameEnquiryService = nipOutwardNameEnquiryService;
    }
    [HttpPost]
    [Route("FundsTransfer")]
    public async Task<ActionResult> FundsTransfer([FromBody][Required] CreateNIPOutwardTransactionDto request)
    {        
        FundsTransferResult<string> response =  await nipOutwardDebitService.ProcessTransaction(request);

        if(!response.IsSuccess)
        {
            response.ErrorMessage = response.Message;
            response.Message = "Transaction failed";
        }
        //response.Content = string.Empty;

        //result = response;
        //result.PaymentReference = request.PaymentReference;
        //result.ResponseTime = DateTime.UtcNow.AddHours(1);
        return Ok(response);
    }
    [HttpPost]
    [Route("TransactionValidation")]
    public async Task<ActionResult> TransactionValidation([FromBody][Required] TransactionValidationRequestDto request)
    {
        var result = new Result<string>();
        result.RequestTime = DateTime.UtcNow.AddHours(1);
        var response = new Result<string>();
        
        response =  await nipOutwardTransactionService.CheckIfTransactionIsSuccessful(request);

        if(!response.IsSuccess)
        {
            response.ErrorMessage = response.Message;
        }
        response.Content = string.Empty;

        result = response;
        result.ResponseTime = DateTime.UtcNow.AddHours(1);
        result.SessionID = request.SessionID;
        return Ok(result);
    }
    [HttpPost]
    [Route("NameEnquiry")]
    public async Task<ActionResult> NameEnquiry([FromBody][Required] NameEnquiryRequestDto request)
    {
        var result = new Result<NameEnquiryResponseDto?>();
        result.RequestTime = DateTime.UtcNow.AddHours(1);
        var response = new Result<NameEnquiryResponseDto?>();

        response = await nipOutwardNameEnquiryService.DoNameEnquiry(request);

        if (!response.IsSuccess)
        {
            response.ErrorMessage = response.Message;
            response.Content = null;

        }

        result = response;
        result.ResponseTime = DateTime.UtcNow.AddHours(1);
        result.SessionID = request.SessionID;
        return Ok(result);
    }
}