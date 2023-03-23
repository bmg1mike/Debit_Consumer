namespace Sterling.NIPOutwardService.API.Controllers.v1;

[Route("api/v{version:apiVersion}/[controller]")]
[ApiController]
[ApiVersion("1.0")]
public class BaseController : ControllerBase
{

    public BaseController()
    {
      
    }
 
}
