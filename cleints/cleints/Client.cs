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
            while (_isConnected)
            {      
                Logger.Log(LogType.info1, "Press Enter to send file data");
                Logger.WriteLogs();
                Console.ReadLine();
                string projectDir = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
                string sData = File.ReadAllText(projectDir + "\\tester.txt");
                string context="";
                //the context should signify why is the message being sent
                sendMessage(sData, context, Coder.Mode.AESsecretKey);
            }
        }

        public void sendMessage(string data, string context, Coder.Mode mode) {
            Package? package = packageData(data);
            package = encryptData(package, mode);
            _sWriter = new StreamWriter(_client.GetStream(), Encoding.ASCII);
            _sWriter.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(package));
            _sWriter.Flush();
            Console.Write("> Sent!");
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
    }
}
