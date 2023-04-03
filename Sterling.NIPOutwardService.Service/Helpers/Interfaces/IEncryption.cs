namespace Sterling.NIPOutwardService.Service.Helpers.Interfaces;

public interface IEncryption 
{
    string CalculateChecksum(string ApiKey);
    string DecryptAes(string ciphertext, string secretKey, string iv);
    string EncryptAes(string plaintext, string secretkey, string iv);
}