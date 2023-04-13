namespace Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.WalletToWallet;

public class WalletToWalletResponseDto
{
    public string message { get; set; }
    public string response { get; set; }
    public string responsedata { get; set; }
    public WalletInfos data { get; set; }

    public class WalletInfos
    {
        public bool sent { get; set; }
    }
}