using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace SpeedHackDetector.Network
{
    public class ClientIdentifier
    {
        private Socket m_RemoteClient;
        private Socket m_LocalClient;
        private String m_Username;

        public ClientIdentifier(Socket remote, Socket local)
        {
            this.m_LocalClient = local;
            this.m_RemoteClient = remote;
        }

        public Socket RemoteClientSocket { get { return this.m_RemoteClient; } }

        public Socket LocalClientSocket { get { return this.m_LocalClient; } }

        public String Username { get { return this.m_Username; } set { this.m_Username = value; } }

    }
}
