namespace Sterling.NIPOutwardService.Domain.DataTransferObjects.Dtos.ImalTransaction;

public class ImalGetAccountDetailsResponse 
{
    public string Message { get; set; }
    public GetAccounts GetAccounts { get; set; }
}

public class GetAccounts
{
    public string BRANCH_NAME { get; set; }
    public string BRANCH_CODE { get; set; }
    public string ACCOUNT_NO { get; set; }
    public string ACC_TYPE { get; set; }
    public string CURRENCY { get; set; }
    public int CURRENCY_CODE { get; set; }
    public int GL_CODE { get; set; }
    public int CIF_SUB_NO { get; set; }
    public int SL_NO { get; set; }
    public string BVN { get; set; }
    public string ACC_NAME { get; set; }
    public string STATUS { get; set; }
    public string CIF_STATUS { get; set; }
    public string PHONE_NUMBER { get; set; }
    public string EMAIL { get; set; }
    public string HAS_PND { get; set; }
    public string CUST_TYPE { get; set; }
    public string DATE_OPENED { get; set; }
    public string FIRST_NAME { get; set; }
    public string LAST_NAME { get; set; }
    public double Aval_Balance { get; set; }
    public string RM_NAME { get; set; }
    public string DAO_CODE { get; set; }
}


