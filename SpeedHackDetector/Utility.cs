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
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Xml;
using Microsoft.Win32;
using SpeedHackDetector.Network;

namespace SpeedHackDetector
{
    public static class Utility
    {
        private static Random m_Random = new Random();
        private static Encoding m_UTF8, m_UTF8WithEncoding;

        public static Encoding UTF8
        {
            get
            {
                if (m_UTF8 == null)
                    m_UTF8 = new UTF8Encoding(false, false);

                return m_UTF8;
            }
        }

        public static Encoding UTF8WithEncoding
        {
            get
            {
                if (m_UTF8WithEncoding == null)
                    m_UTF8WithEncoding = new UTF8Encoding(true, false);

                return m_UTF8WithEncoding;
            }
        }

        public static void Separate(StringBuilder sb, string value, string separator)
        {
            if (sb.Length > 0)
                sb.Append(separator);

            sb.Append(value);
        }

        public static string Intern(string str)
        {
            if (str == null)
                return null;
            else if (str.Length == 0)
                return String.Empty;

            return String.Intern(str);
        }

        public static void Intern(ref string str)
        {
            str = Intern(str);
        }

        private static Dictionary<IPAddress, IPAddress> _ipAddressTable;

        public static IPAddress Intern(IPAddress ipAddress)
        {
            if (_ipAddressTable == null)
            {
                _ipAddressTable = new Dictionary<IPAddress, IPAddress>();
            }

            IPAddress interned;

            if (!_ipAddressTable.TryGetValue(ipAddress, out interned))
            {
                interned = ipAddress;
                _ipAddressTable[ipAddress] = interned;
            }

            return interned;
        }

        public static void Intern(ref IPAddress ipAddress)
        {
            ipAddress = Intern(ipAddress);
        }

        public static bool IsValidIP(string text)
        {
            bool valid = true;

            IPMatch(text, IPAddress.None, ref valid);

            return valid;
        }

        public static bool IPMatch(string val, IPAddress ip)
        {
            bool valid = true;

            return IPMatch(val, ip, ref valid);
        }

        public static string FixHtml(string str)
        {
            if (str == null)
                return "";

            bool hasOpen = (str.IndexOf('<') >= 0);
            bool hasClose = (str.IndexOf('>') >= 0);
            bool hasPound = (str.IndexOf('#') >= 0);

            if (!hasOpen && !hasClose && !hasPound)
                return str;

            StringBuilder sb = new StringBuilder(str);

            if (hasOpen)
                sb.Replace('<', '(');

            if (hasClose)
                sb.Replace('>', ')');

            if (hasPound)
                sb.Replace('#', '-');

            return sb.ToString();
        }

        public static bool IPMatchCIDR(string cidr, IPAddress ip)
        {
            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                return false;	//Just worry about IPv4 for now


            /*
            string[] str = cidr.Split( '/' );

            if ( str.Length != 2 )
                return false;

            /* **************************************************
            IPAddress cidrPrefix;

            if ( !IPAddress.TryParse( str[0], out cidrPrefix ) )
                return false;
             * */

            /*
            string[] dotSplit = str[0].Split( '.' );

            if ( dotSplit.Length != 4 )		//At this point and time, and for speed sake, we'll only worry about IPv4
                return false;

            byte[] bytes = new byte[4];

            for ( int i = 0; i < 4; i++ )
            {
                byte.TryParse( dotSplit[i], out bytes[i] );
            }

            uint cidrPrefix = OrderedAddressValue( bytes );

            int cidrLength = Utility.ToInt32( str[1] );
            //The below solution is the fastest solution of the three

            */

            byte[] bytes = new byte[4];
            string[] split = cidr.Split('.');
            bool cidrBits = false;
            int cidrLength = 0;

            for (int i = 0; i < 4; i++)
            {
                int part = 0;

                int partBase = 10;

                string pattern = split[i];

                for (int j = 0; j < pattern.Length; j++)
                {
                    char c = (char)pattern[j];


                    if (c == 'x' || c == 'X')
                    {
                        partBase = 16;
                    }
                    else if (c >= '0' && c <= '9')
                    {
                        int offset = c - '0';

                        if (cidrBits)
                        {
                            cidrLength *= partBase;
                            cidrLength += offset;
                        }
                        else
                        {
                            part *= partBase;
                            part += offset;
                        }
                    }
                    else if (c >= 'a' && c <= 'f')
                    {
                        int offset = 10 + (c - 'a');

                        if (cidrBits)
                        {
                            cidrLength *= partBase;
                            cidrLength += offset;
                        }
                        else
                        {
                            part *= partBase;
                            part += offset;
                        }
                    }
                    else if (c >= 'A' && c <= 'F')
                    {
                        int offset = 10 + (c - 'A');

                        if (cidrBits)
                        {
                            cidrLength *= partBase;
                            cidrLength += offset;
                        }
                        else
                        {
                            part *= partBase;
                            part += offset;
                        }
                    }
                    else if (c == '/')
                    {
                        if (cidrBits || i != 3)	//If there's two '/' or the '/' isn't in the last byte
                        {
                            return false;
                        }

                        partBase = 10;
                        cidrBits = true;
                    }
                    else
                    {
                        return false;
                    }
                }

                bytes[i] = (byte)part;
            }

            uint cidrPrefix = OrderedAddressValue(bytes);

            return IPMatchCIDR(cidrPrefix, ip, cidrLength);
        }

