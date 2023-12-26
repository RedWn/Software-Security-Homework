using Org.BouncyCastle.Bcpg.OpenPgp;
using server;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Cleints
{
    class Client
    {
        private TcpClient _client;
        private StreamReader _sReader;
        private StreamWriter _sWriter;
        private byte[] _sessionKey;
        private PgpPublicKey _publicKey;

        private Boolean _isConnected;

        public Client(String ipAddress, int portNum)
        {
            _client = new TcpClient();
            _client.Connect(ipAddress, portNum);
            try
            {
                sendHandshake();
                HandleCommunication();
            }
            catch (Exception)
            {
                _isConnected = false;
                _client.Close();
            }
        }

        public void sendHandshake() { }

        public void HandleCommunication()
        {
            _sReader = new StreamReader(_client.GetStream(), Encoding.ASCII);
            _sessionKey = Coder.getSessionKey();
            _isConnected = true;
            while (_isConnected)
            {
                Logger.Log(LogType.info1, "Type the message and press Enter to send file data");
                Logger.WriteLogs();
                string sData = ReadMultipleLines();
                //the context should signify why is the message being sent
                sendMessage(sData);
                receiveMessage();
            }
        }

        #region ENCRYPTION
        public Package decryptMessage(Package data, string mode)
        {
            string temp = new(data.body["encrypted"]);
            data.body.Clear();
            string decodedBody = Coder.decode(temp, _sessionKey, mode);
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
            data.body["encrypted"] = Coder.encode(temp, _sessionKey, mode);
            return data;
        }
        #endregion

        public async void receiveMessage()
        {
            string data = _sReader.ReadLine();
            Logger.Log(LogType.warning, "message recieved");
            Logger.WriteLogs();

            Package message = packageMessage(data);
            switch (message.type)
            {
                case "handshake":
                    message = decryptMessage(message, message.encryption);
                    _sessionKey = Convert.FromBase64String(message.body["publicKey"]);
                    break;
                case "generic":
                    decryptMessage(message, message.encryption);
                    break;
            }
        }

        public void sendMessage(string data)
        {
            Package? package = packageMessage(data);
            package = encryptData(package, package.encryption);
            _sWriter = new StreamWriter(_client.GetStream(), Encoding.ASCII);
            _sWriter.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(package));
            _sWriter.Flush();
            Console.Write("> Sent!");
        }

        public Package packageMessage(string data)
        {
            Dictionary<string, object> dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
            Package package = new(dictionary["encryption"].ToString(), dictionary["type"].ToString());
            package.body = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(dictionary["body"].ToString());
            return package;
        }

        public static string ReadMultipleLines()
        {
            StringBuilder sb = new StringBuilder();
            string line;
            while ((line = Console.ReadLine()) != "")
            {
                sb.AppendLine(line);
            }
            return sb.ToString();
        }
    }
}
