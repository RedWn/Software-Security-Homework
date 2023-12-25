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
                Console.Write("Waiting for a connection...");
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
            String sData = null;
            while (client.client.Connected)
            {
                sData = client.sReader.ReadLine();
                Console.WriteLine(client.IPEndPoint.Address + " > " + sData);
                parseMessage(sData);
            }
        }

        public async void parseMessage(string message)
        {
            Console.WriteLine("message recieved");
            Request? request = Newtonsoft.Json.JsonConvert.DeserializeObject<Request>(message);
            Console.WriteLine("decoding complete!");
        }
    }
}
