/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using SpeedHackDetector.Proxy;
using System.Configuration;
using System.Collections.Specialized;

namespace SpeedHackDetector.Network
{
    public class Listener : IDisposable
    {
        private static int i = 0;
        private Socket m_Listener;

        private Queue<Socket> m_Accepted;
        private object m_AcceptedSyncRoot;

        private AsyncCallback m_OnAccept;
        private PacketskHandler m_Handler;

        private static Socket[] m_EmptySockets = new Socket[0];

        private static IPEndPoint[] m_EndPoints = new IPEndPoint[] {
			new IPEndPoint( IPAddress.Any, 2598 )
		};

        public static IPEndPoint[] EndPoints
        {
            get { return m_EndPoints; }
            set { m_EndPoints = value; }
        }

        public Listener(IPEndPoint ipep, PacketskHandler handler)
        {
            m_Accepted = new Queue<Socket>();
            m_AcceptedSyncRoot = ((ICollection)m_Accepted).SyncRoot;
            m_OnAccept = new AsyncCallback(OnAccept);
            m_Listener = Bind(ipep);
            this.m_Handler = handler;
        }

        private Socket Bind(IPEndPoint ipep)
        {
            Socket s = SocketPool.AcquireSocket();

            try
            {
                s.LingerState.Enabled = false;
                s.ExclusiveAddressUse = false;

                s.Bind(ipep);
                s.Listen(8);

                if (ipep.Address.Equals(IPAddress.Any))
                {
                    try
                    {
                        Console.WriteLine("Listening: {0}:{1}", IPAddress.Loopback, ipep.Port);

                        IPHostEntry iphe = Dns.GetHostEntry(Dns.GetHostName());

                        IPAddress[] ip = iphe.AddressList;

                        for (int i = 0; i < ip.Length; ++i)
                            Console.WriteLine("Listening: {0}:{1}", ip[i], ipep.Port);
                    }
                    catch { }
                }
                else
                {
                    Console.WriteLine("Listening: {0}:{1}", ipep.Address, ipep.Port);
                }

                IAsyncResult res = s.BeginAccept(SocketPool.AcquireSocket(), 0, m_OnAccept, s);

                return s;
            }
            catch (Exception e)
            {
                if (e is SocketException)
                {
                    SocketException se = (SocketException)e;

                    if (se.ErrorCode == 10048)
                    { // WSAEADDRINUSE
                        Console.WriteLine("Listener Failed: {0}:{1} (In Use)", ipep.Address, ipep.Port);
                    }
                    else if (se.ErrorCode == 10049)
                    { // WSAEADDRNOTAVAIL
                        Console.WriteLine("Listener Failed: {0}:{1} (Unavailable)", ipep.Address, ipep.Port);
                    }
                    else
                    {
                        Console.WriteLine("Listener Exception:");
                        Console.WriteLine(e);
                    }
                }

                return null;
            }
        }

        private void OnAccept(IAsyncResult asyncResult)
        {
            Socket listener = (Socket)asyncResult.AsyncState;

            Socket accepted = null;

            try
            {
                accepted = listener.EndAccept(asyncResult);
            }
            catch (SocketException ex)
            {
                throw new NotImplementedException("GESTISCI L'ECCEZIONE",ex);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            if (accepted != null)
            {
                if (handleSocket(accepted))
                {
                    Enqueue(accepted);
                }
                else
                {
                    Release(accepted);
                }
            }

            try
            {
                listener.BeginAccept(SocketPool.AcquireSocket(), 0, m_OnAccept, listener);
            }
            catch (SocketException ex)
            {
                throw new NotImplementedException("GESTISCI L'ECCEZIONE",ex);
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private bool handleSocket(Socket socket)
        {
            try
            {
                NetworkStream m_NetworkStreamLocal = new NetworkStream(socket);

                TcpClient m_RemoteSocket = new TcpClient(ConfigurationManager.AppSettings.Get("serverIp"), Int32.Parse(ConfigurationManager.AppSettings.Get("serverPort")));
                NetworkStream m_NetworkStreamRemote = m_RemoteSocket.GetStream();

                Client _RemoteClient = new RemoteClient("remote" + i, m_Handler)
                {
                    m_SendingNetworkStream = m_NetworkStreamLocal,
                    m_ListenNetworkStream = m_NetworkStreamRemote,
                    m_ListenSocket = m_RemoteSocket.Client
                };

                Client _LocalClient = new LocalClient("local" + i, m_Handler)
                {
                    m_SendingNetworkStream = m_NetworkStreamRemote,
                    m_ListenNetworkStream = m_NetworkStreamLocal,
                    m_ListenSocket = socket
                };

                _RemoteClient.Other = _LocalClient;
                _LocalClient.Other = _RemoteClient;
                ThreadPool.QueueUserWorkItem(_RemoteClient.ThreadStartHander, "remote" + i);
                ThreadPool.QueueUserWorkItem(_LocalClient.ThreadStartHander, "local" + i);
                i++;
                ClientIdentifier client = new ClientIdentifier(m_RemoteSocket.Client,socket);
                ClientStorage.GetInstance().AddClient(client);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        private void Enqueue(Socket socket)
        {
            lock (m_AcceptedSyncRoot)
            {
                m_Accepted.Enqueue(socket);
            }

            Detector.Set();
        }

        private void Release(Socket socket)
        {
            try
            {
                socket.Shutdown(SocketShutdown.Both);
            }
            catch (SocketException ex)
            {
                throw new NotImplementedException("GESTISCI L'ECCEZIONE",ex);
            }

            try
            {
                socket.Close();

                //SocketPool.ReleaseSocket(socket);
            }
            catch (SocketException ex)
            {
                throw new NotImplementedException("GESTISCI L'ECCEZIONE",ex);
            }
        }

        public void Dispose()
        {
            Socket socket = Interlocked.Exchange<Socket>(ref m_Listener, null);

            if (socket != null)
            {
                socket.Close();
            }
        }
    }
}