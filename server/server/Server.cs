using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Formats.Asn1;

namespace server
{
    class TcpServer
    {
        private TcpListener _server;
        private Boolean _isRunning;
        private List<Clientte> clients;

        public TcpServer(string ip, int port)
        {
            clients = new List<Clientte>();
            IPAddress localAddr = IPAddress.Parse(ip);
            _server = new TcpListener(localAddr, port);
            _server.Start();

            _isRunning = true;

            LoopClients();
        }

        public void LoopClients()
        {
            while (_isRunning)
            {
                Logger.Log(LogType.info2, "Waiting for a connection...");
                Logger.WriteLogs();
                TcpClient newClient = _server.AcceptTcpClient();
                Console.WriteLine("Connected!");
                Thread t = new(new ParameterizedThreadStart(HandleClient));
                t.Start(newClient);
            }
        }

        public void HandleClient(object obj)
        {
            Clientte client = new(obj);
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

        public async void receiveMessage(Clientte client)
        {
            string data = client.sReader.ReadLine();
            Logger.Log(LogType.warning, "message received");
            Logger.WriteLogs();

            Package message = packageMessage(data);
            switch (message.type)
            {
                case "handshake":
                    message = client.decryptMessage(message, message.encryption);
                    client.sessionKey = Convert.FromBase64String(message.body["publicKey"]);
                    sendMessage(client, "{\"encryption\":\"NA\",\"type\":\"handshake\",\"body\":{\"publicKey\": \"key set\"}}", "NA"); //test only
                    break;
                case "sessionKey":
                    message = client.decryptMessage(message, message.encryption);
                    client.sessionKey = Convert.FromBase64String(message.body["key"]);
                    sendMessage(client, "{\"encryption\":\"NA\",\"type\":\"generic\",\"body\":{\"message\": \"key set\"}}", "NA"); //test only
                    break;
                case "generic":
                    message = client.decryptMessage(message, message.encryption);
                    sendMessage(client, "{\"encryption\":\"NA\",\"type\":\"generic\",\"body\":{\"message\": \"received\"}}", "NA"); //test only
                    break;
            }
        }

        public void sendMessage(Clientte client, string data string mode)
        {
            Package? package = packageMessage(data);
            package = client.encryptData(package, mode);
            client.sWriter.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(package));
            client.sWriter.Flush();
            Console.Write("> Sent!");
        }

        public Package packageMessage(string data)
        {
            Dictionary<string, object> dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
            Package package = new(dictionary["encryption"].ToString(), dictionary["type"].ToString());
            package.body = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(dictionary["body"].ToString());
            return package;
        }
    }
}
