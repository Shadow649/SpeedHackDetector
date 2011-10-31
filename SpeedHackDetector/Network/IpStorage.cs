using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Linq;
using System.IO;
using System.Text;

namespace SpeedHackDetector.Network
{
    public class IpStorage
    {
        private ConcurrentDictionary<IPAddress, List<String>> m_Ip2Account;

        public IpStorage()
        {
            this.m_Ip2Account = new ConcurrentDictionary<IPAddress, List<String>>();
            System.Timers.Timer m_Timer = new System.Timers.Timer();
            m_Timer.Elapsed += new System.Timers.ElapsedEventHandler(checkAccount);
            m_Timer.Interval = 1000;
            m_Timer.Enabled = true;
            m_Timer.AutoReset = true;
        }

        private void checkAccount(object sender, System.Timers.ElapsedEventArgs e)
        {
            foreach(IPAddress key in this.m_Ip2Account.Keys) 
            {
                List<String> acc = this.m_Ip2Account[key];
                if (acc.Count > 1)
                {
                    Warning(acc,key);
                }
            }
        }


        public void Add(String username, IPAddress addr)
        {
            Log(username, addr);
            List<String> accounts = null;
            if (!m_Ip2Account.ContainsKey(addr))
            {
                accounts = new List<String>();
            }
            else
            {
                accounts = m_Ip2Account[addr];
            }
            accounts.Add(username);
            this.m_Ip2Account.TryAdd(addr, accounts);
        }
        
        public void remove(IPAddress addr)
        {
            List<String> accounts = new List<String>();
            this.m_Ip2Account.TryRemove(addr, out accounts);
        }


        public void Warning(List<String> usernames, IPAddress addr)
        {
            String baseDirecotry = Directory.GetCurrentDirectory();
            AppendPath(ref baseDirecotry, "Logs");
            AppendPath(ref baseDirecotry, "IpLogs");
            String doppi = Path.Combine(baseDirecotry, "sospettidoppi.log");
            String accounts = getUsernameFromList(usernames);
            using (StreamWriter sw = new StreamWriter(doppi, true))
                sw.WriteLine("Accounts: " + accounts + " ha loggato con ip " + addr.ToString() + " in Data: " + DateTime.Now);
        }

        private string getUsernameFromList(IEnumerable<String> usernames)
        {
            String result = "";
            foreach (String username in usernames)
            {
                result += username +",";
            }
            return result;
        }



        public void Log(String username, IPAddress addr)
        {
            String baseDirecotry = Directory.GetCurrentDirectory();
            AppendPath(ref baseDirecotry, "Logs");
            AppendPath(ref baseDirecotry, "IpLogs");
            String generalLog = Path.Combine(baseDirecotry, "ip.log");
            String userLog = Path.Combine(baseDirecotry, String.Format("{0}.log", username));
            using (StreamWriter sw = new StreamWriter(generalLog, true))
                sw.WriteLine("Account: " + username + " ha loggato con ip " + addr.ToString() + " in Data: " + DateTime.Now);
            using (StreamWriter sw = new StreamWriter(userLog, true))
                sw.WriteLine("Account: " + username + " ha loggato con ip " + addr.ToString() + " in Data: " + DateTime.Now);
        }

        private void AppendPath(ref string path, string toAppend)
        {
            path = Path.Combine(path, toAppend);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}
