namespace Sterling.NIPOutwardService.Domain.Common;

public class FundsTransferResult<T>:Result<T>
{
   public string PaymentReference { get; set; } = "";
}