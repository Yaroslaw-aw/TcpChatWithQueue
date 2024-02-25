using System.Net;

namespace Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 55555);

            ChatClient Client = new ChatClient(endPoint);

            Thread.Sleep(1000);

            await Client.Run();

            Console.ReadLine();
        }
    }
}