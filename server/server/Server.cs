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
            _passphrase = RandomString(8);

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
            Client client = new(obj);

            client.keys.PGPKeys = _PGPKeys;
            client.keys.passphrase = _passphrase;

            clients.Add(client);

            while (client.client.Connected)
            {
                try
                {
                    receiveMessage(client);
                }
                catch (Exception)
                {
                    client.client.Close();
                }
            }
        }

        public async void receiveMessage(Client client)
        {
            string data = client.sReader.ReadLine();
            Logger.Log(LogType.warning, client.port + " > message received");
            Logger.WriteLogs();

            Package message = packageMessage(data);

            switch (message.type)
            {
                case "handshake":
                    message = client.decryptMessage(message, message.encryption);
                    client.keys.targetPublicKeyRing = message.body["publicKey"];
                    Dictionary<string, string> body = new Dictionary<string, string>();
                    body["publicKey"] = _PGPKeys.PublicKeyRing;
                    sendMessage(client, messageBuilder("NA", "handshake", body));
                    break;
                case "sessionKey":
                    message = client.decryptMessage(message, message.encryption);
                    client.keys.sessionKey = Convert.FromBase64String(message.body["key"]);
                    body = new Dictionary<string, string>();
                    body["message"] = "Session key set!";
                    sendMessage(client, messageBuilder("AES", "generic", body));
                    break;
                case "generic":
                    message = client.decryptMessage(message, message.encryption);
                    body = new Dictionary<string, string>();
                    body["message"] = "received!";
                    sendMessage(client, messageBuilder("NA", "generic", body));
                    break;
            }
        }

        public void sendMessage(Client client, string data)
        {
            Package? package = packageMessage(data);
            package = client.encryptData(package, package.encryption);
            client.sWriter.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(package));
            client.sWriter.Flush();
            Console.WriteLine("> Sent!");
        }

        public Package packageMessage(string jsonData)
        {
            var decodedData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
            var decodedBody = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(decodedData["body"].ToString());

            string? encryption = decodedData["encryption"].ToString();
            string? type = decodedData["type"].ToString();

            Package package = new(encryption, type);
            package.body = decodedBody;

            return package;
        }

        public static string RandomString(int length)
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public string messageBuilder(string encryption, string type, Dictionary<string, string> body)
        {
            Package p = new(encryption, type);
            p.body = body;
            return Newtonsoft.Json.JsonConvert.SerializeObject(p);
        }
    }
}
