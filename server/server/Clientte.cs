using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace server
{
    internal class Clientte
    {
        public TcpClient client;
        public IPEndPoint IPEndPoint;
        public StreamWriter sWriter;
        public StreamReader sReader;
        public byte[] sessionKey;
        public PgpPublicKey publicKey;

        public Clientte(object obj)
        {
            client = (TcpClient)obj;
            IPEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
            sWriter = new StreamWriter(client.GetStream(), Encoding.ASCII);
            sReader = new StreamReader(client.GetStream(), Encoding.ASCII);
        }

        public Package decryptMessage(Package data, string mode)
        {
            string temp = new(data.body["encrypted"]);
            data.body.Clear();
            string decodedBody = Coder.decode(temp, sessionKey, mode);
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
            data.body["encrypted"] = Coder.encode(temp, sessionKey, mode);
            return data;
        }
    }
}
