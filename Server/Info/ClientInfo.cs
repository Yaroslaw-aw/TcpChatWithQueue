using System.Net.Sockets;

namespace Server.Info
{
    internal class ClientInfo
    {
        internal string Id { get; init; }

        internal TcpClient Client;

        public ClientInfo(string id, TcpClient client)
        {
            Id = id;
            Client = client;
        }
    }
}
