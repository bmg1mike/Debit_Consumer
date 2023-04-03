using System.Net;
using System.Net.Sockets;
using DidiSoft.Pgp;
using Sterling.NIPOutwardService.Service.Helpers.Interfaces;

namespace Sterling.NIPOutwardService.Service.Helpers.Implementations;

public class SSM : ISSM
{
    private readonly APISettings apiSettings;
    private readonly ILogger logger;
    //private string username = "sterlingnibsstest@test.com";
    //private string password = "Pass123";
    
    
    public SSM(IOptions<APISettings> apiSettings, ILogger logger)
    {
        this.apiSettings = apiSettings.Value;
        this.logger = logger;

    }
  
    //public string Encrypt(string message)
    //{
    //    Byte[] bytesSent = Encoding.ASCII.GetBytes("ENC" + message);
    //    Byte[] bytesReceived = new Byte[10000000];
    //    string page = string.Empty;

    //    using (var socket = ConnectSocket(apiSettings.NIPEncryptionSocketIP, apiSettings.NIPEncryptionSocketPort)) 
    //    {
    //        socket.Send(bytesSent, bytesSent.Length, 0);
    //        int bytes = 0;
    //        do
    //        {
    //            bytes = socket.Receive(bytesReceived, bytesReceived.Length, 0);
    //            page = page + Encoding.ASCII.GetString(bytesReceived, 0, bytes);
    //        }
    //        while (bytes > 0);
    //    } 
        
    //    return page.Substring(3, page.Length - 3);
    //}

    //public string Decrypt(string message)
    //{
    //    Byte[] bytesSent = Encoding.ASCII.GetBytes("DEC" + apiSettings.NIPEncryptionSocketPassword + "#" + message);
    //    Byte[] bytesReceived = new Byte[10000000];
    //    string page = string.Empty;


    //    using (var socket = ConnectSocket(apiSettings.NIPEncryptionSocketIP, apiSettings.NIPEncryptionSocketPort)) 
    //    {
    //        socket.Send(bytesSent, bytesSent.Length, 0);
    //        int bytes = 0;
    //        do
    //        {
    //            bytes = socket.Receive(bytesReceived, bytesReceived.Length, 0);
    //            page = page + Encoding.ASCII.GetString(bytesReceived, 0, bytes);
    //        }
    //        while (bytes > 0);
    //    }

    //    return page.Substring(3, page.Length - 3);
    //}

    //protected Socket ConnectSocket(string server, int port)
    //{
    //    Socket s = null;
    //    IPHostEntry hostEntry = null;
    //    hostEntry = Dns.GetHostEntry(server);
    //    foreach (IPAddress address in hostEntry.AddressList)
    //    {
    //        if (address.AddressFamily == AddressFamily.InterNetwork)
    //        {
    //            IPEndPoint ipe = new IPEndPoint(address, port);
    //            Socket tempSocket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
    //            try
    //            {
    //                tempSocket.Connect(ipe);
    //                if (tempSocket.Connected)
    //                {
    //                    s = tempSocket;
    //                    break;
    //                }
    //                else
    //                {
    //                    continue;
    //                }
    //            }
    //            catch (Exception)
    //            {
    //                if (tempSocket != null)
    //                    tempSocket.Close();

    //            }
    //        }
    //    }
    //    return s;
    //}

    public string Decrypt(string hex_response)
    {
        try
        {
            string[] decryptedVal = hex_response.Split(";", StringSplitOptions.RemoveEmptyEntries);
            StringBuilder sb = new();
            foreach (string decrypted in decryptedVal)
            {
                if(!string.IsNullOrEmpty(decrypted))
                    sb.Append(ProcessDecryption(decrypted));    
            }
            string response_output = sb.ToString();
            logger.Information($"Decrypted value PGP {response_output}");
            return response_output;
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"Error occured while decrypting {hex_response}");
            return "";
        }
    }
    private string ProcessDecryption(string hex_response)
    {
        //Stream? response_stream = null;
        //MemoryStream? response_output_stream = null;
        string? bank_private_key_password = apiSettings.NIBSSPrivateKeyPassword;
        string? bank_private_key_file = apiSettings.NIBSSPrivateKeyPath;
        byte[] byte_response = Hex.GetBytes(hex_response, out _);
        MemoryStream? response_output_stream = new();
        Stream? response_stream = new MemoryStream(byte_response);
        Stream key_stream = new FileStream(bank_private_key_file, FileMode.Open, FileAccess.Read);
        PGPLib pgp = new();
        pgp.DecryptStream(response_stream, key_stream, bank_private_key_password, response_output_stream);
        byte[] byte_response_output = response_output_stream.ToArray();
        string response_output = new System.Text.UTF8Encoding().GetString(byte_response_output);
        return response_output;
    }

    [Obsolete]
    public string Encrypt(string request)
    {
        StringBuilder response = new();
        List<string> strings = SplitString(request, 1024).ToList();
        foreach (string s in strings)
        {
            response.Append(ProcessEncryption(s));
        }
        #region discarded
        //int num3 = num2;
        //int num4 = request.Length;
        //int num5 = ((num2 != -1) ? (num4 / num2) : (-num4));
        //int num6 = request.Length;
        //int num7 = ((num2 != -1) ? (num6 % num2) : 0);
        //for (int i = 0; i < num5; i++)
        //{
        //    string text3 = request.Substring(num, num3);
        //    string text4 = Encrypt(text3);
        //    response.Append(text4);
        //    //response.Append(";");
        //    num = num3;
        //    num3 = num + num2;
        //}
        //if (num7 != 0)
        //{
        //    num3 = num + num7;
        //    try
        //    {
        //        string text5 = request.Substring(num, num3);
        //        string text3 = Encrypt(text5);
        //        if (string.Equals(text3, "***Error***"))
        //        {
        //            //response = "";
        //        }
        //        else
        //        {
        //            response.Append(text3);
        //            //response.Append(";");
        //        }
        //    }
        //    catch (Exception)
        //    {



        //        throw;
        //    }



        //}
        #endregion
        return response.ToString();
    }
    private static IEnumerable<string> SplitString(string text, int size)
    {
        for (int i = 0; i < text.Length; i+=size)
        {
            yield return text.Substring(i, Math.Min(size, text.Length - i));
        }
    }

    [Obsolete]
    public string ProcessEncryption(string request)
    {
        try
        {
            logger.Information($"Raw Request {request}");
            byte[]? input_byte = null;
            string input_hex = ""; string? encrypt_val = "";
            MemoryStream? input_stream = null;
            MemoryStream? output_stream = null;
            string? nibss_public_key_file = apiSettings.NIBSSPublicKeyPath;
            PGPLib pgp = new();
            input_stream = new MemoryStream(new System.Text.UTF8Encoding().GetBytes(request));
            output_stream = new MemoryStream();
            pgp.EncryptStream(input_stream, new FileInfo(nibss_public_key_file), output_stream, false);
            try
            {
                input_byte = output_stream.ToArray();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error encrypting");
            }
            input_hex = Hex.ToString(input_byte);
            encrypt_val = input_hex + ";";
            logger.Information("Encrypted value " + encrypt_val);
            return encrypt_val;
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"Error occured while decrypting {request}");
            return "";
        }
    }
}


