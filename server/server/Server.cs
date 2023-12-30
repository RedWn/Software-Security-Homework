using Newtonsoft.Json;
using Safester.CryptoLibrary.Api;
using System.Net;
using System.Net.Sockets;

namespace server
{
    class TcpServer
    {
        private bool _isRunning;
        private string _passphrase;

        private TcpListener _server;
        private List<Client> clients;
        private PgpKeyPairHolder _PGPKeys;

        public TcpServer(string ip, int port)
        {
            clients = new List<Client>();
            _passphrase = getRandomString(8);

            _server = new TcpListener(IPAddress.Parse(ip), port);
            _server.Start();

            _isRunning = true;

            string _identity = "server";

            PgpKeyPairGenerator generator = new(_identity, _passphrase.ToArray(), PublicKeyAlgorithm.RSA, PublicKeyLength.BITS_2048);
            _PGPKeys = generator.Generate();
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

            clients.Add(client);

            while (client.client.Connected)
            {
                try
                {
                    ReceiveClientMessage(client);
                }
                catch (Exception)
                {
                    client.client.Close();
                }
            }
        }

        public async void ReceiveClientMessage(Client client)
        {
            Logger.Log(LogType.warning, client.port + " > message received");
            Logger.WriteLogs();

            string data = client.sReader.ReadLine();

            Package message = PackageClientData(data);
            message = client.DecryptPackageBody(message);

            switch (message.type)
            {
                case "handshake":
                    client.keys.targetPublicKeyRing = message.body["publicKey"];
                    var body = new Dictionary<string, string>
                    {
                        ["publicKey"] = _PGPKeys.PublicKeyRing
                    };
                    sendMessage(client, serializePackage("NA", "handshake", body));
                    break;
                case "sessionKey":
                    client.keys.sessionKey = Convert.FromBase64String(message.body["key"]);
                    body = new Dictionary<string, string>
                    {
                        ["message"] = "Session key set!"
                    };
                    sendMessage(client, serializePackage("AES", "generic", body));
                    break;
                case "generic":
                    body = new Dictionary<string, string>
                    {
                        ["message"] = "received!"
                    };
                    sendMessage(client, serializePackage("NA", "generic", body));
                    break;
            }
        }

        public void sendMessage(Client client, string data)
        {
            Package? package = PackageClientData(data);
            package = client.EncryptPackageBody(package);
            client.sWriter.WriteLine(JsonConvert.SerializeObject(package));
            client.sWriter.Flush();
            Console.WriteLine("> Sent!");
        }

        public Package PackageClientData(string jsonData)
        {
            var decodedData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
            var decodedBody = JsonConvert.DeserializeObject<Dictionary<string, string>>(decodedData["body"].ToString());

            string? encryption = decodedData["encryption"].ToString();
            string? type = decodedData["type"].ToString();

            return new Package(encryption, type, decodedBody);
        }

        private static string getRandomString(int length)
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private string serializePackage(string encryption, string type, Dictionary<string, string> body)
        {
            return JsonConvert.SerializeObject(new Package(encryption, type, body));
        }
    }
}
