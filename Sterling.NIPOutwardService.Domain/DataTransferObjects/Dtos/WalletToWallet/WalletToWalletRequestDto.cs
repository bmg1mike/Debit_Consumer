namespace Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.WalletToWallet;

public class WalletToWalletRequestDto
    {
        [Required]
        public string amt { get; set; }
        [Required]
        public string toacct { get; set; }
        [Required]
        public string frmacct { get; set; }
        [Required]
        public string paymentRef { get; set; }
        [Required]
        public string remarks { get; set; }

        [Required]
        public int channelID { get; set; }

        [Required]
        public string CURRENCYCODE { get; set; }

        //  [Required]
        public int TransferType { get; set; }
    }