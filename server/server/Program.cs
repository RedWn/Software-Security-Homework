using server;

namespace Multi_Threaded_TCP
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.Log(LogType.info2, "Starting Multi-Threaded Server..... ");
            Logger.Log(LogType.info1, "Enter Server IP :");
            Logger.WriteLogs();
            Logger.Log(LogType.info1, "Enter Port:");

            string ip = Console.ReadLine();
            Logger.WriteLogs();
            int port = Int32.Parse(Console.ReadLine());

            TcpServer server = new TcpServer(ip, port);
        }
    }
}