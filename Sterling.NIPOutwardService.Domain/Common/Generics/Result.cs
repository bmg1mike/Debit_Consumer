namespace Sterling.NIPOutwardService.Domain.Common;

public class Result<T>
{
   public T Content { get; set; }
   public Error Error { get; set; }
   public bool HasError => ErrorMessage != "";
   public string ErrorMessage { get; set; } = "";
   public string Message { get; set; } = "";
   public bool IsSuccess { get; set; } = true;
   public DateTime RequestTime { get; set; } = DateTime.UtcNow;
   public DateTime ResponseTime { get; set; } = DateTime.UtcNow;
}