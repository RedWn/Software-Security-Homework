using Newtonsoft.Json;
using Safester.CryptoLibrary.Api;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        public Client(string ip, int port)
        {
            _client = new TcpClient();
            _client.Connect(ip, port);
            _sReader = new StreamReader(_client.GetStream(), Encoding.ASCII);
            _identity = "test1";

            string passphrase = Utils.GetRandomString(8);

            PgpKeyPairGenerator generator = new(_identity, passphrase.ToArray(), PublicKeyAlgorithm.RSA, PublicKeyLength.BITS_2048);

            keys = new ClientKeys
            {
                passphrase = passphrase,
                PGPKeys = generator.Generate()
            };

            try
            {
                sendHandshake();
                login();
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
            if (!File.Exists("storedKeys"))
            {
                File.WriteAllText("storedKeys", JsonConvert.SerializeObject(keys));
            }
            else {
                keys = JsonConvert.DeserializeObject<ClientKeys>(File.ReadAllText("storedKeys"));
            }
            string publicKey = keys.PGPKeys.PublicKeyRing;
            var body = new Dictionary<string, string>
            {
                ["publicKey"] = publicKey
            };
            sendMessageToServer(new Package("NA", "handshake", body));
            receiveMessageFromServer();

            keys.sessionKey = Coder.getSessionKey();
            string sessionKey = Convert.ToBase64String(keys.sessionKey);

            body = new Dictionary<string, string>
            {
                ["key"] = sessionKey
            };

            sendMessageToServer(new Package("PGP", "sessionKey", body));
            receiveMessageFromServer();
        }

        public void login () {
            Logger.Log(LogType.info1, "Enter username:");
            Logger.WriteLogs();
            string username = Console.ReadLine();
            Logger.Log(LogType.info1, "Enter password:");
            Logger.WriteLogs();
            string password = Console.ReadLine();

            var body = new Dictionary<string, string>
            {
                ["username"] = username,
                ["password"] = password
            };
            sendMessageToServer(new Package("PGP", "login", body));

        }
        
        public void HandleCommunication()
        {
            _isConnected = true;
            while (_isConnected)
            {
                Logger.Log(LogType.info1, "Type your message and press Enter to send. (Message format should match tester.json)");
                Logger.WriteLogs();

                string userMessage = readMultipleLinesFromConsole();

                sendMessageToServer(Package.FromJsonString(userMessage));
                receiveMessageFromServer();
            }
        }

        #region ENCRYPTION
        public Package decryptMessage(Package data, string mode)
        {

            string decodedBody = Coder.decode(data.body["encrypted"], mode, keys);

            data.body.Clear();
            data.body = JsonConvert.DeserializeObject<Dictionary<string, string>>(decodedBody);

            Logger.Log(LogType.info2, "decryption complete!");
            Logger.Log(LogType.info2, JsonConvert.SerializeObject(data));
            Logger.WriteLogs();

            return data;
        }

        public Package encryptData(Package data)
        {
            string temp = JsonConvert.SerializeObject(data.body);
            data.body.Clear();
            data.body["encrypted"] = Coder.encode(temp, data.encryption, keys);
            return data;
        }
        #endregion

        public async void receiveMessageFromServer()
        {
            string data = _sReader.ReadLine();
            Logger.Log(LogType.warning, "message recieved");
            Logger.WriteLogs();

            Package message = Package.FromJsonString(data);
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

        public void sendMessageToServer(Package package)
        {
            package = encryptData(package);
            _sWriter = new StreamWriter(_client.GetStream(), Encoding.ASCII);
            _sWriter.WriteLine(JsonConvert.SerializeObject(package));
            _sWriter.Flush();
            Console.WriteLine("> Sent!");
        }

        private string readMultipleLinesFromConsole()
        {
            StringBuilder sb = new StringBuilder();
            string line;
            while ((line = Console.ReadLine()) != "END")
            {
                sb.AppendLine(line);
            }
            return sb.ToString();
        }

    }
}
