namespace Sterling.NIPOutwardService.Data.Helpers.Implementations;
public class UtilityHelper : IUtilityHelper
{
    private readonly AppSettings appSettings;
    public UtilityHelper(IOptions<AppSettings> appSettings)
    {
        this.appSettings = appSettings.Value;
    }
    public string GenerateFundsTransferSessionId(long Id)
    {
        var width = 12;

        var result = Id % Math.Pow(10, width);

        string SessionID = appSettings.SterlingBankCode + 
        DateTime.Now.ToString("yyMMddHHmmss") + result.ToString().PadLeft(width, '0');

        return SessionID;
    }
    public string GenerateRandomNumber(int count)
    {
        string[] key2 = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        Random rand = new Random();
        string txt = "";
        for (int j = 0; j < count; j++)
            txt += key2[rand.Next(0, 9)];
        return txt;
    }

    public string GenerateTransactionReference(string bra_code)
    {
        return "232" + DateTime.UtcNow.AddHours(1).ToString("yyyyMMddHHmmss");
    }

    public string RemoveSpecialCharacters(string str)
    {
        //string[] chars = new string[] { ",", ".", "/", "!", "@", "#", "$", "%", "^", "&", "*", "'", "\"", ";", "-", "_", "(", ")", ":", "|", "[", "]" };
        string[] chars = new string[] { ",", ".", "!", "@", "#", "$", "%", "^", "&", "*", "'", "\"", ";", "-", "_", "(", ")", ":", "|", "[", "]" };
        for (int i = 0; i < chars.Length; i++)
        {
            if (str.Contains(chars[i]))
            {
                str = str.Replace(chars[i], " ");
            }
        }
        return str;
    }
}