/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Net.Sockets;

namespace SpeedHackDetector.Network
{
    public delegate void OnPacketReceive(ByteQueue state, PacketReader pvSrc, Socket s);

    public class PacketHandler
    {
        private int m_PacketID;
        private int m_Length;
        private bool m_Ingame;
        private OnPacketReceive m_OnReceive;

        public PacketHandler(int packetID, int length, bool ingame, OnPacketReceive onReceive)
        {
            m_PacketID = packetID;
            m_Length = length;
            m_Ingame = ingame;
            m_OnReceive = onReceive;
        }

        public int PacketID
        {
            get
            {
                return m_PacketID;
            }
        }

        public int Length
        {
            get
            {
                return m_Length;
            }
        }

        public OnPacketReceive OnReceive
        {
            get
            {
                return m_OnReceive;
            }
        }


        public bool Ingame
        {
            get
            {
                return m_Ingame;
            }
        }
    }
}