        public static bool IPMatchCIDR(IPAddress cidrPrefix, IPAddress ip, int cidrLength)
        {
            if (cidrPrefix == null || ip == null || cidrPrefix.AddressFamily == AddressFamily.InterNetworkV6)	//Ignore IPv6 for now
                return false;

            uint cidrValue = SwapUnsignedInt((uint)GetLongAddressValue(cidrPrefix));
            uint ipValue = SwapUnsignedInt((uint)GetLongAddressValue(ip));

            return IPMatchCIDR(cidrValue, ipValue, cidrLength);
        }

        public static bool IPMatchCIDR(uint cidrPrefixValue, IPAddress ip, int cidrLength)
        {
            if (ip == null || ip.AddressFamily == AddressFamily.InterNetworkV6)
                return false;

            uint ipValue = SwapUnsignedInt((uint)GetLongAddressValue(ip));

            return IPMatchCIDR(cidrPrefixValue, ipValue, cidrLength);
        }

        public static bool IPMatchCIDR(uint cidrPrefixValue, uint ipValue, int cidrLength)
        {
            if (cidrLength <= 0 || cidrLength >= 32)   //if invalid cidr Length, just compare IPs
                return cidrPrefixValue == ipValue;

            uint mask = uint.MaxValue << 32 - cidrLength;

            return ((cidrPrefixValue & mask) == (ipValue & mask));
        }

        private static uint OrderedAddressValue(byte[] bytes)
        {
            if (bytes.Length != 4)
                return 0;

            return (uint)((((bytes[0] << 0x18) | (bytes[1] << 0x10)) | (bytes[2] << 8)) | bytes[3]) & ((uint)0xffffffff);
        }

        private static uint SwapUnsignedInt(uint source)
        {
            return (uint)((((source & 0x000000FF) << 0x18)
            | ((source & 0x0000FF00) << 8)
            | ((source & 0x00FF0000) >> 8)
            | ((source & 0xFF000000) >> 0x18)));
        }

        public static bool TryConvertIPv6toIPv4(ref IPAddress address)
        {
            if (!Socket.OSSupportsIPv6 || address.AddressFamily == AddressFamily.InterNetwork)
                return true;

            byte[] addr = address.GetAddressBytes();
            if (addr.Length == 16)	//sanity 0 - 15 //10 11 //12 13 14 15
            {
                if (addr[10] != 0xFF || addr[11] != 0xFF)
                    return false;

                for (int i = 0; i < 10; i++)
                {
                    if (addr[i] != 0)
                        return false;
                }

                byte[] v4Addr = new byte[4];

                for (int i = 0; i < 4; i++)
                {
                    v4Addr[i] = addr[12 + i];
                }

                address = new IPAddress(v4Addr);
                return true;
            }

            return false;
        }

