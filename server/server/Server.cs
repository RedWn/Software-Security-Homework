using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Tls;
using Safester.CryptoLibrary.Api;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace server
{
    class TcpServer
    {
        private bool _isRunning;
        private string _identity;
        private string _passphrase;

        private TcpListener _server;
        private List<Connection> _clients;
        private Connection _CA;
        private PgpKeyPairHolder _PGPKeys;

        private string _currentUser;

        public TcpServer(string ip, int port)
        {
            _clients = new List<Connection>();

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
                Logger.Log(LogType.info2, "Waiting for connections...");
                Logger.WriteLogs();

                TcpClient newClient = _server.AcceptTcpClient();

                Logger.Log(LogType.info2, $"Client connected from {newClient.Client.LocalEndPoint}");
                Logger.WriteLogs();

                Thread t = new(new ParameterizedThreadStart(HandleClientConnection));
                t.Start(newClient);
            }
        }

        public void HandleClientConnection(object obj)
        {
            Connection client = new((TcpClient)obj);

            client.keys.PGPKeys = _PGPKeys;
            client.keys.passphrase = _passphrase;

            _clients.Add(client);

            while (client.client.Connected)
            {
                try
                {
                    ReceiveMessage(client);
                }
                catch (Exception e)
                {
                    client.client.Close();
                }
            }
        }

        public void ReceiveMessage(Connection client)
        {
            Logger.Log(LogType.warning, $"Message received from {client.port}");
            Logger.WriteLogs();

            string data = client.sReader.ReadLine();

            Package message = Package.FromJSON(data);
            message = client.DecryptPackageBody(message);

            if (message.signature != null && message.body != null && message.body.TryGetValue("role", out string? value) && value == "doctor")
            {
                Console.WriteLine("User is Doctor.");
                bool isSignatureVerified = Signer.VerifySignature(client.keys.PGPKeys.PublicKeyRing, JsonConvert.SerializeObject(message.body), message.signature);
                if (!isSignatureVerified)
                {
                    var body = new Dictionary<string, string>
                    {
                        ["message"] = "Invalid signature"
                    };
                    SendMessage(client, new Package("NA", "generic", body));

                    return;
                }
            }


            switch (message.type)
            {
                case "handshake":
                    client.keys.targetPublicKeyRing = message.body["publicKey"];
                    var body = new Dictionary<string, string>
                    {
                        ["publicKey"] = _PGPKeys.PublicKeyRing
                    };
                    SendMessage(client, new Package("NA", "handshake", body));
                    break;
                case "sessionKey":
                    client.keys.sessionKey = Convert.FromBase64String(message.body["key"]);
                    body = new Dictionary<string, string>
                    {
                        ["message"] = "Session key set!"
                    };
                    SendMessage(client, new Package("AES", "generic", body));
                    break;
                case "signup":
                    addKeyToFile(message.body["username"], message.body["role"], client.keys.PGPKeys.PublicKeyRing, message.body["password"]);
                    _currentUser = message.body["username"];
                    body = new Dictionary<string, string>
                    {
                        ["message"] = "User Added!"
                    };
                    SendMessage(client, new Package("AES", "generic", body));
                    break;
                case "login":
                    if (checkPassword(message.body["username"], message.body["password"]))
                    {
                        _currentUser = message.body["username"];
                        body = new Dictionary<string, string>
                        {
                            ["message"] = "Success",
                            ["role"] = getRole(message.body["username"]),
                        };
                        SendMessage(client, new Package("AES", "generic", body));
                    }
                    else
                    {
                        body = new Dictionary<string, string>
                        {
                            ["message"] = "Wrong password"
                        };
                        SendMessage(client, new Package("AES", "generic", body));
                    }
                    break;
                case "generic":
                    body = new Dictionary<string, string>
                    {
                        ["message"] = "Received!"
                    };
                    SendMessage(client, new Package("NA", "generic", body));
                    break;
                case "CA":
                    RSA rsa = RSA.Create();
                    if (true)
                    {
                        body = new Dictionary<string, string>
                        {
                            ["massege"] = "Auth Succeed"
                        };
                        Console.WriteLine("YES");
                        SendMessage(client, new Package("NA", "generic", body));
                    }
                    else
                    {
                        body = new Dictionary<string, string>
                        {
                            ["massege"] = "Auth Failed"
                        };
                        Console.WriteLine("NO");

                        SendMessage(client, new Package("NA", "generic", body));
                    }
                    break;
            }
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
            try
            {
                Dictionary<string, DBEntry> keys = JsonConvert.DeserializeObject<Dictionary<string, DBEntry>>(File.ReadAllText("publickeys"));
                return (Convert.ToBase64String(Encoding.UTF8.GetBytes(password)) == keys[username].password);
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public string getRole(string username)
        {
            Dictionary<string, DBEntry> keys = JsonConvert.DeserializeObject<Dictionary<string, DBEntry>>(File.ReadAllText("publickeys"));
            return keys[username].role;
        }

        public void SendMessage(Connection client, Package package)
        {
            package = client.EncryptPackageBody(package);
            client.sWriter.WriteLine(JsonConvert.SerializeObject(package));
            client.sWriter.Flush();
            //Console.WriteLine("> Sent!");
        }
    }
}
