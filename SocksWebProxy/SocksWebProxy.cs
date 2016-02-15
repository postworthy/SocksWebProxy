using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Org.Mentalis.Proxy.Http;
using Org.Mentalis.Network.ProxySocket;
using com.LandonKey.SocksWebProxy.Proxy;

namespace com.LandonKey.SocksWebProxy
{
    public class SocksWebProxy : IWebProxy
    {
        static readonly object locker = new object();
		static readonly List<ProxyListener> listeners = new List<ProxyListener>();
        static bool allowBypass;

        void GetListener(ProxyConfig config, bool allowBypass = true)
        {   
            lock(locker)
            {
                SocksWebProxy.allowBypass = allowBypass;
				ProxyListener listener = listeners.Where(x => x.Port == config.HttpPort).FirstOrDefault();
                if(listener == null)
                {
                    listener = new ProxyListener(config);
                    listener.Start();
                    listeners.Add(listener);
                }
                if (listener.Version != config.Version) 
                    throw new Exception("Socks Version Mismatch for Port " + config.HttpPort);
            }
        }

        ProxyConfig Config { get; set; }

        /// <summary>
        /// Creates a new SocksWebProxy
        /// </summary>
        /// <param name="config">Proxy settings</param>
        /// <param name="allowBypass">Whether to allow bypassing the proxy server. 
        /// The current implementation to allow bypassing the proxy server requiers elevated privileges. 
        /// If you want to use the library in an environment with limited privileges (like Azure Websites or Azure Webjobs), set allowBypass = false</param>
        /// <returns></returns>
        public SocksWebProxy(ProxyConfig config = null, bool allowBypass = true)
        {
			if (config == null)
				config = ProxyConfig.Settings ?? new ProxyConfig();

			Config = config;
            GetListener(config, allowBypass);
        }

        ICredentials cred = null;
        public virtual ICredentials Credentials
        {
            get { return cred; }
            set { cred = value; }
        }

        public Uri GetProxy(Uri destination)
        {
            return new Uri("http://127.0.0.1:" + Config.HttpPort);
        }

        /// <summary>
        /// Indicates whether to use the proxy server for the specified host.
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public bool IsBypassed(Uri host)
        {
            if (allowBypass)
            {
                return !IsActive();
            }
            return false;
        }

        public bool IsActive()
        {
            var isSocksPortListening = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(x => x.Port == Config.SocksPort);
            return isSocksPortListening;
        }
    }
}
