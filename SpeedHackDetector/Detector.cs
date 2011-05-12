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
using SpeedHackDetector.Network;
using System.Threading;

namespace SpeedHackDetector
{

    public class Detector
    {
        public static readonly bool Is64Bit = (IntPtr.Size == 8);
        private static bool m_MultiProcessor = false;
        private static AutoResetEvent m_Signal = new AutoResetEvent(true);

        public static void Set() { m_Signal.Set(); }

        public static void Main(string[] args)
        {
            int m_ProcessorCount = Environment.ProcessorCount;
            if (m_ProcessorCount > 1)
                m_MultiProcessor = true;
            if (m_MultiProcessor || Is64Bit)
                Console.WriteLine("Core: Optimizing for {0} {2}processor{1}", m_ProcessorCount, m_ProcessorCount == 1 ? "" : "s", Is64Bit ? "64-bit " : "");

            SocketPool.Create();
            PacketskHandler packetsHandler = new PacketskHandler();
            while (m_Signal.WaitOne())
            {
                
            }
        }
    }
}
