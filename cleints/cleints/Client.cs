using Newtonsoft.Json;
using Safester.CryptoLibrary.Api;
using System.Net.Sockets;
using System.Text;

namespace Cleints
{
    class Client
    {
        ClientKeys keys;

        private TcpClient _client;
        private StreamReader _sReader;
        private StreamWriter _sWriter;

        private string _identity;
        private bool _isConnected;


        public Client(string ipAddress, int portNum)
        {
            _client = new TcpClient();
            _client.Connect(ipAddress, portNum);
            _identity = "test1";

            keys = new ClientKeys();
            keys.passphrase = Utils.GetRandomString(8);
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
            sendMessage(getSerializedPackage("NA", "handshake", body));
            receiveMessage();
            keys.sessionKey = Coder.getSessionKey();
            string sessionKey = Convert.ToBase64String(keys.sessionKey);
            body = new Dictionary<string, string>();
            body["key"] = sessionKey;
            sendMessage(getSerializedPackage("PGP", "sessionKey", body));
            receiveMessage();
        }

        public void HandleCommunication()
        {
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
            data.body = JsonConvert.DeserializeObject<Dictionary<string, string>>(decodedBody);
            Logger.Log(LogType.info2, "decryption complete!");
            Logger.Log(LogType.info2, JsonConvert.SerializeObject(data));
            Logger.WriteLogs();
            return data;
        }

        public Package encryptData(Package data, string mode)
        {
            string temp = JsonConvert.SerializeObject(data.body);
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

            Package message = Package.FromClientData(data);
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
            Package? package = Package.FromClientData(data);
            package = encryptData(package, package.encryption);
            _sWriter = new StreamWriter(_client.GetStream(), Encoding.ASCII);
            _sWriter.WriteLine(JsonConvert.SerializeObject(package));
            _sWriter.Flush();
            Console.WriteLine("> Sent!");
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


        private string getSerializedPackage(string encryption, string type, Dictionary<string, string> body)
        {
            return JsonConvert.SerializeObject(new Package(encryption, type, body));
        }
    }
}
