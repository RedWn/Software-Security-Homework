using Cleints;
namespace Clients
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.Log(LogType.info1, "let's connect to Multi-Threaded TCP Server");
            Logger.Log(LogType.info2, "Enter server IP:");
            Logger.WriteLogs();
            string ip = Console.ReadLine();
            Logger.Log(LogType.info2, "Enter Port:");
            Logger.WriteLogs();
            int port = Int32.Parse(Console.ReadLine());

            Client client = new(ip, port);
        }
    }
}