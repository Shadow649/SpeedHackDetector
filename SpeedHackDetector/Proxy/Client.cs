/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using SpeedHackDetector.Network;
using SpeedHackDetector.Encryption;

namespace SpeedHackDetector.Proxy
{
    public class Client
    {
        public Socket m_ListenSocket;
        public NetworkStream m_SendingNetworkStream;
        public NetworkStream m_ListenNetworkStream;

        private IClientEncryption m_Encryption;
        private uint m_Seed;
        private bool m_Seeded;

        private ByteQueue m_Buffer;
        private NetworkHandler m_NetworkHander;
        //Thread m_Thread;
        private Client m_Other;

        private int m_PacketSeed;
        private bool m_PacketSeeded;

        public bool Seeded { get { return this.m_PacketSeeded; } set { this.m_PacketSeeded = value; } }
        public int Seed { set { this.m_PacketSeed = value; } }
        public Client Other { set { this.m_Other = value; } }

        public IClientEncryption Encryption 
        { 
            get { return this.m_Encryption; } 
            set 
            { 
                if(m_Encryption==null || !value.GetType().Equals(m_Encryption.GetType())) 
                {
                    this.m_Encryption = value; 
                    if(m_Other != null) m_Other.Encryption = value; 
                }
            } 
        }
        public Client(string Name , NetworkHandler hander)
        {
            //m_Thread = new Thread(new ThreadStart(ThreadStartHander));
            //m_Thread.Name = Name;
            //m_Thread.Start();
            m_Buffer = new ByteQueue(this);
            this.m_NetworkHander = hander;
        }

        public void ThreadStartHander(Object param)
        {
            String name = (String) param;
            try
            {
                Byte[] data = new byte[99999];
                while (m_ListenSocket.Connected)
                {
                    data = new byte[99999];
                    int _bytesReaded = m_ListenSocket.Receive(data);
                    if (_bytesReaded > 0)
                    {
                        handeServerConnection(data, _bytesReaded);
                        m_SendingNetworkStream.Write(data, 0, _bytesReaded);
                        Detector.Set();
                        if (name.Contains("local"))
                        {
                            lock (m_Buffer)
                            {
                                DecodeIncomingPacket(m_ListenSocket, ref data, ref _bytesReaded,true);
                            }
                            ByteQueue temp = new ByteQueue(this);
                            temp.Enqueue(data, 0, _bytesReaded);
                            temp.Socket = m_ListenSocket;
                            m_NetworkHander.OnReceive(temp);
                        }
                        //if (name.Contains("local"))
                          //  Console.WriteLine("(((((((" + _bytesReaded + "))))))))))" + name + "\n" + print(data, _bytesReaded));
                    }
                    else
                    {
                        m_ListenSocket.Disconnect(false);
                        m_ListenSocket.Dispose();
                        Thread.CurrentThread.Abort();
                    }
                    //Thread.Sleep(10);
                }
            }
            catch (Exception)
            {
                if (m_ListenSocket.Connected)
                {
                    m_ListenSocket.Disconnect(false);
                    m_ListenSocket.Dispose();
                }
                Thread.CurrentThread.Abort();
            }
        }

        private void handeServerConnection(byte[] data, int _bytesReaded)
        {
            if (_bytesReaded == 11 && data[0] == 140)
            {
                data[1] = 127;
                data[2] = 0;
                data[3] = 0;
                data[4] = 1;
                data[6] = 38;
            }
        }

        private string print(byte[] data, int _bytesReaded)
        {
            String res = "";
            for (int i = 0; i < _bytesReaded; i++ )
            {
                res += data[i].ToString();
            }
            return res;
        }

