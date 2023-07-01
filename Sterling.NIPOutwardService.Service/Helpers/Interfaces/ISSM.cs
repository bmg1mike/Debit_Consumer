namespace Sterling.NIPOutwardService.Service.Helpers.Interfaces;

public interface ISSM 
{
    string Decrypt(string hex_response);
    string Encrypt(string request);
}