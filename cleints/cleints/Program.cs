using Cleints;

namespace Clients
{
    class Program
    {
        static void Main(string[] args)
        {
            User user = new User();

            Logger.Log(LogType.info1, "let's connect to Multi-Threaded TCP Server");
            Logger.Log(LogType.info2, "Enter server IP:");
            Logger.WriteLogs();
            string ip = Console.ReadLine();
            Logger.Log(LogType.info2, "Enter Port:");
            Logger.WriteLogs();
            int port = Int32.Parse(Console.ReadLine());
            Logger.Log(LogType.info1, "Enter Youre User Name, please  ");
            Logger.WriteLogs();
            user.Name = Console.ReadLine();

            Logger.Log(LogType.info1, "Password");
            Logger.WriteLogs();
            user.Password = Console.ReadLine();

            Client client = new(ip, port, user);
        }
    }
}