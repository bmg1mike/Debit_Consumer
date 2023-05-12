namespace Sterling.NIPOutwardService.Service.Services.Interfaces.ExternalServices;

public interface IImalTransactionService 
{
    Task<ImalTransactionResponseDto> NipFundsTransfer(ImalTransactionRequestDto request);
    OutboundLog GetOutboundLog();
}