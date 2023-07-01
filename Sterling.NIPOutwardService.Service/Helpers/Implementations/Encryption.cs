using System.Security.Cryptography;
using Serilog.Core;
using Sterling.NIPOutwardService.Service.Helpers.Interfaces;

namespace Sterling.NIPOutwardService.Service.Helpers.Implementations;

public class Encryption:IEncryption
{
    public string CalculateChecksum(string ApiKey)
    {
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string checksum = GenerateChecksum(ApiKey, timestamp);
        string headerValue = $"{checksum}:{timestamp}";
        return headerValue;
    }
    private string GenerateChecksum(string apiKey, long timestamp)
    {
        // Convert the API key and the timestamp to byte arrays
        byte[] apiKeyBytes = Encoding.UTF8.GetBytes(apiKey);
        byte[] timestampBytes = BitConverter.GetBytes(timestamp);
        // Concatenate the byte arrays
        byte[] inputBytes = apiKeyBytes.Concat(timestampBytes).ToArray();
        // Create a SHA256 instance
        using SHA256 sha256 = SHA256.Create();
        // Compute the hash of the input bytes
        byte[] hashBytes = sha256.ComputeHash(inputBytes);
        // Convert the hash bytes to a hexadecimal string
        string hashString = BitConverter.ToString(hashBytes).Replace("-", "");
        // Return the checksum value
        return hashString;
    }

    public string DecryptAes(string ciphertext, string secretKey, string iv)
    {
        try
        {  // Create a new instance of the Aes    
            // class.  This generates a new key and initialization                 
            // vector (IV).                 
            using Aes myAes = Aes.Create();
            myAes.Key = Encoding.UTF8.GetBytes(secretKey);
            myAes.IV = Encoding.UTF8.GetBytes(iv);

            // Decrypt the bytes to a string. 
            string roundtrip = DecryptStringFromBytes_Aes(ciphertext, myAes.Key, myAes.IV);
            return roundtrip;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Method: DecryptAes.");
            return string.Empty;
        }
    }

    private string DecryptStringFromBytes_Aes(string cipherText, byte[] Key, byte[] IV)
    {             // Check arguments.             
        if (cipherText == null || cipherText.Length <= 0)
            throw new ArgumentNullException("cipherText");
        if (Key == null || Key.Length <= 0)
            throw new ArgumentNullException("Key");
        if (IV == null || IV.Length <= 0)
            throw new ArgumentNullException("IV");

        // Declare the string used to hold             
        // the decrypted text.             
        string plaintext = null;

        // Create an Aes object             
        // with the specified key and IV.             
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Key;
            aesAlg.IV = IV;
            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
            byte[] cipherbytes = HexadecimalStringToByteArray(cipherText);

            // Create the streams used for decryption.                 
            using MemoryStream msDecrypt = new(cipherbytes);
            using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);
            using StreamReader srDecrypt = new(csDecrypt);
            // Read the decrypted bytes from the decrypting stream                             
            // and place them in a string.                             
            plaintext = srDecrypt.ReadToEnd();

        }

        return plaintext;
    }

    public string EncryptAes(string plaintext, string secretkey, string iv)
    {
        try
        {
            using Aes myAes = Aes.Create();
            myAes.Key = Encoding.UTF8.GetBytes(secretkey);
            myAes.IV = Encoding.UTF8.GetBytes(iv);

            // Encrypt the string to an array of bytes.                     
            byte[] encrypted = EncryptStringToBytes_Aes(plaintext, myAes.Key, myAes.IV);

            string ciphertext = ByteArrayToString(encrypted);

            return ciphertext;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Method: EncryptAes.");
            return string.Empty;
        }
    }

    private byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
    {             // Check arguments.             
        if (plainText == null || plainText.Length <= 0)
            throw new ArgumentNullException("plainText");
        if (Key == null || Key.Length <= 0)
            throw new ArgumentNullException("Key");
        if (IV == null || IV.Length <= 0)
            throw new ArgumentNullException("IV");
        byte[] encrypted;
        // with the specified key and IV.             
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Key;
            aesAlg.IV = IV;
            // Create an encryptor to perform the stream transform.                 
            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
            // Create the streams used for encryption.                 
            using MemoryStream msEncrypt = new();
            using CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write);
            using (StreamWriter swEncrypt = new(csEncrypt))
            {
                //Write all data to the stream.     
                swEncrypt.Write(plainText);
            }
            encrypted = msEncrypt.ToArray();
        }
        // Return the encrypted bytes from the memory stream.            
        return encrypted;
    }
    private byte[] HexadecimalStringToByteArray(string input)
    {
        var outputLength = input.Length / 2;
        var output = new byte[outputLength];
        using (var sr = new StringReader(input))
        {
            for (var i = 0; i < outputLength; i++)
                output[i] = Convert.ToByte(new string(new char[2] { (char)sr.Read(), (char)sr.Read() }), 16);
        }
        return output;
    }

    private string ByteArrayToString(byte[] ba)
    {
        StringBuilder hex = new(ba.Length * 2);
        foreach (byte b in ba) hex.AppendFormat("{0:x2}", b); return hex.ToString();
    }
}