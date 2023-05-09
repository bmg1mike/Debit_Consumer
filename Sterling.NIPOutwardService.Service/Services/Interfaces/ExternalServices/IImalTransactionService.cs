using Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.ImalTransaction;

namespace Sterling.NIPOutwardService.Service.Services.Interfaces.ExternalServices;

public interface IImalTransactionService 
{
    Task<ImalTransactionResponseDto> NipFundsTransfer(ImalTransactionRequestDto request);
    OutboundLog GetOutboundLog();
    Task<ImalGetAccountDetailsResponse> GetAccountDetailsByNuban(string nuban);
}