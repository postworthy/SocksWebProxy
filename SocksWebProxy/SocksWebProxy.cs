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
    public class SocksWebProxy:IWebProxy
    {
        private static object locker = new object();
        private static List<ProxyListener> listeners;

        private ProxyListener GetListener(ProxyConfig config)
        {   
            lock(locker)
            {
                if (listeners == null)
                    listeners = new List<ProxyListener>();

                var listener = listeners.Where(x => x.Port == config.HttpPort).FirstOrDefault();

                if(listener == null)
                {
                    listener = new ProxyListener(config);
                    listener.Start();
                    listeners.Add(listener);
                }

                if (listener.Version != config.Version) 
                    throw new Exception("Socks Version Mismatch for Port " + config.HttpPort);

                return listener;
            }
        }

        private ProxyConfig Config { get; set; }

        public SocksWebProxy(ProxyConfig config = null)
        {
            Config = config;
            GetListener(config);
        }
        private ICredentials cred = null;
        public ICredentials Credentials
        {
            get
            {
                return cred;
            }
            set
            {
                cred = value;
            }
        }

        public Uri GetProxy(Uri destination)
        {
            return new Uri("http://127.0.0.1:" + Config.HttpPort);
        }

        public bool IsBypassed(Uri host)
        {
            return !IsActive();
        }

        public bool IsActive()
        {
            var isSocksPortListening = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(x => x.Port == Config.SocksPort);
            return isSocksPortListening;
        }
    }
}
