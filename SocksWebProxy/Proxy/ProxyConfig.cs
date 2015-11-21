using Org.Mentalis.Network.ProxySocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace com.LandonKey.SocksWebProxy.Proxy
{
    public class ProxyConfig
    {
        public enum SocksVersion
        {
            Four,
            Five
        }
        public int HttpPort { get; set; }
        public IPAddress HttpAddress { get; set; }
        public int SocksPort { get; set; }
        public IPAddress SocksAddress { get; set; }
        public SocksVersion Version { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public ProxyTypes ProxyType
        {
            get
            {
                return (Version == SocksVersion.Five) ? ProxyTypes.Socks5 : ProxyTypes.Socks4;
            }
        }

        public ProxyConfig()
        {
            HttpAddress = IPAddress.Parse("127.0.0.1");
            HttpPort = 12345;
            SocksAddress = IPAddress.Parse("127.0.0.1");
            SocksPort = 9150;
            Version = SocksVersion.Five;
            Username = "";
            Password = "";
        }
        public ProxyConfig(IPAddress httpIP, int httpPort,IPAddress socksIP,int socksPort,SocksVersion version)
        {
            HttpAddress = httpIP;
            HttpPort = httpPort;
            SocksAddress = socksIP;
            SocksPort = socksPort;
            Version = version;
            Username = "";
            Password = "";
        }
        public ProxyConfig(IPAddress httpIP, int httpPort, IPAddress socksIP, int socksPort, SocksVersion version, string username, string password)
        {
            HttpAddress = httpIP;
            HttpPort = httpPort;
            SocksAddress = socksIP;
            SocksPort = socksPort;
            Version = version;
            Username = username;
            Password = password;
        }
    }
}
