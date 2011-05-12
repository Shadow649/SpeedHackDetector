using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace SpeedHackDetector.Proxy
{
    public class RemoteClient : Client
    {

        public RemoteClient(string p, Network.PacketskHandler m_Handler) : base(p,m_Handler)
        {
        }
        public override void doAction(int readed)
        {
            EncodeOutgoingPacket(m_ListenSocket, ref data, ref readed);
            int newLenght = 0;
            byte[] asd = Decompressor.Decompressor.Decompress(data, out newLenght);
            Console.WriteLine("(((((((" + readed + "))))))))))" + "REMOTE" + "\n" + UnicodeEncoding.ASCII.GetString(asd, 0, readed).Replace((char)7, '?'));
        }

        private void EncodeOutgoingPacket(Socket to, ref byte[] buffer, ref int length)
        {
            if (m_Encryption != null)
            {
                m_Encryption.serverEncrypt(ref buffer, length);
                return;
            }
        }
    }
}
