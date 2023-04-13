namespace Sterling.NIPOutwardService.Service.Services.Interfaces.ExternalServices;

public interface IWalletTransactionService
{
    Task<WalletToWalletResponseDto> WalletToWalletTransfer(WalletToWalletRequestDto request);
    OutboundLog GetOutboundLog();
}