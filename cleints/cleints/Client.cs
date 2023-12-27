using server;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Net.Sockets;
using System.Text;

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
            }
            catch (Exception)
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
                sendMessage(sData, context);
                receiveMessage();
            }
        }

        #region RECEIVE
        public async void receiveMessage()
        {
            string data = _sReader.ReadLine();
            Logger.Log(LogType.warning, "message recieved");
            Logger.WriteLogs();

            Package message = packageMessage(data);

            byte[] tempKey = loadTempKey(); //TODO: remove
            message = decryptMessage(message, tempKey/*_sessionKey*/, message.encryption); //still under construction
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

        public Package decryptMessage(Package data, byte[] key, string mode)
        {
            //string temp = Newtonsoft.Json.JsonConvert.SerializeObject(data.body);
            string temp = new(data.body["encrypted"]);
            data.body.Clear();
            string decodedBody = Coder.decode(temp, key, mode);
            data.body = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(decodedBody);
            return data;
        }

        public byte[] loadTempKey()
        {
            return File.ReadAllBytes("D:\\Prog\\ISSHW\\cleints\\cleints\\key.txt");
        }
        #endregion

        #region SEND
        public void sendMessage(string data, string context) {
            Package? package = packageData(data);
            package = encryptData(package, package.encryption);
            _sWriter = new StreamWriter(_client.GetStream(), Encoding.ASCII);
            _sWriter.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(package));
            _sWriter.Flush();
            Console.Write("> Sent!");
        }

        public Package packageData(string data)
        {
            Dictionary<string, object> dictionary = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
            Package package = new(dictionary["encryption"].ToString(), dictionary["type"].ToString());
            package.body = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(dictionary["body"].ToString());
            return package;
        }

        public Package encryptData(Package data, string mode)
        {
            string temp = Newtonsoft.Json.JsonConvert.SerializeObject(data.body);
            data.body.Clear();
            _sessionKey = File.ReadAllBytes("D:\\Prog\\ISSHW\\cleints\\cleints\\key.txt"); //TODO: remove this
            data.body["encrypted"] = Coder.encode(temp, _sessionKey, mode);
            return data;
        }
#endregion
    }
}
