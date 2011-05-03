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
using System.Collections.Concurrent;
using SpeedHackDetector.Filter;

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

    public class NetworkHandler
    {
        private ConcurrentDictionary<IPAddress, FastWalk> m_Ip2AccountStorage;
        private Listener[] m_Listeners;
        private Queue<ByteQueue> m_Queue;
        private Queue<ByteQueue> m_WorkingQueue;
        private byte[] m_Peek;

        public NetworkHandler()
        {
            m_Ip2AccountStorage = new ConcurrentDictionary<IPAddress, FastWalk>();
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

            m_Queue = new Queue<ByteQueue>();
            m_WorkingQueue = new Queue<ByteQueue>();
            m_Peek = new byte[4];
        }

        public void Slice()
        {
            CheckListener();

            lock (this)
            {
                Queue<ByteQueue> temp = m_WorkingQueue;
                m_WorkingQueue = m_Queue;
                m_Queue = temp;
            }

            while (m_WorkingQueue.Count > 0)
            {
                ByteQueue ns = m_WorkingQueue.Dequeue();
                HandleReceive(ns);
            }
        }
        private const int BufferSize = 4096;
        private BufferPool m_Buffers = new BufferPool("Processor", 4, BufferSize);

        public bool HandleReceive(ByteQueue ns)
        {
            ByteQueue buffer = ns;

            if (buffer == null || buffer.Length <= 0)
                return true;

            lock (buffer)
            {
                int length = buffer.Length;

                    if (buffer.Length <= 4)
                    {
                        return true;
                    }

                while (length > 0)
                {
                    int packetID = buffer.GetPacketID();

                    PacketHandler handler = GetHandler(packetID);

                    if (handler == null)
                    {
                        return false;   
                    }

                    int packetLength = handler.Length;

                    if (packetLength <= 0)
                    {
                        if (length >= 3)
                        {
                            packetLength = buffer.GetPacketLength();

                            if (packetLength < 3)
                            {
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (length >= packetLength)
                    {


                        byte[] packetBuffer;

                        if (BufferSize >= packetLength)
                            packetBuffer = m_Buffers.AcquireBuffer();
                        else
                            packetBuffer = new byte[packetLength];

                        packetLength = buffer.Dequeue(packetBuffer, 0, packetLength);

                        PacketReader r = new PacketReader(packetBuffer, packetLength, handler.Length != 0);

                        handler.OnReceive(ns, r, ns.Socket);
                        length = buffer.Length;

                        if (BufferSize >= packetLength)
                            m_Buffers.ReleaseBuffer(packetBuffer);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return true;
        }

        private void CheckListener()
        {
            for (int j = 0; j < m_Listeners.Length; ++j)
            {
                Socket[] accepted = m_Listeners[j].Slice();

                for (int i = 0; i < accepted.Length; ++i)
                {
                    
                }
            }
        }

        public void OnReceive(ByteQueue ns)
        {
            lock (this)
                m_Queue.Enqueue(ns);

            Detector.Set();
        }
        internal PacketHandler GetHandler(int packetID)
        {
            switch (packetID)
            {
                case 0x02:
                    return new PacketHandler(0x02, 7, false, onMovementRequest);
                case 0X80:
                    return new PacketHandler(0x80, 62, false, onAccountLogin);
                case 0XEF:
                    return new PacketHandler(0XEF, 21, false, onLoginSeed);
                case 0x22:
                    return new PacketHandler(0x22, 3, false, Resynchronize);
                default:
                    return null;
            }

        }

        public void onMovementRequest(ByteQueue state, PacketReader pvSrc,Socket s)
        {
            IPAddress ip = ((IPEndPoint)s.RemoteEndPoint).Address;
            Direction dir = (Direction)pvSrc.ReadByte();
            int seq = pvSrc.ReadByte();
            int key = pvSrc.ReadInt32();
            FastWalk f = this.m_Ip2AccountStorage[ip];
            bool speedhack = f.checkFastWalk(dir);
            if (f.Sequence == 0 && seq != 0) 
            {
                f.ClearFastwalkStack();
            }
            ++seq;

            if (seq == 256)
                seq = 1;

            f.Sequence = seq;

        }

        public void onLoginSeed(ByteQueue state, PacketReader pvSrc, Socket s)
        {
            //DO NOTHING
        }

        public void onAccountLogin(ByteQueue state, PacketReader pvSrc, Socket s)
        {
            IPAddress ip = ((IPEndPoint)s.RemoteEndPoint).Address;
            String username = pvSrc.ReadString( 30 );
            FastWalk fastWalk = new FastWalk(username);
            fastWalk.Sequence = 0;
            this.m_Ip2AccountStorage.TryAdd(ip, fastWalk);

        }

        public void onAccountLogedOut(ByteQueue state, PacketReader pvSrc, Socket s)
        {

        }

        public void Resynchronize(ByteQueue state, PacketReader pvSrc, Socket s)
        {
            IPAddress ip = ((IPEndPoint)s.RemoteEndPoint).Address;
            FastWalk f = this.m_Ip2AccountStorage[ip];
            f.Sequence = 0;
            f.ClearFastwalkStack();
        }


 

    }
}
