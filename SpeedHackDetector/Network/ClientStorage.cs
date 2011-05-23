using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace SpeedHackDetector.Network
{
    public class ClientStorage
    {
        private ConcurrentDictionary<Socket, ClientIdentifier> m_Clients;

        private static ClientStorage m_Instance;

        public static ClientStorage GetInstance()
        {
            if (m_Instance == null)
            {
                m_Instance = new ClientStorage();
            }
            return m_Instance;
        }

        public ClientStorage()
        {
            this.m_Clients = new ConcurrentDictionary<Socket, ClientIdentifier>();
        }

        public void AddClient(ClientIdentifier client)
        {
            this.m_Clients.TryAdd(client.LocalClientSocket, client);
            this.m_Clients.TryAdd(client.RemoteClientSocket, client);
        }

        public ClientIdentifier GetClient(Socket s)
        {
            return this.m_Clients[s];
        }

        public ClientIdentifier RemoveClient(Socket s)
        {
            ClientIdentifier client;
            this.m_Clients.TryRemove(s, out client);
            return client;
        }
    }
}
