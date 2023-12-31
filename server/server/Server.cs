using Newtonsoft.Json;
using Safester.CryptoLibrary.Api;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

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
            if (!File.Exists("publickeys")) {
                Dictionary<string, string> stub = new Dictionary<string, string>
                {
                    ["username"] = "public key"
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
                try
                {
                    ReceiveMessageFromClient(client);
                }
                catch (Exception)
                {
                    client.client.Close();
                }
            }
        }

        public async void ReceiveMessageFromClient(Client client)
        {
            Logger.Log(LogType.warning, client.port + " > message received");
            Logger.WriteLogs();

            string data = client.sReader.ReadLine();

            Package message = Package.FromJsonString(data);
            message = client.DecryptPackageBody(message);

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
                case "login":
                    addKeyToFile(message.body["username"], client.keys.PGPKeys.PublicKeyRing);        
                    body = new Dictionary<string, string>
                    {
                        ["message"] = "User Added!"
                    };
                    SendMessageToClient(client, new Package("AES", "generic", body));
                    break;
                case "generic":
                    body = new Dictionary<string, string>
                    {
                        ["message"] = "Received!"
                    };
                    SendMessageToClient(client, new Package("NA", "generic", body));
                    break;
            }
        }

        public void addKeyToFile(string username, string key) {
            Dictionary<string, string> keys = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("publickeys"));
            keys[username] = key;
            File.Delete("publickeys");
            File.WriteAllText("publickeys", JsonConvert.SerializeObject(keys));
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
