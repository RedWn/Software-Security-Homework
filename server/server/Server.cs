using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

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
                    recieveMessage(client);
                    //Console.WriteLine(client.IPEndPoint.Address + " > " + sData);
                }catch (Exception)
                {
                    client.client.Close();
                }
            }
        }

        public async void recieveMessage(Clientte client) {
            string data = client.sReader.ReadLine();
            Logger.Log(LogType.warning, "message recieved");
            Logger.WriteLogs();

            Package message = packageMessage(data);

            message = decryptMessage(message); //still under construction
            Logger.Log(LogType.info2, "decryption complete!");
            Logger.Log(LogType.info2, Newtonsoft.Json.JsonConvert.SerializeObject(message));
            Logger.WriteLogs();
        }

        public Package packageMessage(string data)
        {
            Dictionary<string, object> dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
            Package package = new(dictionary["encryption"].ToString(), dictionary["type"].ToString());
            package.body = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(dictionary["body"].ToString());
            return package;
        }

        public Package decryptMessage(Package data) {
            string temp = Newtonsoft.Json.JsonConvert.SerializeObject(data.body);
            data.body.Clear();
            //string decodedBody = Coder.decode(temp, _sessionKey, mode);
            //data.body = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(decodedBody);
            return data;
        }
    }
}
