using com.LandonKey.SocksWebProxy;
using com.LandonKey.SocksWebProxy.Proxy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            RunParallel(1, "https://check.torproject.org/");


            // wait until the user presses enter
            Console.WriteLine("");
            Console.WriteLine("Press enter to continue...");
            Console.ReadLine();
        }

        private static void RunParallel(int count, string url)
        {
            var locker = new object();
            var proxy = new SocksWebProxy(new ProxyConfig(
                //This is an internal http->socks proxy that runs in process
                IPAddress.Parse("127.0.0.1"),
                //This is the port your in process http->socks proxy will run on
                12345,
                //This could be an address to a local socks proxy (ex: Tor / Tor Browser, If Tor is running it will be on 127.0.0.1)
                IPAddress.Parse("127.0.0.1"),
                //This is the port that the socks proxy lives on (ex: Tor / Tor Browser, Tor is 9150)
                9150,
                //This Can be Socks4 or Socks5
                ProxyConfig.SocksVersion.Five
                ));
            Enumerable.Range(1, count).ToList().AsParallel().ForAll(new Action<int>(x =>
            {
                WebClient client = new WebClient();
                client.Proxy = proxy.IsActive() ? proxy : null;
                string html = client.DownloadString(url);

                lock (locker)
                {
                    Console.WriteLine(x + ":::::::::::::::::::::");
                    Console.WriteLine("");
                    if (html.Contains("Congratulations. This browser is configured to use Tor."))
                        Console.WriteLine("Connected through Tor.");
                    else
                        Console.Write("Not connected through Tor.");
                    Console.WriteLine("");
                    Console.WriteLine(x + ":::::::::::::::::::::");
                }
            }));
        }
    }
}
