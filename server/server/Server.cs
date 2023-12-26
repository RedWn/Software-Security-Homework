using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Formats.Asn1;
using Newtonsoft.Json;
using Safester.CryptoLibrary.Api;

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
               Logger.Log(LogType.info2,"Waiting for a connection...");
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
                }catch (Exception)
                {
                    client.client.Close();
                }
            }
        }

        #region RECEIVE
        public async void receiveMessage(Clientte client) {
            string data = client.sReader.ReadLine();
            client.user = JsonConvert.DeserializeObject<User>(data);
            
            Logger.Log(LogType.warning, "message received");
            Logger.WriteLogs();
            Console.WriteLine(data);
            Package message = packageMessage(client.user.Message);

            byte[] tempKey = loadTempKey();
            message = decryptMessage(message, tempKey/*client.sessionKey*/, message.encryption); //still under construction
            Logger.Log(LogType.info2, "decryption complete!");
            Logger.Log(LogType.info2, Newtonsoft.Json.JsonConvert.SerializeObject(message));
            Logger.WriteLogs();
            sendMessage(client, "{\"encryption\":\"AES\",\"type\":\"ACK\",\"body\":{\"message\": \"received\"}}", "","AES"); //test only
        }

        public Package packageMessage(string data)
        {
            Dictionary<string, object> dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
            Package package = new(dictionary["encryption"].ToString(), dictionary["type"].ToString());
            package.body = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(dictionary["body"].ToString());
            return package;
        }

        public Package decryptMessage(Package data, byte[] key, string mode) {
            //string temp = Newtonsoft.Json.JsonConvert.SerializeObject(data.body);
            string temp = new(data.body["encrypted"]);
            data.body.Clear();
            string decodedBody = Coder.decode(temp, key, mode);
            data.body = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(decodedBody);
            return data;
        }

        public byte[] loadTempKey()
        {
            return File.ReadAllBytes("key.txt");
        }
        #endregion

        #region SEND
        public void sendMessage(Clientte client, string data, string context, string mode)
        {
            Package? package = packageData(data);
            byte[] tempKey = loadTempKey();
            package = encryptData(package, tempKey/*client.sessionKey*/, mode); //TODO: remove
            client.sWriter.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(package));
            client.sWriter.Flush();
            Console.Write("> Sent!");
        }

        public Package packageData(string data)
        {
            Dictionary<string, object> dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
            Package package = new(dictionary["encryption"].ToString(), dictionary["type"].ToString());
            package.body = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(dictionary["body"].ToString());
            return package;
        }

        public Package encryptData(Package data, byte[] key, string mode)
        {
            string temp = Newtonsoft.Json.JsonConvert.SerializeObject(data.body);
            data.body.Clear();
            data.body["encrypted"] = Coder.encode(temp, key, mode);
            return data;
        }
        #endregion
    }
}
