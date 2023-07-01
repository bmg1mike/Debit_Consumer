namespace Sterling.NIPOutwardService.Data.Helpers.Interfaces;
public interface IUtilityHelper 
{
    string GenerateFundsTransferSessionId(long Id);
    string GenerateRandomNumber(int count);
    string GenerateTransactionReference(string bra_code);
    string RemoveSpecialCharacters(string str);
}