public class Hex
    {
        public static byte[] HexWithFix(string hex)
        {
            byte[] raw = null;
            try
            {

                raw = new byte[hex.Length / 2];
                for (int i = 0; i < raw.Length; i++)
                {
                    raw[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                }
            }
            catch (Exception)
            {

 

            }
            return raw;
        }

 

        public static byte[] GetBytes(string data, out int discarded)
        {
            discarded = 1;
            int c;
            List<byte> result = new();

 

            try
            {
                using MemoryStream ms = new();
                using StreamWriter sw = new(ms);
                sw.Write(data);
                sw.Flush();
                ms.Position = 0;
                using StreamReader sr = new(ms);

 

                StringBuilder number = new();

 

                while ((c = sr.Read()) > 0)
                {
                    if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))
                    {
                        number.Append((char)c);

 

                        if (number.Length >= 2)
                        {
                            result.Add(Convert.ToByte(number.ToString(), 16));
                            number.Length = 0;
                        }
                    }
                }
            }
            catch (Exception)
            {

 

            }
            return result.ToArray();
        }

 

        public static int GetByteCount(string hexString)
        {
            int numHexChars = 0;
            char c;
            for (int i = 0; i < hexString.Length; i++)
            {
                c = hexString[i];
                if (IsHexDigit(c))
                    numHexChars++;
            }
            if (numHexChars % 2 != 0)
            {
                numHexChars--;
            }
            return numHexChars / 2; // 2 characters per byte
        }

        public static byte[] GetBytes_old(string hexString, out int discarded)
        {
            discarded = 0;
            string newString = "";
            char c;
            // remove all none A-F, 0-9, characters
            for (int i = 0; i < hexString.Length; i++)
            {
                c = hexString[i];
                if (IsHexDigit(c))
                    newString += c;
                else
                    discarded++;
            }
            // if odd number of characters, discard last character
            if (newString.Length % 2 != 0)
            {
                discarded++;
                newString = newString[..^1];
            }

 

            int byteLength = newString.Length / 2;
            byte[] bytes = new byte[byteLength];
            string hex;
            int j = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                hex = new String(new Char[] { newString[j], newString[j + 1] });
                bytes[i] = HexToByte(hex);
                j += 2;
            }
            return bytes;
        }
        public static string ToString(byte[] bytes)
        {
            string hexString = "";
            for (int i = 0; i < bytes.Length; i++)
            {
                hexString += bytes[i].ToString("X2");
            }
            return hexString;
        }

        public static bool InHexFormat(string hexString)
        {
            bool hexFormat = true;

 

            foreach (char digit in hexString)
            {
                if (!IsHexDigit(digit))
                {
                    hexFormat = false;
                    break;
                }
            }
            return hexFormat;
        }
        public static bool IsHexDigit(Char c)
        {
            int numChar;
            int numA = Convert.ToInt32('A');
            int num1 = Convert.ToInt32('0');
            c = Char.ToUpper(c);
            numChar = Convert.ToInt32(c);
            if (numChar >= numA && numChar < (numA + 6))
                return true;
            if (numChar >= num1 && numChar < (num1 + 10))
                return true;
            return false;
        }
        private static byte HexToByte(string hex)
        {
            if (hex.Length > 2 || hex.Length <= 0)
                throw new ArgumentException("hex must be 1 or 2 characters in length");
            byte newByte = byte.Parse(hex, System.Globalization.NumberStyles.HexNumber);
            return newByte;
        }
    }