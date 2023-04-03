using Sterling.NIPOutwardService.Domain.Common.Generics;
using Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.NameEnquiry;
using Sterling.NIPOutwardService.Service.Services.Implementations;

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
    [HttpPost]
    [Route("TransactionValidation")]
    public async Task<ActionResult> TransactionValidation([FromBody] TransactionValidationRequest request)
    {
        var result = new FundsTransferResult<string>();
        result.RequestTime = DateTime.UtcNow.AddHours(1);
        var response = new FundsTransferResult<string>();
        
        response =  await nipOutwardTransactionService.CheckIfTransactionIsSuccesful(request);

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
    [HttpPost]
    [Route("NameEnquiry")]
    public async Task<ActionResult> NameEnquiry([FromBody] NameEnquiryRequestDto request)
    {
        var result = new NameEnquiryResult<NameEnquiryResponseDto>();
        result.RequestTime = DateTime.UtcNow.AddHours(1);
        var response = new NameEnquiryResult<NameEnquiryResponseDto>();

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