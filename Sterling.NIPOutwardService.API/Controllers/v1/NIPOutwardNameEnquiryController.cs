using Sterling.NIPOutwardService.Domain.Common.Generics;
using Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.NameEnquiry;

namespace Sterling.NIPOutwardService.API.Controllers.v1;

[Authorize(AuthenticationSchemes = "Bearer")]
public partial class NIPOutwardNameEnquiryController : BaseController 
{
    private readonly INIPOutwardNameEnquiryService nipOutwardNameEnquiryService;
    public NIPOutwardNameEnquiryController(INIPOutwardNameEnquiryService nipOutwardNameEnquiryService)
    {
        this.nipOutwardNameEnquiryService = nipOutwardNameEnquiryService;
    }
    [HttpPost]
    [Route("NameEnquiry")]
    public async Task<ActionResult> NameEnquiry([FromBody] NameEnquiryRequestDto request)
    {
        var result = new NameEnquiryResult<NameEnquiryResponseDto>();
        result.RequestTime = DateTime.UtcNow.AddHours(1);
        var response = new NameEnquiryResult<NameEnquiryResponseDto>();
        
        response =  await nipOutwardNameEnquiryService.DoNameEnquiry(request);

        if(!response.IsSuccess)
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