        public static bool IPMatch(string val, IPAddress ip, ref bool valid)
        {
            valid = true;

            string[] split = val.Split('.');

            for (int i = 0; i < 4; ++i)
            {
                int lowPart, highPart;

                if (i >= split.Length)
                {
                    lowPart = 0;
                    highPart = 255;
                }
                else
                {
                    string pattern = split[i];

                    if (pattern == "*")
                    {
                        lowPart = 0;
                        highPart = 255;
                    }
                    else
                    {
                        lowPart = 0;
                        highPart = 0;

                        bool highOnly = false;
                        int lowBase = 10;
                        int highBase = 10;

                        for (int j = 0; j < pattern.Length; ++j)
                        {
                            char c = (char)pattern[j];

                            if (c == '?')
                            {
                                if (!highOnly)
                                {
                                    lowPart *= lowBase;
                                    lowPart += 0;
                                }

                                highPart *= highBase;
                                highPart += highBase - 1;
                            }
                            else if (c == '-')
                            {
                                highOnly = true;
                                highPart = 0;
                            }
                            else if (c == 'x' || c == 'X')
                            {
                                lowBase = 16;
                                highBase = 16;
                            }
                            else if (c >= '0' && c <= '9')
                            {
                                int offset = c - '0';

                                if (!highOnly)
                                {
                                    lowPart *= lowBase;
                                    lowPart += offset;
                                }

                                highPart *= highBase;
                                highPart += offset;
                            }
                            else if (c >= 'a' && c <= 'f')
                            {
                                int offset = 10 + (c - 'a');

                                if (!highOnly)
                                {
                                    lowPart *= lowBase;
                                    lowPart += offset;
                                }

                                highPart *= highBase;
                                highPart += offset;
                            }
                            else if (c >= 'A' && c <= 'F')
                            {
                                int offset = 10 + (c - 'A');

                                if (!highOnly)
                                {
                                    lowPart *= lowBase;
                                    lowPart += offset;
                                }

                                highPart *= highBase;
                                highPart += offset;
                            }
                            else
                            {
                                valid = false;	//high & lowpart would be 0 if it got to here.
                            }
                        }
                    }
                }

                int b = (byte)(Utility.GetAddressValue(ip) >> (i * 8));

                if (b < lowPart || b > highPart)
                    return false;
            }

            return true;
        }

        public static bool IPMatchClassC(IPAddress ip1, IPAddress ip2)
        {
            return ((Utility.GetAddressValue(ip1) & 0xFFFFFF) == (Utility.GetAddressValue(ip2) & 0xFFFFFF));
        }

        #region To[Something]
        public static bool ToBoolean(string value)
        {
            bool b;
            bool.TryParse(value, out b);

            return b;
        }

        public static double ToDouble(string value)
        {
            double d;
            double.TryParse(value, out d);

            return d;
        }

        public static TimeSpan ToTimeSpan(string value)
        {
            TimeSpan t;
            TimeSpan.TryParse(value, out t);

            return t;
        }

        public static int ToInt32(string value)
        {
            int i;

            if (value.StartsWith("0x"))
                int.TryParse(value.Substring(2), NumberStyles.HexNumber, null, out i);
            else
                int.TryParse(value, out i);

            return i;
        }
        #endregion

        #region Get[Something]
        public static int GetInt32(string intString, int defaultValue)
        {
            try
            {
                return XmlConvert.ToInt32(intString);
            }
            catch
            {
                try
                {
                    return Convert.ToInt32(intString);
                }
                catch
                {
                    return defaultValue;
                }
            }
        }

        public static DateTime GetDateTime(string dateTimeString, DateTime defaultValue)
        {
            try
            {
                return XmlConvert.ToDateTime(dateTimeString, XmlDateTimeSerializationMode.Local);
            }
            catch
            {
                DateTime d;

                if (DateTime.TryParse(dateTimeString, out d))
                    return d;

                return defaultValue;
            }
        }

        public static TimeSpan GetTimeSpan(string timeSpanString, TimeSpan defaultValue)
        {
            try
            {
                return XmlConvert.ToTimeSpan(timeSpanString);
            }
            catch
            {
                return defaultValue;
            }
        }

