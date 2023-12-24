using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    internal class Clientte
    {
        public TcpClient client;
        public IPEndPoint IPEndPoint;
        public StreamWriter sWriter;
        public StreamReader sReader;

        public Clientte(object obj)
        {
            client = (TcpClient)obj;
            IPEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
            sWriter = new StreamWriter(client.GetStream(), Encoding.ASCII);
            sReader = new StreamReader(client.GetStream(), Encoding.ASCII);
        }
    }
}
