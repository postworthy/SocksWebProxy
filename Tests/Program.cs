using com.LandonKey.SocksWebProxy;
using com.LandonKey.SocksWebProxy.Proxy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tests
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
			int runs = 10;
#else
			int runs = 3;
#endif
			RunParallel(runs, "https://check.torproject.org/");
#if DEBUG
			// wait until the user presses enter
			Console.WriteLine("");
            Console.WriteLine("Press enter to continue...");
            Console.ReadLine();
#endif
		}

		static readonly object _locker = new object();

		private static void RunParallel(int count, string url)
        {
			var proxy = new SocksWebProxy();
			Enumerable.Range(0, count).ToList().ForEach(new Action<int>(x =>
            {
                if (x != 0) Thread.Sleep(5000);
                WebClient client = new WebClient();
                client.Proxy = proxy;
                var doc = new HtmlAgilityPack.HtmlDocument();
                var html = client.DownloadString(url);
                doc.LoadHtml(html);
                var nodes = doc.DocumentNode.SelectNodes("//p/strong");
                IPAddress ip;
                foreach(var node in nodes)
                {
                    if(IPAddress.TryParse(node.InnerText, out ip))
                    {

                        lock (_locker)
                        {
                            Console.WriteLine(x + ":::::::::::::::::::::");
                            Console.WriteLine("");
                            if (html.Contains("Congratulations. This browser is configured to use Tor."))
                                Console.WriteLine("Connected through Tor with IP: " + ip.ToString());
                            else
                                Console.Write("Not connected through Tor with IP: " + ip.ToString());
                            Console.WriteLine("");
                            Console.WriteLine(x + ":::::::::::::::::::::");
                        }
                        return;
                    }
                }

                lock (_locker)
                {
                    Console.WriteLine(x + ":::::::::::::::::::::");
                    Console.WriteLine("");
                    Console.Write("IP not found");
                    Console.WriteLine("");
                    Console.WriteLine(x + ":::::::::::::::::::::");
                }
                
            }));
        }
    }
}
