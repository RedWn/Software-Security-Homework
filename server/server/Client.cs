using Newtonsoft.Json;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace server
{
    internal class Client
    {
        public TcpClient client;
        public StreamWriter sWriter;
        public StreamReader sReader;
        public ClientKeys keys;

        public int port;

        public Client(TcpClient tcpClient)
        {
            client = tcpClient;

            IPEndPoint ipendpoint = client.Client.RemoteEndPoint as IPEndPoint;
            port = ipendpoint.Port;

            sWriter = new StreamWriter(client.GetStream(), Encoding.ASCII);
            sReader = new StreamReader(client.GetStream(), Encoding.ASCII);
            keys = new ClientKeys();
        }

        public Package DecryptMessage(Package data, string mode)
        {
            string decryptedBody = Coder.decode(data.body["encrypted"], mode, keys);

            data.body.Clear();
            data.body = JsonConvert.DeserializeObject<Dictionary<string, string>>(decryptedBody);

            Logger.Log(LogType.info2, "decryption complete!");
            Logger.Log(LogType.info2, JsonConvert.SerializeObject(data));
            Logger.WriteLogs();

            return data;
        }

        public Package encryptData(Package data, string mode)
        {
            string encodedData = Coder.encode(JsonConvert.SerializeObject(data.body), mode, keys);

            data.body.Clear();
            data.body["encrypted"] = encodedData;

            return data;
        }
    }
}
