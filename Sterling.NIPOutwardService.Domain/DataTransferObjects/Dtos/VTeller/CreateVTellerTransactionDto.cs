namespace  Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.VTeller;

public class CreateVTellerTransactionDto
{

    public long ID;
    public Account inCust = new Account();
    public Beneficiary outCust = new Beneficiary();
    public string VAT_bra_code = "";
    public string VAT_cus_num = "";
    public string VAT_cur_code = "";
    public string VAT_led_code = "";
    public string VAT_sub_acct_code = "";
    public Int32 Appid;
    public string SessionID;
    public string transactionCode;
    public string PaymentReference = "0";
    public string DebitAccountNumber;
    public decimal Amount;
    public decimal feecharge;
    public decimal vat;
    public string tellerID;
    public string origin_branch;
    public string BranchCode;
    public string CustomerID;
    public string CurrencyCode;
    public string LedgerCode;
    public string OriginatorName;
}

public class Account
{
    public string bra_code;
    public string cus_num;
    public string cur_code;
    public string led_code;
    public string sub_acct_code;
    public string cus_sho_name;
}

public class Beneficiary
{
    public string cusname;
}