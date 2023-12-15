using server;

namespace Multi_Threaded_TCP
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Multi-Threaded Server");
            TcpServer server = new TcpServer(25);
        }
    }
}