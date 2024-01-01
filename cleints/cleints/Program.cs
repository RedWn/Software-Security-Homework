using Cleints;

namespace Clients
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Client";
            Logger.Log(LogType.info1, "Enter Server IP (default 127.0.0.1):", false);
            Logger.WriteLogs();

            string ip = Console.ReadLine();
            if (ip == "") ip = "127.0.0.1";

            Logger.Log(LogType.info1, "Enter Port (default 5001):", false);
            Logger.WriteLogs();
            string portString = Console.ReadLine();
            int port = int.Parse(portString == "" ? "5001" : portString);

            Client client = new(ip, port);

            // Certificate certificate = new Certificate();
            // certificate.csrClient();
        }
    }
}