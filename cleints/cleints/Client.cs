﻿using System;
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

    private Boolean _isConnected;

    public Client(String ipAddress, int portNum)
    {
        _client = new TcpClient();
        _client.Connect(ipAddress, portNum);

        HandleCommunication();
    }

    public void HandleCommunication()
    {
        _sReader = new StreamReader(_client.GetStream(), Encoding.ASCII);
        _sWriter = new StreamWriter(_client.GetStream(), Encoding.ASCII);

        _isConnected = true;
        String sData = null;
        while (_isConnected)
        {
            Console.Write("> ");
            sData = Console.ReadLine();
            _sWriter.WriteLine(sData);
            _sWriter.Flush();
        }
    }
} 
}