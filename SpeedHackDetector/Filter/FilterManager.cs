using System;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Net.Sockets;
using SpeedHackDetector.Network;

namespace SpeedHackDetector.Filter
{
    public class FilterManager<T>
    {
        private ConcurrentDictionary<Socket, Filter<T>> m_FilterStorage;

        public FilterManager()
        {
            m_FilterStorage = new ConcurrentDictionary<Socket, Filter<T>>();
        }

        public void add(Socket s, Filter<T> f) 
        {
            this.m_FilterStorage.TryAdd(s,f);
        }

        public Filter<T> get(Socket s)
        {
            return this.m_FilterStorage[s];
        }

    }
}
