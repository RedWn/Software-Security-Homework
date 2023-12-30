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

        public Package DecryptPackageBody(Package package)
        {
            string decryptedBody = Coder.decode(package.body["encrypted"], package.encryption, keys);

            package.body.Clear();
            package.body = JsonConvert.DeserializeObject<Dictionary<string, string>>(decryptedBody);

            Logger.Log(LogType.info2, "decryption complete!");
            Logger.Log(LogType.info2, JsonConvert.SerializeObject(package));
            Logger.WriteLogs();

            return package;
        }

        public Package EncryptPackageBody(Package package)
        {
            string encodedData = Coder.encode(JsonConvert.SerializeObject(package.body), package.encryption, keys);

            package.body.Clear();
            package.body["encrypted"] = encodedData;

            return package;
        }
    }
}
