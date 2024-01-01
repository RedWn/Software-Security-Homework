using Clients;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Cmp;
using Safester.CryptoLibrary.Api;
using server;
using System.Collections;
using System.Net.Sockets;
using System.Security.Cryptography;
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
        private string _role;
        private string certificate;

        public Client(string ip, int port, int type)
        {

            _client = new TcpClient();
            _client.Connect(ip, port);
            _sReader = new StreamReader(_client.GetStream(), Encoding.ASCII);

            _identity = "test1";

            string passphrase = Utils.GetRandomString(8);

            PgpKeyPairGenerator generator = new(_identity, passphrase.ToArray(), PublicKeyAlgorithm.RSA, PublicKeyLength.BITS_2048);

            RSA rsa = RSA.Create();
            keys = new ClientKeys
            {
                passphrase = passphrase,
                PGPKeys = generator.Generate(),
                certificateRSAKey = rsa.ExportSubjectPublicKeyInfo(),
            };
            //try { 
            if (type == 1)
            {
                sendHandshake();
                enterUser();
                authCertificate();
                HandleCommunication();
            }
            else
            {
                sendCSR();
                authCertificate();
                Program.Main(Array.Empty<string>());
            }
            /*}catch (Exception e)
            {
                _isConnected = false;
                _client.Close();
            }*/
        }

        public void sendHandshake()
        {
            /*if (!File.Exists("storedKeys"))
            {
                File.WriteAllText("storedKeys", JsonConvert.SerializeObject(keys));
            }
            else
            {
                keys = JsonConvert.DeserializeObject<ClientKeys>(File.ReadAllText("storedKeys"));
            }*/
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
            Logger.Log(LogType.info1, "Enter 1 to Signup, 2 to login:");
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
            string roleInput = Console.ReadLine();

            if (roleInput == "1") _role = "doctor";
            else _role = "student";

            var body = new Dictionary<string, string>
            {
                ["username"] = username,
                ["password"] = password,
                ["role"] = _role
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
                if (reply.body["message"] == "Success")
                {
                    _role = reply.body["role"];
                    break;
                }
            }
        }

        public void sendCSR()
        {
            if (_role != "doctor")
                return;
            var body = new Dictionary<string, string>
            {
                ["publicKey"] = Encoding.UTF8.GetString(keys.certificateRSAKey)
            };
            sendMessageToServer(new Package("PGP", "CSR", body));
            Package reply = receiveMessageFromServer();
        }

        public void authCertificate()
        {
            if (_role != "doctor")
                return;
            var body = new Dictionary<string, string>
            {
                ["certificate"] = certificate
            };
            sendMessageToServer(new Package("AES", "CA", body));
        }

        public void HandleCommunication()
        {
            _isConnected = true;
            while (_isConnected)
            {
                Logger.Log(LogType.info1, "Type your message and press Enter to send. (Message format should match tester.json)");
                Logger.WriteLogs();

                string userMessage = readMultipleLinesFromConsole();

                sendMessageToServer(Package.FromJSON(userMessage));
                receiveMessageFromServer();
            }
        }

        #region ENCRYPTION
        public Package decryptMessage(Package data, string mode)
        {

            string decodedBody = Coder.decode(data.body["encrypted"], mode, keys);

            data.body.Clear();
            data.body = JsonConvert.DeserializeObject<Dictionary<string, string>>(decodedBody);

            Logger.Log(LogType.info2, $"Decryption complete: {JsonConvert.SerializeObject(data)}");
            Logger.WriteLogs();

            return data;
        }

        public Package encryptData(Package data)
        {
            string body = JsonConvert.SerializeObject(data.body);
            data.signature = Signer.SignText(keys.PGPKeys.PrivateKeyRing, keys.passphrase, body);

            data.body.Clear();
            data.body["encrypted"] = Coder.encode(body, data.encryption, keys);

            if (_role != null) data.body["role"] = _role;

            return data;
        }
        #endregion

        public Package receiveMessageFromServer()
        {
            string data = _sReader.ReadLine();
            Logger.Log(LogType.warning, "Message recieved from server");
            Logger.WriteLogs();

            Package message = Package.FromJSON(data);
            switch (message.type)
            {
                case "handshake":
                    message = decryptMessage(message, message.encryption);
                    keys.targetPublicKeyRing = message.body["publicKey"];
                    break;
                case "generic":
                    message = decryptMessage(message, message.encryption);
                    break;
                case "challenge":
                    message = decryptMessage(message, message.encryption);
                    var body = new Dictionary<string, string>
                    {
                        ["output"] = challenge(message.body["input"])
                    };
                    sendMessageToServer(new Package("PGP", "challenge", body));
                    break;
                case "certificate":
                    message = decryptMessage(message, message.encryption);
                    certificate = message.body["certificate"];
                    break;
            }
            return message;
        }

        private string challenge(string input)
        {
            int x = int.Parse(input);
            int ans = (int)(14 * Math.Pow(x, 2) + 5 * x + 3);
            return ans.ToString();
        }

        public void sendMessageToServer(Package package)
        {
            package = encryptData(package);
            _sWriter = new StreamWriter(_client.GetStream(), Encoding.ASCII);
            _sWriter.WriteLine(JsonConvert.SerializeObject(package));
            _sWriter.Flush();
            //Console.WriteLine("> Sent!");
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
