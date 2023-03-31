using System.Net;
using System.Net.Sockets;
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
  
    public string Encrypt(string message)
    {
        Byte[] bytesSent = Encoding.ASCII.GetBytes("ENC" + message);
        Byte[] bytesReceived = new Byte[10000000];
        string page = string.Empty;

        using (var socket = ConnectSocket(apiSettings.NIPEncryptionSocketIP, apiSettings.NIPEncryptionSocketPort)) 
        {
            socket.Send(bytesSent, bytesSent.Length, 0);
            int bytes = 0;
            do
            {
                bytes = socket.Receive(bytesReceived, bytesReceived.Length, 0);
                page = page + Encoding.ASCII.GetString(bytesReceived, 0, bytes);
            }
            while (bytes > 0);
        } 
        
        return page.Substring(3, page.Length - 3);
    }

    public string Decrypt(string message)
    {
        Byte[] bytesSent = Encoding.ASCII.GetBytes("DEC" + apiSettings.NIPEncryptionSocketPassword + "#" + message);
        Byte[] bytesReceived = new Byte[10000000];
        string page = string.Empty;


        using (var socket = ConnectSocket(apiSettings.NIPEncryptionSocketIP, apiSettings.NIPEncryptionSocketPort)) 
        {
            socket.Send(bytesSent, bytesSent.Length, 0);
            int bytes = 0;
            do
            {
                bytes = socket.Receive(bytesReceived, bytesReceived.Length, 0);
                page = page + Encoding.ASCII.GetString(bytesReceived, 0, bytes);
            }
            while (bytes > 0);
        }

        return page.Substring(3, page.Length - 3);
    }



    protected Socket ConnectSocket(string server, int port)
    {
        Socket s = null;
        IPHostEntry hostEntry = null;
        hostEntry = Dns.GetHostEntry(server);
        foreach (IPAddress address in hostEntry.AddressList)
        {
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                IPEndPoint ipe = new IPEndPoint(address, port);
                Socket tempSocket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    tempSocket.Connect(ipe);
                    if (tempSocket.Connected)
                    {
                        s = tempSocket;
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
                catch (Exception)
                {
                    if (tempSocket != null)
                        tempSocket.Close();

                }
            }
        }
        return s;
    }
}