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

            Package message = Package.FromClientData(data);
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
            var package = Package.FromClientData(data);
            package = client.EncryptPackageBody(package);
            client.sWriter.WriteLine(JsonConvert.SerializeObject(package));
            client.sWriter.Flush();
            Console.WriteLine("> Sent!");
        }
        private string serializePackage(string encryption, string type, Dictionary<string, string> body)
        {
            return JsonConvert.SerializeObject(new Package(encryption, type, body));
        }
    }
}
