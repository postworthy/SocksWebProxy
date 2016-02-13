using Org.Mentalis.Network.ProxySocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using System.Configuration;

namespace com.LandonKey.SocksWebProxy.Proxy
{
    public class ProxyConfig : ConfigurationSection
    {
		//lazy singleton
		private class Inner { internal static readonly ProxyConfig SINGLETON = ConfigurationManager.GetSection("SocksWebProxy") as ProxyConfig; }
		public static ProxyConfig Settings { get { return Inner.SINGLETON; } }

		public enum SocksVersion
        {			
            Four,
            Five,
		}

		const int DEFAULT_HTTP_PORT = 12345;
		[ConfigurationProperty("HttpPort", DefaultValue = DEFAULT_HTTP_PORT)]
        public virtual int HttpPort
		{
			get { return (int)this["HttpPort"]; }
			set
			{
				if (value <= 0 || value > ushort.MaxValue)
					throw new ArgumentOutOfRangeException("HttpPort");

				this["HttpPort"] = value;
			}
		}

		const string LOCAL_HOST = "127.0.0.1";
		[ConfigurationProperty("HttpAddress", DefaultValue = LOCAL_HOST)]
		public virtual string HttpAddress
		{
			get { return this["HttpAddress"] as string; }
			set
			{
				if (string.IsNullOrWhiteSpace(value))
					throw new ArgumentException("HttpAddress is null or blank");

				this["HttpAddress"] = value.Trim();
			}
		}

		public virtual IPAddress HttpIPAddress
		{
			get { return IPAddress.Parse(HttpAddress); }
			set
			{
				if (value == null)
					throw new ArgumentNullException("HttpAddress");

				HttpAddress = value.ToString();
			}
		}

		const int DEFAULT_SOCKS_PORT = 9150;
		[ConfigurationProperty("SocksPort", DefaultValue = DEFAULT_SOCKS_PORT)]
		public virtual int SocksPort
		{
			get { return (int)this["SocksPort"]; }
			set
			{
				if (value <= 0 || value > ushort.MaxValue)
					throw new ArgumentOutOfRangeException("SocksPort");

				this["SocksPort"] = value;
			}
		}

		[ConfigurationProperty("SocksAddress", DefaultValue = LOCAL_HOST)]
		public virtual string SocksAddress
		{
			get { return this["SocksAddress"] as string; }
			set
			{
				if (string.IsNullOrWhiteSpace(value))
					throw new ArgumentException("SocksAddress is null or empty.");

				this["SocksAddress"] = value.Trim();
			}
		}

		public virtual IPAddress SocksIPAddress
		{
			get { return IPAddress.Parse(SocksAddress); }
			set
			{
				if (value == null)
					throw new ArgumentNullException("SocksAddress");

				SocksAddress = value.ToString();
			}
		}

		const SocksVersion DEFAULT_VERSION = SocksVersion.Five;
		[ConfigurationProperty("Version", DefaultValue = DEFAULT_VERSION)]
		public virtual SocksVersion Version
		{
			get { return (SocksVersion)this["Version"]; }
			set { this["Version"] = value; }
		}

		[ConfigurationProperty("Username", DefaultValue = "")]
		public virtual string Username
		{
			get { return this["Username"] as string; }
			set
			{
				this["Username"] = value ?? string.Empty;
			}
		}

		[ConfigurationProperty("Password", DefaultValue = "")]
		public virtual string Password
		{
			get { return this["Password"] as string; }
			set
			{
				this["Password"] = value ?? string.Empty;
			}
		}

        public virtual ProxyTypes ProxyType
        {
            get
            {
                return (Version == SocksVersion.Five) ? ProxyTypes.Socks5 : ProxyTypes.Socks4;
            }
        }

        public ProxyConfig()
        {
			HttpPort = DEFAULT_HTTP_PORT;
			HttpAddress = LOCAL_HOST;
			SocksPort = DEFAULT_SOCKS_PORT;
			SocksAddress = LOCAL_HOST;
			Version = DEFAULT_VERSION;
		}
        public ProxyConfig(IPAddress httpIP, int httpPort,IPAddress socksIP,int socksPort,SocksVersion version)
        {
            HttpIPAddress = httpIP;
            HttpPort = httpPort;
            SocksIPAddress = socksIP;
            SocksPort = socksPort;
            Version = version;
        }
        public ProxyConfig(IPAddress httpIP, int httpPort, IPAddress socksIP, int socksPort, SocksVersion version, string username, string password)
        {
            HttpIPAddress = httpIP;
            HttpPort = httpPort;
            SocksIPAddress = socksIP;
            SocksPort = socksPort;
            Version = version;
            Username = username;
            Password = password;
        }
    }
}
