/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Collections.Concurrent;
using SpeedHackDetector.Filter;
using SpeedHackDetector.Proxy;
using System.Diagnostics;


namespace SpeedHackDetector.Network
{

    public enum Direction : byte
    {
        North = 0x0,
        Right = 0x1,
        East = 0x2,
        Down = 0x3,
        South = 0x4,
        Left = 0x5,
        West = 0x6,
        Up = 0x7,

        Mask = 0x7,
        Running = 0x80,
        ValueMask = 0x87
    }

    public class PacketskHandler
    {
        private Listener[] m_Listeners;
        private byte[] m_Peek;
        private FilterManager<Direction> m_DirectionFilterManager;
        private IpStorage m_IpStorage;

        public PacketskHandler()
        {
            this.m_DirectionFilterManager = new FilterManager<Direction>();

            this.m_IpStorage = new IpStorage();

            IPEndPoint[] ipep = Listener.EndPoints;

            m_Listeners = new Listener[ipep.Length];

            bool success = false;

            do
            {
                for (int i = 0; i < ipep.Length; i++)
                {
                    Listener l = new Listener(ipep[i],this);
                    if (!success && l != null)
                        success = true;
                    m_Listeners[i] = l;
                }
                if (!success)
                {
                    Console.WriteLine("Retrying...");
                    Thread.Sleep(10000);
                }
            } while (!success);

            m_Peek = new byte[4];
        }

        public PacketHandler GetHandler(int packetID)
        {
            switch (packetID)
            {
                case 0x02:
                    return new PacketHandler(0x02, 7, false, onMovementRequest);
                case 0x91:
                    return new PacketHandler(0x91, 65, false, onAccountLogin);
                case 0XEF:
                    return new PacketHandler(0XEF, 21, false, onLoginSeed);
                case 0x22:
                    return new PacketHandler(0x22, 3, false, Resynchronize);
                case 0xD1:
                    return new PacketHandler(0x01, 2, false, onLogoutReq);
                case 0x5D:
                    return new PacketHandler(0x5D, 73, false, onPreLogin);
                default:
                    return null;
            }

        }

        public FilterManager<Direction> DirectionFilter { get { return this.m_DirectionFilterManager; } }

        public IpStorage IpStorage { get { return this.m_IpStorage; } }

        public void onMovementRequest(ByteQueue state, PacketReader pvSrc,Socket s)
        {
            Direction dir = (Direction)pvSrc.ReadByte();
            int seq = pvSrc.ReadByte();
            int key = pvSrc.ReadInt32();
            ClientIdentifier client = ClientStorage.GetInstance().GetClient(s);
            Filter<Direction> f = m_DirectionFilterManager.get(client);
            bool speedhack = f.DoFilter(dir);
            if (speedhack)
            {
                //LOGGA SU FILE PER ORA STAMPO
                String baseDirecotry = Directory.GetCurrentDirectory();
                AppendPath(ref baseDirecotry, "Logs");
                AppendPath(ref baseDirecotry, "SpeedHack");
                baseDirecotry = Path.Combine(baseDirecotry, String.Format("{0}.log", f.Username));
                using (StreamWriter sw = new StreamWriter(baseDirecotry, true))
                    sw.WriteLine("Account: " + f.Username + " usa speedhack " + " Data: " + DateTime.Now);

                //Console.WriteLine("Account: " + f.Username + "usa speedhack" + "Data: " + DateTime.Now);
                //Console.WriteLine("THREAD ID " + Thread.CurrentThread.ManagedThreadId);
            }
            if (f.Sequence == 0 && seq != 0) 
            {
                f.Reset();
            }
            ++seq;

            if (seq == 256)
                seq = 1;

            f.Sequence = seq;

        }

        private void AppendPath(ref string path, string toAppend)
        {
            path = Path.Combine(path, toAppend);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public void onLoginSeed(ByteQueue state, PacketReader pvSrc, Socket s)
        {
            //DO NOTHING
        }

        public void onAccountLogin(ByteQueue state, PacketReader pvSrc, Socket s)
        {
            int authID = pvSrc.ReadInt32();
            String username = pvSrc.ReadString( 30 );
            FastWalk fastWalk = new FastWalk(username);
            fastWalk.Sequence = 0;
            IPEndPoint endPoint = s.RemoteEndPoint as IPEndPoint;
            this.m_IpStorage.Add(username, endPoint.Address);
            ClientIdentifier client = ClientStorage.GetInstance().GetClient(s);
            client.Username = username;
            this.m_DirectionFilterManager.add(client, fastWalk);

        }

        public void onPreLogin(ByteQueue state, PacketReader pvSrc, Socket s)
        {
            int authID = pvSrc.ReadInt32();
            String username = pvSrc.ReadString(30);
            ClientIdentifier client = ClientStorage.GetInstance().GetClient(s);
            Filter<Direction> f = m_DirectionFilterManager.get(client);
            f.start();

        }

        public void onLogoutReq(ByteQueue state, PacketReader pvSrc, Socket s)
        {
            byte [] toSend = { (byte) 0x01};
            s.Send(toSend);
            state.Sender.Dispose();
        }

        public void Resynchronize(ByteQueue state, PacketReader pvSrc, Socket s)
        {
            ClientIdentifier client = ClientStorage.GetInstance().GetClient(s);
            Filter<Direction> f = m_DirectionFilterManager.get(client);
            f.Sequence = 0;
            f.Reset();
        }


 

    }
}