        public static string GetAttribute(XmlElement node, string attributeName)
        {
            return GetAttribute(node, attributeName, null);
        }

        public static string GetAttribute(XmlElement node, string attributeName, string defaultValue)
        {
            if (node == null)
                return defaultValue;

            XmlAttribute attr = node.Attributes[attributeName];

            if (attr == null)
                return defaultValue;

            return attr.Value;
        }

        public static string GetText(XmlElement node, string defaultValue)
        {
            if (node == null)
                return defaultValue;

            return node.InnerText;
        }

        public static int GetAddressValue(IPAddress address)
        {
#pragma warning disable 618
            return (int)address.Address;
#pragma warning restore 618
        }

        public static long GetLongAddressValue(IPAddress address)
        {
#pragma warning disable 618
            return address.Address;
#pragma warning restore 618
        }
        #endregion

        public static double RandomDouble()
        {
            return m_Random.NextDouble();
        }


        

        

        public static object GetArrayCap(Array array, int index)
        {
            return GetArrayCap(array, index, null);
        }

        public static object GetArrayCap(Array array, int index, object emptyValue)
        {
            if (array.Length > 0)
            {
                if (index < 0)
                {
                    index = 0;
                }
                else if (index >= array.Length)
                {
                    index = array.Length - 1;
                }

                return array.GetValue(index);
            }
            else
            {
                return emptyValue;
            }
        }

        //4d6+8 would be: Utility.Dice( 4, 6, 8 )
        public static int Dice(int numDice, int numSides, int bonus)
        {
            int total = 0;
            for (int i = 0; i < numDice; ++i)
                total += Random(numSides) + 1;
            total += bonus;
            return total;
        }

        public static int RandomList(params int[] list)
        {
            return list[m_Random.Next(list.Length)];
        }

        public static bool RandomBool()
        {
            return (m_Random.Next(2) == 0);
        }

        public static int RandomMinMax(int min, int max)
        {
            if (min > max)
            {
                int copy = min;
                min = max;
                max = copy;
            }
            else if (min == max)
            {
                return min;
            }

            return min + m_Random.Next((max - min) + 1);
        }

        public static int Random(int from, int count)
        {
            if (count == 0)
            {
                return from;
            }
            else if (count > 0)
            {
                return from + m_Random.Next(count);
            }
            else
            {
                return from - m_Random.Next(-count);
            }
        }

        public static int Random(int count)
        {
            return m_Random.Next(count);
        }

        #region Random Hues

        public static int RandomNondyedHue()
        {
            switch (Random(6))
            {
                case 0: return RandomPinkHue();
                case 1: return RandomBlueHue();
                case 2: return RandomGreenHue();
                case 3: return RandomOrangeHue();
                case 4: return RandomRedHue();
                case 5: return RandomYellowHue();
            }

            return 0;
        }

        public static int RandomPinkHue()
        {
            return Random(1201, 54);
        }

        public static int RandomBlueHue()
        {
            return Random(1301, 54);
        }

        public static int RandomGreenHue()
        {
            return Random(1401, 54);
        }

        public static int RandomOrangeHue()
        {
            return Random(1501, 54);
        }

        public static int RandomRedHue()
        {
            return Random(1601, 54);
        }

        public static int RandomYellowHue()
        {
            return Random(1701, 54);
        }

        public static int RandomNeutralHue()
        {
            return Random(1801, 108);
        }

        public static int RandomSnakeHue()
        {
            return Random(2001, 18);
        }

        public static int RandomBirdHue()
        {
            return Random(2101, 30);
        }

        public static int RandomSlimeHue()
        {
            return Random(2201, 24);
        }

        public static int RandomAnimalHue()
        {
            return Random(2301, 18);
        }

        public static int RandomMetalHue()
        {
            return Random(2401, 30);
        }

        public static int ClipDyedHue(int hue)
        {
            if (hue < 2)
                return 2;
            else if (hue > 1001)
                return 1001;
            else
                return hue;
        }

