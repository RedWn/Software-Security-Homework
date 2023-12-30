using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Safester.CryptoLibrary.Api;

namespace server
{
    internal class Client
    {
        public TcpClient client;
        public int port;
        public StreamWriter sWriter;
        public StreamReader sReader;
        public ClientKeys keys;

        public Client(object obj)
        {
            client = (TcpClient)obj;
            IPEndPoint ipendpoint = client.Client.RemoteEndPoint as IPEndPoint;
            port = ipendpoint.Port;
            sWriter = new StreamWriter(client.GetStream(), Encoding.ASCII);
            sReader = new StreamReader(client.GetStream(), Encoding.ASCII);
            keys = new ClientKeys();
        }

        public Package decryptMessage(Package data, string mode)
        {
            string temp = new(data.body["encrypted"]);
            data.body.Clear();
            string decodedBody = Coder.decode(temp, mode, keys);
            data.body = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(decodedBody);
            Logger.Log(LogType.info2, "decryption complete!");
            Logger.Log(LogType.info2, Newtonsoft.Json.JsonConvert.SerializeObject(data));
            Logger.WriteLogs();
            return data;
        }

        public Package encryptData(Package data, string mode)
        {
            string temp = Newtonsoft.Json.JsonConvert.SerializeObject(data.body);
            data.body.Clear();
            data.body["encrypted"] = Coder.encode(temp, mode, keys);
            return data;
        }
    }
}