        public void DecodeIncomingPacket(Socket from, ref byte[] buffer, ref int length, bool ClientLocal)
        {
            #region m_Encryption != null
            //if (m_Encryption is GameEncryption && !ClientLocal && buffer[0] != 232)
            //{
              //  return;
            //}
            if (m_Encryption != null)
            {
                // If we're decrypting using LoginCrypt and we've already been relayed,
                // only decrypt a single packet using logincrypt and then disable it
                if ( m_Encryption is LoginEncryption)
                {
                    uint newSeed = ((((LoginEncryption)(m_Encryption)).Key1 + 1) ^ ((LoginEncryption)(m_Encryption)).Key2);

                    // Swap the seed
                    newSeed = ((newSeed >> 24) & 0xFF) | ((newSeed >> 8) & 0xFF00) | ((newSeed << 8) & 0xFF0000) | ((newSeed << 24) & 0xFF000000);

                    // XOR it with the old seed
                    newSeed ^= m_Seed;

                    IClientEncryption newEncryption = new GameEncryption(newSeed);

                    // Game Encryption comes first
                    newEncryption.clientDecrypt(ref buffer, length);

                    // The login encryption is still used for this one packet
                    m_Encryption.clientDecrypt(ref buffer, length);

                    // Swap the encryption schemes
                    Encryption = newEncryption;
                    m_Seed = newSeed;

                    return;
                }

                m_Encryption.clientDecrypt(ref buffer, length);
                return;
            }
            #endregion

            #region Port Scan
            //11JUN2008 RunUO SVN fix ** START ***
            // If the client did not connect on the game server port,
            // it's not our business to handle encryption for it
            //if (((IPEndPoint)from.Socket.LocalEndPoint).Port != Listener.Port) 
            //{
            //    m_Encryption = new NoEncryption();
            //    return;
            //}
            bool handle = false;

            for (int i = 0; i < Listener.EndPoints.Length; i++)
            {
                IPEndPoint ipep = (IPEndPoint)Listener.EndPoints[i];

                if (((IPEndPoint)from.LocalEndPoint).Port == ipep.Port)
                    handle = true;
            }

            if (!handle)
            {
                Encryption = new NoEncryption();
                return;
            }
            //11JUN2008 RunUO SVN fix ** END ***
            #endregion

            #region !m_Seeded
            // For simplicities sake, enqueue what we just received as long as we're not initialized
            m_Buffer.Enqueue(buffer, 0, length);
            // Clear the array
            length = 0;

            // If we didn't receive the seed yet, queue data until we can read the seed
            //if (!m_Seeded) 
            //{
            //    // Now check if we have at least 4 bytes to get the seed
            //    if (m_Buffer.Length >= 4) 
            //    {
            //        byte[] m_Peek = new byte[m_Buffer.Length];
            //        m_Buffer.Dequeue( m_Peek, 0, m_Buffer.Length ); // Dequeue everything
            //        m_Seed = (uint)((m_Peek[0] << 24) | (m_Peek[1] << 16) | (m_Peek[2] << 8) | m_Peek[3]);
            //        m_Seeded = true;

            //        Buffer.BlockCopy(m_Peek, 0, buffer, 0, 4);
            //        length = 4;
            //    } 
            //    else 
            //    {
            //        return;
            //    }
            //}
            //http://uodev.de/viewtopic.php?t=5097&postdays=0&postorder=asc&start=15&sid=dfb8e6c73b9e3eb95c1634ca3586e8a7
            //if (!m_Seeded)
            //{
            //    int seed_length = m_Buffer.GetSeedLength();

            //    if (m_Buffer.Length >= seed_length)
            //    {
            //        byte[] m_Peek = new byte[m_Buffer.Length];
            //        m_Buffer.Dequeue(m_Peek, 0, seed_length);

            //        if (seed_length == 4)
            //            m_Seed = (uint)((m_Peek[0] << 24) | (m_Peek[1] << 16) | (m_Peek[2] << 8) | m_Peek[3]);
            //        else if (seed_length == 21)
            //            m_Seed = (uint)((m_Peek[1] << 24) | (m_Peek[2] << 16) | (m_Peek[3] << 8) | m_Peek[4]);

            //        m_Seeded = true;

            //        Buffer.BlockCopy(m_Peek, 0, buffer, 0, seed_length);
            //        length = seed_length;
            //    }
            //    else
            //    {
            //        return;
            //    }
            //}

            //11JUN2008 My Version

            if (!m_Seeded)
            {
                if (m_Buffer.Length <= 3) //Short Length, try again.
                {
                    Console.WriteLine("Encryption: Failed - Short Lenght");
                    return;
                }
                //else if ((m_Buffer.Length == 83) && (m_Buffer.GetPacketID() == 239)) //New Client
                //{
                //    byte[] m_Peek = new byte[21];
                //    m_Buffer.Dequeue(m_Peek, 0, 21);

                //    m_Seed = (uint)((m_Peek[1] << 24) | (m_Peek[2] << 16) | (m_Peek[3] << 8) | m_Peek[4]);
                //    m_Seeded = true;

                //    Buffer.BlockCopy(m_Peek, 0, buffer, 0, 21);
                //    length = 21;

                //    Console.WriteLine("Encryption: Passed - New Client");
                //}

                //05MAR2009 Smjert's fix for double log in.  *** START ***
                else if ((m_Buffer.Length == 83 || m_Buffer.Length == 21) && (m_Buffer.GetPacketID() == 239)) //New Client
                {
                    length = m_Buffer.Length;
                    byte[] m_Peek = new byte[21];
                    m_Buffer.Dequeue(m_Peek, 0, 21);

                    m_Seed = (uint)((m_Peek[1] << 24) | (m_Peek[2] << 16) | (m_Peek[3] << 8) | m_Peek[4]);
                    m_Seeded = true;

                    Buffer.BlockCopy(m_Peek, 0, buffer, 0, 21);


                    Console.WriteLine("Encryption: Passed - New Client");

                    // We need to wait the next packet
                    if (length == 21)
                        return;

                    length = 21;
                }

                else if (m_Buffer.Length >= 4) //Old Client
                //05MAR2009 Smjert's fix for double log in.  *** END ***
                {
                    byte[] m_Peek = new byte[4];
                    m_Buffer.Dequeue(m_Peek, 0, 4);

                    m_Seed = (uint)((m_Peek[0] << 24) | (m_Peek[1] << 16) | (m_Peek[2] << 8) | m_Peek[3]);
                    m_Seeded = true;

                    Buffer.BlockCopy(m_Peek, 0, buffer, 0, 4);
                    length = 4;

                    Console.WriteLine("Encryption: Passed - Old Client");
                }
                else //It should never reach here.
                {
                    Console.WriteLine("Encryption: Failed - It should never reach here");
                    return;
                }
            }
            #endregion

            // If the context isn't initialized yet, that means we haven't decided on an encryption method yet
            #region m_Encryption == null
            if (m_Encryption == null)
            {
                int packetLength = m_Buffer.Length;
                int packetOffset = length;
                m_Buffer.Dequeue(buffer, length, packetLength); // Dequeue everything
                length += packetLength;

                // This is special handling for the "special" UOG packet
                if (packetLength >= 3)
                {
                    if (buffer[packetOffset] == 0xf1 && buffer[packetOffset + 1] == ((packetLength >> 8) & 0xFF) && buffer[packetOffset + 2] == (packetLength & 0xFF))
                    {
                        Encryption = new NoEncryption();
                        return;
                    }
                }

                // Check if the current buffer contains a valid login packet (62 byte + 4 byte header)
                // Please note that the client sends these in two chunks. One 4 byte and one 62 byte.
                if (packetLength == 62)
                {
                    Console.WriteLine("Checking packetLength 62 == " + packetLength);
                    // Check certain indices in the array to see if the given data is unencrypted
                    if (buffer[packetOffset] == 0x80 && buffer[packetOffset + 30] == 0x00 && buffer[packetOffset + 60] == 0x00)
                    {
                        if (Configuration.AllowUnencryptedClients)
                        {
                            Encryption = new NoEncryption();
                        }
                        
                    }
                    else
                    {
                        LoginEncryption encryption = new LoginEncryption();
                        if (encryption.init(m_Seed, buffer, packetOffset, packetLength))
                        {
                            Console.WriteLine("Client: {0}: Encrypted client detected, using keys of client {1}", "asd", encryption.Name);
                            Encryption = encryption;
                            Console.WriteLine("Encryption: Check 1");
                            byte[] packet = new byte[packetLength];
                            Console.WriteLine("Encryption: Check 2");
                            Buffer.BlockCopy(buffer, packetOffset, packet, 0, packetLength);
                            Console.WriteLine("Encryption: Check 3");
                            encryption.clientDecrypt(ref packet, packet.Length);
                            Console.WriteLine("Encryption: Check 4");
                            Buffer.BlockCopy(packet, 0, buffer, packetOffset, packetLength);
                            Console.WriteLine("Encryption: Check 5");
                            //return; //Just throwing this in.
                        }
                        else
                        {
                            Console.WriteLine("Detected an unknown client.");
                        }
                    }
                }
                else if (packetLength == 65)
                {
                    Console.WriteLine("Checking packetLength 65 == " + packetLength);
                    // If its unencrypted, use the NoEncryption class
                    if (buffer[packetOffset] == '\x91' && buffer[packetOffset + 1] == ((m_Seed >> 24) & 0xFF) && buffer[packetOffset + 2] == ((m_Seed >> 16) & 0xFF) && buffer[packetOffset + 3] == ((m_Seed >> 8) & 0xFF) && buffer[packetOffset + 4] == (m_Seed & 0xFF))
                    {
                        if (Configuration.AllowUnencryptedClients)
                        {
                            Encryption = new NoEncryption();
                        }
                        
                    }
                    else
                    {
                        // If it's not an unencrypted packet, simply assume it's encrypted with the seed
                        Encryption = new GameEncryption(m_Seed);

                        byte[] packet = new byte[packetLength];
                        Buffer.BlockCopy(buffer, packetOffset, packet, 0, packetLength);
                        m_Encryption.clientDecrypt(ref packet, packet.Length);
                        Buffer.BlockCopy(packet, 0, buffer, packetOffset, packetLength);
                    }
                }

                // If it's still not initialized, copy the data back to the queue and wait for more
                if (m_Encryption == null)
                {
                    Console.WriteLine("Encryption: Check - Waiting");
                    m_Buffer.Enqueue(buffer, packetOffset, packetLength);
                    length -= packetLength;
                    return;
                }
            }
            #endregion
        }

        public void EncodeOutgoingPacket(Socket to, ref byte[] buffer, ref int length)
        {
            if (m_Encryption != null )
            {
                m_Encryption.serverEncrypt(ref buffer, length);
                return;
            }
        }
    }
}
