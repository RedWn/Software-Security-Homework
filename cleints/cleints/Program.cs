using Cleints;
namespace Clients
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("let's connect to Multi-Threaded TCP Server");
            Console.WriteLine("Provide us with your  IP:");
            String ip = Console.ReadLine();

            Console.WriteLine("Provide Port:");
            int port = Int32.Parse(Console.ReadLine());

            Client client = new Client(ip, port);
        }
    }
}