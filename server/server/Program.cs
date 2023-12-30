using server;

namespace Multi_Threaded_TCP
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.Log(LogType.info2, "[-- Server --]");
            Logger.Log(LogType.info1, "Enter Server IP (default 127.0.0.1):");
            Logger.WriteLogs();

            string ip = Console.ReadLine();
            if (ip == "") ip = "127.0.0.1";

            Logger.Log(LogType.info1, "Enter Port (default 5001):");
            Logger.WriteLogs();
            string portString = Console.ReadLine();
            int port = int.Parse(portString == "" ? "5001" : portString);

            TcpServer server = new(ip, port);
            server.AcceptConnections();
        }
    }
}