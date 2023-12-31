using Newtonsoft.Json;
using Safester.CryptoLibrary.Api;
using server;
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

            sendHandshake();
            enterUser();
            HandleCommunication();


        }

        public void sendHandshake()
        {
            if (!File.Exists("storedKeys"))
            {
                File.WriteAllText("storedKeys", JsonConvert.SerializeObject(keys));
            }
            else
            {
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

        public void enterUser()
        {
            Logger.Log(LogType.info1, "Enter 1 to singup, 2 to login:");
            Logger.WriteLogs();
            string mode = Console.ReadLine();
            switch (mode)
            {
                case "1":
                    signup();
                    break;
                case "2":
                    login();
                    break;
            }
        }

        public void signup()
        {
            Logger.Log(LogType.info1, "Enter username:");
            Logger.WriteLogs();
            string username = Console.ReadLine();
            Logger.Log(LogType.info1, "Enter password:");
            Logger.WriteLogs();
            string password = Console.ReadLine();
            Logger.Log(LogType.info1, "Enter role (1 for doctor, 2 for student):");
            Logger.WriteLogs();
            string role = Console.ReadLine();
            if (role == "1")
                role = "doctor";
            else
                role = "student";

            var body = new Dictionary<string, string>
            {
                ["username"] = username,
                ["password"] = password,
                ["role"] = role
            };
            sendMessageToServer(new Package("PGP", "signup", body));
            receiveMessageFromServer();
        }

        public void login()
        {
            while (true)
            {
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
                Package reply = receiveMessageFromServer();
                if (reply.body["message"] == "success")
                {
                    break;
                }
            }
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
            string body = JsonConvert.SerializeObject(data.body);
            data.signature = Signer.SignText(keys.PGPKeys.PrivateKeyRing, keys.passphrase, body);

            data.body.Clear();
            data.body["encrypted"] = Coder.encode(body, data.encryption, keys);

            return data;
        }
        #endregion

        public Package receiveMessageFromServer()
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
                    message = decryptMessage(message, message.encryption);
                    break;
            }
            return message;
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
