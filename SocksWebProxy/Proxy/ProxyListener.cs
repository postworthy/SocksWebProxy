using Org.Mentalis.Proxy;
using Org.Mentalis.Proxy.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace com.LandonKey.SocksWebProxy.Proxy
{
    public sealed class ProxyListener : Org.Mentalis.Proxy.Http.HttpListener
    {
        public new int Port { get; private set; }
        public ProxyConfig.SocksVersion Version { get; private set; }
        private ProxyConfig Config { get; set; }
        public ProxyListener(ProxyConfig config)
            : base(config.HttpAddress, config.HttpPort)
        {
            Port = config.HttpPort;
            Version = config.Version;
            Config = config;
        }
        public override void OnAccept(IAsyncResult ar)
        {
            try
            {
                Socket NewSocket = ListenSocket.EndAccept(ar);
                if (NewSocket != null)
                {
                    ProxyClient NewClient = new ProxyClient(Config, NewSocket, new DestroyDelegate(this.RemoveClient));
                    AddClient(NewClient);
                    NewClient.StartHandshake();
                }
            }
            catch { }
            try
            {
                //Restart Listening
                ListenSocket.BeginAccept(new AsyncCallback(this.OnAccept), ListenSocket);
            }
            catch
            {
                Dispose();
            }
        }
    }
}
