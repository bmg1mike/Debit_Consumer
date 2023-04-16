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
        
        return Ok(response);
    }
    [HttpPost]
    [Route("TransactionValidation")]
    public async Task<ActionResult> TransactionValidation([FromBody][Required] TransactionValidationRequestDto request)
    {        
        Result<string> response =  await nipOutwardTransactionService.CheckIfTransactionIsSuccessful(request);

        return Ok(response);
    }
    [HttpPost]
    [Route("NameEnquiry")]
    public async Task<ActionResult> NameEnquiry([FromBody][Required] NameEnquiryRequestDto request)
    {

        Result<NameEnquiryResponseDto> response = await nipOutwardNameEnquiryService.DoNameEnquiry(request);

        return Ok(response);
    }
}