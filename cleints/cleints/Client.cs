using Org.BouncyCastle.Bcpg.OpenPgp;
using Safester.CryptoLibrary.Api;
using server;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Cleints
{
    public class ClientKeys {
        public byte[] sessionKey;
        public PgpKeyPairHolder PGPKeys;
        public string passphrase;
        public string targetPublicKeyRing;
    }
    class Client
    {
        private TcpClient _client;
        private StreamReader _sReader;
        private StreamWriter _sWriter;
        private string _identity;
        private bool _isConnected;
        ClientKeys keys;
        

        public Client(string ipAddress, int portNum)
        {
            _client = new TcpClient();
            _client.Connect(ipAddress, portNum);
            _identity = "test1";
            keys = new ClientKeys();
            keys.passphrase = RandomString(8);
            PgpKeyPairGenerator generator = new(_identity, keys.passphrase.ToArray(), PublicKeyAlgorithm.RSA, PublicKeyLength.BITS_2048);
            keys.PGPKeys = generator.Generate();
            _sReader = new StreamReader(_client.GetStream(), Encoding.ASCII);
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

        public void sendHandshake()
        {
            string publicKey = keys.PGPKeys.PublicKeyRing;
            Dictionary<string, string> body = new Dictionary<string, string>();
            body["publicKey"] = publicKey;
            sendMessage(messageBuilder( "NA", "handshake",body));
            receiveMessage();
            keys.sessionKey = Coder.getSessionKey();
            string sessionKey = Convert.ToBase64String(keys.sessionKey);
            body = new Dictionary<string, string>();
            body["key"] = sessionKey;
            sendMessage(messageBuilder("PGP", "sessionKey", body));
            receiveMessage();
        }

        public void HandleCommunication()
        {
            keys.sessionKey = Coder.getSessionKey();
            _isConnected = true;
            while (_isConnected)
            {
                Logger.Log(LogType.info1, "Type the message and press Enter to send file data");
                Logger.WriteLogs();
                string sData = ReadMultipleLines();
                sendMessage(sData);
                receiveMessage();
            }
        }

        #region ENCRYPTION
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
                    keys.targetPublicKeyRing = message.body["publicKey"];
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
            Console.WriteLine("> Sent!");
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
            while ((line = Console.ReadLine()) != "END")
            {
                sb.AppendLine(line);
            }
            return sb.ToString();
        }

        public static string RandomString(int length)
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public string messageBuilder(string encryption, string type, Dictionary<string, string> body) {
            Package p = new(encryption,type);
            p.body = body;
            return Newtonsoft.Json.JsonConvert.SerializeObject(p);
        }
    }
}
