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
using System.Configuration;

namespace SpeedHackDetector.Proxy
{
    public abstract class Client
    {
        public Socket m_ListenSocket;
        public NetworkStream m_SendingNetworkStream;
        public NetworkStream m_ListenNetworkStream;

        protected IClientEncryption m_Encryption;
        protected bool m_Disposed;

        protected ByteQueue m_Buffer;
        protected PacketskHandler m_PacketsHander;

        private Client m_Other;
        private String[] proxyIp;

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
            proxyIp = ConfigurationManager.AppSettings.Get("proxyIp").Split('.');
            //m_Thread = new Thread(new ThreadStart(ThreadStartHander));
            //m_Thread.Name = Name;
            //m_Thread.Start();
            m_Buffer = new ByteQueue(this);
            this.m_PacketsHander = hander;
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
                        //Console.WriteLine("(((((((" + _bytesReaded +"))))))))))" + name + "\n" + print(data, _bytesReaded));
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
                ClientIdentifier client = ClientStorage.GetInstance().RemoveClient(m_ListenSocket);
                this.m_PacketsHander.DirectionFilter.remove(client);
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

                data[1] = Byte.Parse(proxyIp[0]);
                data[2] = Byte.Parse(proxyIp[1]);
                data[3] = Byte.Parse(proxyIp[2]);
                data[4] = Byte.Parse(proxyIp[3]);
                String portInByte = get16BitRepresentation(ConfigurationManager.AppSettings.Get("proxyPort"));

                Byte port1 = Convert.ToByte(portInByte.Substring(0, 8),2);
                Byte port2 = Convert.ToByte(portInByte.Substring(8, 8),2);
                data[5] = port1;
                data[6] = port2;
            }
        }

        private string get16BitRepresentation(String port)
        {
            List<char> appList = new List<char>();
            String converted = Convert.ToString(Int32.Parse(port), 2);
            char[] convertedArray = converted.ToArray<char>();
            if (convertedArray.Length < 16)
            {
                char[] _16BitArray = new char[16 - convertedArray.Length];
                for (int i = 0; i < 16 - convertedArray.Length; i++)
                {
                    _16BitArray[i] = '0';
                }
                appList.AddRange(_16BitArray);
                appList.AddRange(convertedArray);
            }
            return new String(appList.ToArray());
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

    }
}
