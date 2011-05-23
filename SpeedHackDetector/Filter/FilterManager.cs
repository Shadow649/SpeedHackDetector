using System;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using SpeedHackDetector.Network;

namespace SpeedHackDetector.Filter
{
    public class FilterManager<T>
    {
        private ConcurrentDictionary<ClientIdentifier, Filter<T>> m_FilterStorage;

        public FilterManager()
        {
            m_FilterStorage = new ConcurrentDictionary<ClientIdentifier, Filter<T>>();
        }

        public void add(ClientIdentifier c, Filter<T> f) 
        {
            this.m_FilterStorage.TryAdd(c,f);
        }

        public Filter<T> get(ClientIdentifier c)
        {
            return this.m_FilterStorage[c];
        }

        public void remove(ClientIdentifier c)
        {
            Filter<T> filter;
            this.m_FilterStorage.TryRemove(c, out filter);
        }

        public ICollection<Filter<T>> Values { get { return this.m_FilterStorage.Values; } }
        

    }
}
