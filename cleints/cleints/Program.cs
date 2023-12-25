using Cleints;
namespace Clients
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("let's connect to Multi-Threaded TCP Server");
            Console.WriteLine("Enter server IP:");
            String ip = Console.ReadLine();

            Console.WriteLine("Enter Port:");
            int port = Int32.Parse(Console.ReadLine());

            Client client = new(ip, port);
        }
    }
}