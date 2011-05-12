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
    public abstract class Client
    {
        public Socket m_ListenSocket;
        public NetworkStream m_SendingNetworkStream;
        public NetworkStream m_ListenNetworkStream;

        protected IClientEncryption m_Encryption;
        protected bool m_Disposed;
        static System.IO.StreamWriter file = new System.IO.StreamWriter("f:\\test.txt", true);

        protected ByteQueue m_Buffer;
        protected PacketskHandler m_NetworkHander;

        private Client m_Other;

        private int m_PacketSeed;
        private bool m_PacketSeeded;

        public bool Seeded { get { return this.m_PacketSeeded; } set { this.m_PacketSeeded = value; } }
        public int Seed { set { this.m_PacketSeed = value; } }
        public Client Other { set { this.m_Other = value; } }
        protected Byte[] data;

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
        public Client(string Name , PacketskHandler hander)
        {
            //m_Thread = new Thread(new ThreadStart(ThreadStartHander));
            //m_Thread.Name = Name;
            //m_Thread.Start();
            m_Buffer = new ByteQueue(this);
            this.m_NetworkHander = hander;
            this.m_Disposed = false;
        }

        public void ThreadStartHander(Object param)
        {
            String name = (String) param;
            try
            {
                while (m_ListenSocket.Connected)
                {
                    data = new byte[99999];
                    int _bytesReaded = m_ListenSocket.Receive(data);
                    if (_bytesReaded > 0)
                    {
                        handeServerConnection(data, _bytesReaded);
                        m_SendingNetworkStream.Write(data, 0, _bytesReaded);
                        Detector.Set();
                        doAction(_bytesReaded);
                        Console.WriteLine("(((((((" + _bytesReaded +"))))))))))" + name + "\n" + print(data, _bytesReaded));
                    }
                    else
                    {
                        Dispose();
                    }
                    //Thread.Sleep(10);
                }
                int a, b;
                ThreadPool.GetAvailableThreads(out a, out b);
                Console.WriteLine("Worker " + a + "completion" + b);
            }
            catch (Exception)
            {
                if (m_ListenSocket.Connected)
                {
                    Dispose();
                }
            }
        }

        public abstract void doAction(int readed);

        public void Dispose()
        {
            if (!m_Disposed)
            {
                m_ListenSocket.Disconnect(false);
                this.m_ListenSocket.Dispose();
                this.m_Disposed = true;
                this.m_Other.Dispose();
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
        private static class Logger
        {
            public static void log(String name, byte[] data, int _bytesReaded)
            {
                
                file.WriteLine("(((((((" + DateTime.Now.Minute + " " + DateTime.Now.Second + " " + DateTime.Now.Millisecond + "))))))))))" + name + "\n" + print(data, _bytesReaded));
            }
            private static string print(byte[] data, int _bytesReaded)
            {
                String res = "";
                for (int i = 0; i < _bytesReaded; i++)
                {
                    res += data[i].ToString();
                }
                return res;
            }
        }

    }
}