        public static int RandomDyedHue()
        {
            return Random(2, 1000);
        }

        //[Obsolete( "Depreciated, use the methods for the Mobile's race", false )]
        public static int ClipSkinHue(int hue)
        {
            if (hue < 1002)
                return 1002;
            else if (hue > 1058)
                return 1058;
            else
                return hue;
        }

        //[Obsolete( "Depreciated, use the methods for the Mobile's race", false )]
        public static int RandomSkinHue()
        {
            return Random(1002, 57) | 0x8000;
        }

        //[Obsolete( "Depreciated, use the methods for the Mobile's race", false )]
        public static int ClipHairHue(int hue)
        {
            if (hue < 1102)
                return 1102;
            else if (hue > 1149)
                return 1149;
            else
                return hue;
        }

        //[Obsolete( "Depreciated, use the methods for the Mobile's race", false )]
        public static int RandomHairHue()
        {
            return Random(1102, 48);
        }

        #endregion

        

      

        public static ArrayList BuildArrayList(IEnumerable enumerable)
        {
            IEnumerator e = enumerable.GetEnumerator();

            ArrayList list = new ArrayList();

            while (e.MoveNext())
            {
                list.Add(e.Current);
            }

            return list;
        }

        public static void FormatBuffer(TextWriter output, Stream input, int length)
        {
            output.WriteLine("        0  1  2  3  4  5  6  7   8  9  A  B  C  D  E  F");
            output.WriteLine("       -- -- -- -- -- -- -- --  -- -- -- -- -- -- -- --");

            int byteIndex = 0;

            int whole = length >> 4;
            int rem = length & 0xF;

            for (int i = 0; i < whole; ++i, byteIndex += 16)
            {
                StringBuilder bytes = new StringBuilder(49);
                StringBuilder chars = new StringBuilder(16);

                for (int j = 0; j < 16; ++j)
                {
                    int c = input.ReadByte();

                    bytes.Append(c.ToString("X2"));

                    if (j != 7)
                    {
                        bytes.Append(' ');
                    }
                    else
                    {
                        bytes.Append("  ");
                    }

                    if (c >= 0x20 && c < 0x80)
                    {
                        chars.Append((char)c);
                    }
                    else
                    {
                        chars.Append('.');
                    }
                }

                output.Write(byteIndex.ToString("X4"));
                output.Write("   ");
                output.Write(bytes.ToString());
                output.Write("  ");
                output.WriteLine(chars.ToString());
            }

            if (rem != 0)
            {
                StringBuilder bytes = new StringBuilder(49);
                StringBuilder chars = new StringBuilder(rem);

                for (int j = 0; j < 16; ++j)
                {
                    if (j < rem)
                    {
                        int c = input.ReadByte();

                        bytes.Append(c.ToString("X2"));

                        if (j != 7)
                        {
                            bytes.Append(' ');
                        }
                        else
                        {
                            bytes.Append("  ");
                        }

                        if (c >= 0x20 && c < 0x80)
                        {
                            chars.Append((char)c);
                        }
                        else
                        {
                            chars.Append('.');
                        }
                    }
                    else
                    {
                        bytes.Append("   ");
                    }
                }

                output.Write(byteIndex.ToString("X4"));
                output.Write("   ");
                output.Write(bytes.ToString());
                output.Write("  ");
                output.WriteLine(chars.ToString());
            }
        }

        private static Stack<ConsoleColor> m_ConsoleColors = new Stack<ConsoleColor>();

        public static void PushColor(ConsoleColor color)
        {
            try
            {
                m_ConsoleColors.Push(Console.ForegroundColor);
                Console.ForegroundColor = color;
            }
            catch
            {
            }
        }

        public static void PopColor()
        {
            try
            {
                Console.ForegroundColor = m_ConsoleColors.Pop();
            }
            catch
            {
            }
        }

        public static bool NumberBetween(double num, int bound1, int bound2, double allowance)
        {
            if (bound1 > bound2)
            {
                int i = bound1;
                bound1 = bound2;
                bound2 = i;
            }

            return (num < bound2 + allowance && num > bound1 - allowance);
        }

    }
}