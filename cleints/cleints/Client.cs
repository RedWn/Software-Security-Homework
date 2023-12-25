using server;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Cleints
{
    class Client
    {
        private TcpClient _client;
        private StreamReader _sReader;
        private StreamWriter _sWriter;
        private byte[] _sessionKey;

        private Boolean _isConnected;

        public Client(String ipAddress, int portNum)
        {
            _client = new TcpClient();
            _client.Connect(ipAddress, portNum);
            try
            {
                HandleCommunication();
            }catch (Exception)
            {
                _isConnected = false;
                _client.Close();
            }
        }

        public void HandleCommunication()
        {
            _sReader = new StreamReader(_client.GetStream(), Encoding.ASCII);
            _sessionKey = Coder.getSessionKey();
            _isConnected = true;
            String sData;
            while (_isConnected)
            {      
                Console.Write("> Press Enter to send file data");
                Console.ReadLine();
                sData = File.ReadAllText("tester.txt");
                Package? package = packageData(sData);
                Console.WriteLine(package.body.ToString());
                package = encryptData(package, Coder.Mode.AESsecretKey); //temporary
                sendData(package);
            }
        }
        public Package packageData(string data) {
            Dictionary<string, object> dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
            Package package = new(dictionary["encryption"].ToString(), dictionary["type"].ToString());
            package.body = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(dictionary["body"].ToString());
            return package;
        }
        public Package encryptData(Package data, Coder.Mode mode) {
            string temp = Newtonsoft.Json.JsonConvert.SerializeObject(data.body);
            data.body.Clear();
            data.body["encrypted"] = Coder.encode(temp, _sessionKey, mode);
            return data;
        }
        public void sendData(Package data) {
            _sWriter = new StreamWriter(_client.GetStream(), Encoding.ASCII);
            string temp = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            _sWriter.WriteLine(temp);
            _sWriter.Flush();
            Console.Write("> Sent!");
        }
    }
}
