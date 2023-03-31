namespace Sterling.NIPOutwardService.Domain.Common.Generics;

public class NameEnquiryResult<T>:Result<T>
{
   public string SessionID { get; set; } = "";
}