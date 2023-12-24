using server;

namespace Multi_Threaded_TCP
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Multi-Threaded Server");
            Console.WriteLine("Enter server IP:");
            String ip = Console.ReadLine();

            Console.WriteLine("Enter Port:");
            int port = Int32.Parse(Console.ReadLine());
            TcpServer server = new TcpServer(ip,port);
        }
    }
}