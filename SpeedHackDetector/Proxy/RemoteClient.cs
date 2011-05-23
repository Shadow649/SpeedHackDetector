using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using SpeedHackDetector.Filter;
using SpeedHackDetector.Network;

namespace SpeedHackDetector.Proxy
{
    public class RemoteClient : Client
    {
        private  readonly byte[] RAW_WORLD_SAVE_END_PACKET = new byte[] {8,9,3,9,8,8,1,7,7,9,8,1,9,7,1,3,9,1,6,2,1,1,2,1,2,2,8,2,1,4,1,1,5,1,8,2
            ,1,1,1,5,6,3,2,0,0,0,0,0,2,8,2,2,2,1,3,6,1,7,5,5,4,1,6,0,1,1,6,1,1,8,1,8,3,5,5,1,4,3,1,1,4,5,3,1,7,8,8,7,6
            ,2,8,6,1,0,3,3,7,1,5,3};

        private String WORLD_SAVE_FINISHED = "save has took";
        public RemoteClient(string p, Network.PacketskHandler m_Handler) : base(p,m_Handler)
        {
        }
        public override void doAction(int readed)
        {
            EncodeOutgoingPacket(m_ListenSocket, ref data, ref readed);
            int newLenght = 0;
            byte[] asd = Decompressor.Decompressor.Decompress(data, out newLenght);
            int packetId = asd[0];

            if (packetId == 0x1C)
            {
                int usernameLength = getIndex(asd, newLenght);
                String username = UnicodeEncoding.ASCII.GetString(asd, 14, usernameLength).Replace((char)7, '?');
                String message = UnicodeEncoding.ASCII.GetString(asd, 44, newLenght - 44).Replace((char)7, '?');
                Console.WriteLine("(((((((" + readed + "))))))))))" + "REMOTE" + "\n" + "NAME: " + username + " MESSAGE: " + message);

                if (worldSaveFinished(username,message ))
                {
                    ClientIdentifier client = ClientStorage.GetInstance().GetClient(m_ListenSocket);
                    Filter<Direction> f = m_PacketsHander.DirectionFilter.get(client);
                    f.Reset();
                }
            }

        }

        private int getIndex(byte[] asd, int length)
        {
            int res = 0;
            for (int i = 13; i < length; i++)
            {
                if (asd[i] == 0)
                {
                    res = i - 13;
                    break;
                }
            }
            return res;
        }

        private bool worldSaveFinished(string username, string message)
        {
            return username.ToLower().Equals("") && message.ToLower().Contains("has took");
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
