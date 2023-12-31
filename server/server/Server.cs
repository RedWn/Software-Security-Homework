using Newtonsoft.Json;
using Safester.CryptoLibrary.Api;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace server
{
    class TcpServer
    {
        private bool _isRunning;
        private string _passphrase;
        private string _identity;

        private TcpListener _server;
        private List<Client> _clients;
        private PgpKeyPairHolder _PGPKeys;

        public TcpServer(string ip, int port)
        {
            _clients = new List<Client>();

            _passphrase = Utils.GetRandomString(8);
            _identity = "server";

            _server = new TcpListener(IPAddress.Parse(ip), port);
            _server.Start();

            _isRunning = true;

            PgpKeyPairGenerator generator = new(_identity, _passphrase.ToArray(), PublicKeyAlgorithm.RSA, PublicKeyLength.BITS_2048);
            _PGPKeys = generator.Generate();
            if (!File.Exists("publickeys"))
            {
                Dictionary<string, DBEntry> stub = new()
                {
                    ["username"] = new("role", "public key", "password")
                };
                File.WriteAllText("publickeys", JsonConvert.SerializeObject(stub));
            }
        }

        public void AcceptConnections()
        {
            while (_isRunning)
            {
                Logger.Log(LogType.info2, "Waiting for a connection...");
                Logger.WriteLogs();

                TcpClient newClient = _server.AcceptTcpClient();
                Console.WriteLine("Connected!");

                Thread t = new(new ParameterizedThreadStart(HandleClientConnection));
                t.Start(newClient);
            }
        }

        public void HandleClientConnection(object obj)
        {
            Client client = new((TcpClient)obj);

            client.keys.PGPKeys = _PGPKeys;
            client.keys.passphrase = _passphrase;

            _clients.Add(client);

            while (client.client.Connected)
            {
                ReceiveMessageFromClient(client);
            }
        }

        public Package ReceiveMessageFromClient(Client client)
        {
            Logger.Log(LogType.warning, client.port + " > message received");
            Logger.WriteLogs();

            string data = client.sReader.ReadLine();

            Package message = Package.FromJsonString(data);
            message = client.DecryptPackageBody(message);

            if (message.signature != null)
            {
                bool isSignatureVerified = Signer.VerifySignature(client.keys.PGPKeys.PublicKeyRing, JsonConvert.SerializeObject(message.body), message.signature);
                // TODO: Here kick user if signature is invalid.
            }


            switch (message.type)
            {
                case "handshake":
                    client.keys.targetPublicKeyRing = message.body["publicKey"];
                    var body = new Dictionary<string, string>
                    {
                        ["publicKey"] = _PGPKeys.PublicKeyRing
                    };
                    SendMessageToClient(client, new Package("NA", "handshake", body));
                    break;
                case "sessionKey":
                    client.keys.sessionKey = Convert.FromBase64String(message.body["key"]);
                    body = new Dictionary<string, string>
                    {
                        ["message"] = "Session key set!"
                    };
                    SendMessageToClient(client, new Package("AES", "generic", body));
                    break;
                case "signup":
                    addKeyToFile(message.body["username"], message.body["role"], client.keys.PGPKeys.PublicKeyRing, message.body["password"]);
                    body = new Dictionary<string, string>
                    {
                        ["message"] = "User Added!"
                    };
                    SendMessageToClient(client, new Package("AES", "generic", body));
                    break;
                case "login":
                    if (checkPassword(message.body["username"], message.body["password"]))
                    {
                        body = new Dictionary<string, string>
                        {
                            ["message"] = "success",
                            ["role"] = getRole(message.body["username"]),
                        };
                        SendMessageToClient(client, new Package("AES", "generic", body));
                    }
                    else
                    {
                        body = new Dictionary<string, string>
                        {
                            ["message"] = "wrong password"
                        };
                        SendMessageToClient(client, new Package("AES", "generic", body));
                    }
                    break;
                case "generic":
                    body = new Dictionary<string, string>
                    {
                        ["message"] = "Received!"
                    };
                    SendMessageToClient(client, new Package("NA", "generic", body));
                    break;
            }
            return message;
        }

        public void addKeyToFile(string username, string role, string key, string password)
        {
            Dictionary<string, DBEntry> keys = JsonConvert.DeserializeObject<Dictionary<string, DBEntry>>(File.ReadAllText("publickeys"));
            keys[username] = new(role, key, Convert.ToBase64String(Encoding.UTF8.GetBytes(password)));
            File.Delete("publickeys");
            File.WriteAllText("publickeys", JsonConvert.SerializeObject(keys));
        }

        public bool checkPassword(string username, string password)
        {
            Dictionary<string, DBEntry> keys = JsonConvert.DeserializeObject<Dictionary<string, DBEntry>>(File.ReadAllText("publickeys"));
            return (Convert.ToBase64String(Encoding.UTF8.GetBytes(password)) == keys[username].password);
        }

        public string getRole(string username)
        {
            Dictionary<string, DBEntry> keys = JsonConvert.DeserializeObject<Dictionary<string, DBEntry>>(File.ReadAllText("publickeys"));
            return keys[username].role;
        }

        public void SendMessageToClient(Client client, Package package)
        {
            package = client.EncryptPackageBody(package);
            client.sWriter.WriteLine(JsonConvert.SerializeObject(package));
            client.sWriter.Flush();
            Console.WriteLine("> Sent!");
        }
    }
}
