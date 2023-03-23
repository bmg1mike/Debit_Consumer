namespace Sterling.NIPOutwardService.Service.Helpers;

using System.Net.NetworkInformation;
using System.Text;
using System.Xml;
using System;
using System.IO;
using System.Security.Cryptography;


public class XMLGenerator
{
    private StringBuilder xml = new StringBuilder();

    public string resp = "";

    public string req = "";

    public XmlNodeList debits;

    public XmlNodeList credits;

    public XMLGenerator(string teller_id)
    {
        xml = new StringBuilder();
        xml.Append("<?xml version=\"1.0\" encoding=\"iso-8859-1\"?>");
        xml.Append("<vteller>");
        xml.Append("<teller_id>" + teller_id + "</teller_id>");
    }

    public void addAccount(string bra_code, string cus_num, string cur_code, string led_code, string sub_code, string amount, string expl_code, string remark)
    {
        xml.Append("<account>");
        xml.Append("<bra_code>" + bra_code + "</bra_code>");
        xml.Append("<cus_num>" + cus_num + "</cus_num>");
        xml.Append("<cur_code>" + cur_code + "</cur_code>");
        xml.Append("<led_code>" + led_code + "</led_code>");
        xml.Append("<sub_code>" + sub_code + "</sub_code>");
        xml.Append("<amount>" + amount + "</amount>");
        xml.Append("<desc>" + expl_code + "</desc>");
        xml.Append("<remark>" + remark + "</remark>");
        xml.Append("</account>");
    }

    public void startDebit()
    {
        xml.Append("<debit>");
    }

    public void endDebit()
    {
        xml.Append("</debit>");
    }

    public void startCredit()
    {
        xml.Append("<credit>");
    }

    public void endCredit()
    {
        xml.Append("</credit>");
    }

    public void closeXML()
    {
        getMACs();
        xml.Append("</vteller>");
        req = encryptme(xml.ToString());
    }

    public void parseResponse()
    {
        resp = decryptme(resp);
        XmlDocument xmlDocument = new XmlDocument();
        xmlDocument.LoadXml(resp);
        int count = xmlDocument.ChildNodes[1].ChildNodes[1].ChildNodes.Count;
        debits = xmlDocument.ChildNodes[1].ChildNodes[1].ChildNodes;
        int count2 = xmlDocument.ChildNodes[1].ChildNodes[2].ChildNodes.Count;
        credits = xmlDocument.ChildNodes[1].ChildNodes[2].ChildNodes;
    }

    protected string encryptme(string txt)
    {
        Crypt crypt = new Crypt();
        string text = "";
        return crypt.Encrypt(txt, "u94%#7fh#58&2o9./=+");
    }

    protected string decryptme(string txt)
    {
        Crypt crypt = new Crypt();
        string text = "";
        return crypt.Decrypt(txt, "u94%#7fh#58&2o9./=+");
    }

    protected void getMACs()
    {
        xml.Append("<macs>");
        NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
        NetworkInterface[] array = allNetworkInterfaces;
        foreach (NetworkInterface networkInterface in array)
        {
            IPInterfaceProperties iPProperties = networkInterface.GetIPProperties();
            PhysicalAddress physicalAddress = networkInterface.GetPhysicalAddress();
            xml.Append("<mac>");
            xml.Append(physicalAddress.ToString());
            xml.Append("</mac>");
        }

        xml.Append("</macs>");
    }
}



internal class Crypt
{
    public byte[] Encrypt(byte[] clearData, byte[] Key, byte[] IV)
    {
        MemoryStream memoryStream = new MemoryStream(); 
        Rijndael rijndael = Rijndael.Create();
        rijndael.Key = Key;
        rijndael.IV = IV;
        CryptoStream cryptoStream = new CryptoStream(memoryStream, rijndael.CreateEncryptor(), CryptoStreamMode.Write);
        cryptoStream.Write(clearData, 0, clearData.Length);
        cryptoStream.Close();
        return memoryStream.ToArray();
    }

    public string Encrypt(string clearText, string Password)
    {
        byte[] bytes = Encoding.Unicode.GetBytes(clearText);
        PasswordDeriveBytes passwordDeriveBytes = new PasswordDeriveBytes(Password, new byte[13]
        {
            73, 118, 97, 110, 32, 77, 101, 100, 118, 101,
            100, 101, 118
        });
        byte[] inArray = Encrypt(bytes, passwordDeriveBytes.GetBytes(32), passwordDeriveBytes.GetBytes(16));
        return Convert.ToBase64String(inArray);
    }

    public byte[] Encrypt(byte[] clearData, string Password)
    {
        PasswordDeriveBytes passwordDeriveBytes = new PasswordDeriveBytes(Password, new byte[13]
        {
            73, 118, 97, 110, 32, 77, 101, 100, 118, 101,
            100, 101, 118
        });
        return Encrypt(clearData, passwordDeriveBytes.GetBytes(32), passwordDeriveBytes.GetBytes(16));
    }

    public byte[] Decrypt(byte[] cipherData, byte[] Key, byte[] IV)
    {
        MemoryStream memoryStream = new MemoryStream();
        Rijndael rijndael = Rijndael.Create();
        rijndael.Key = Key;
        rijndael.IV = IV;
        CryptoStream cryptoStream = new CryptoStream(memoryStream, rijndael.CreateDecryptor(), CryptoStreamMode.Write);
        cryptoStream.Write(cipherData, 0, cipherData.Length);
        cryptoStream.Close();
        return memoryStream.ToArray();
    }

    public string Decrypt(string cipherText, string Password)
    {
        byte[] cipherData = Convert.FromBase64String(cipherText);
        PasswordDeriveBytes passwordDeriveBytes = new PasswordDeriveBytes(Password, new byte[13]
        {
            73, 118, 97, 110, 32, 77, 101, 100, 118, 101,
            100, 101, 118
        });
        byte[] bytes = Decrypt(cipherData, passwordDeriveBytes.GetBytes(32), passwordDeriveBytes.GetBytes(16));
        return Encoding.Unicode.GetString(bytes);
    }

    public byte[] Decrypt(byte[] cipherData, string Password)
    {
        PasswordDeriveBytes passwordDeriveBytes = new PasswordDeriveBytes(Password, new byte[13]
        {
            73, 118, 97, 110, 32, 77, 101, 100, 118, 101,
            100, 101, 118
        });
        return Decrypt(cipherData, passwordDeriveBytes.GetBytes(32), passwordDeriveBytes.GetBytes(16));
    }
}