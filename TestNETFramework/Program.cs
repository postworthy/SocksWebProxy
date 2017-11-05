using com.LandonKey.SocksWebProxy;
using com.LandonKey.SocksWebProxy.Proxy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tests
{
    class Program
    {
        private static IPAddress oldIp;
        static void Main(string[] args)
        {
            RunParallel(10, "https://check.torproject.org/");


            // wait until the user presses enter
            Console.WriteLine("");
            Console.WriteLine("Press enter to continue...");
            Console.ReadLine();
        }

        private static int GetNextFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();

            return port;
        }

        private static void RunParallel(int count, string url)
        {
            var locker = new object();

            for (int a = 0; a < count; a++)
            {
                //if (a != 0)
                //{
                //    Thread.Sleep(2000);
                //}

                var proxy = new SocksWebProxy(new ProxyConfig(
                    //This is an internal http->socks proxy that runs in process
                    IPAddress.Parse("127.0.0.1"),
                    //This is the port your in process http->socks proxy will run on
                    GetNextFreePort(),
                    //This could be an address to a local socks proxy (ex: Tor / Tor Browser, If Tor is running it will be on 127.0.0.1)
                    IPAddress.Parse("127.0.0.1"),
                    //This is the port that the socks proxy lives on (ex: Tor / Tor Browser, Tor is 9150)
                    9150,
                    //This Can be Socks4 or Socks5
                    ProxyConfig.SocksVersion.Five
                    ));

                proxy = null;
                GC.Collect();

                int counter = 0;
                WebClient client = new WebClient();
                client.Headers.Add("Cache-Control", "no-cache");
                client.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.BypassCache);
                //client.Proxy = proxy.IsActive() ? proxy : null;
                client.Proxy = proxy;
                var doc = new HtmlAgilityPack.HtmlDocument();
                var html = client.DownloadString(url);
                doc.LoadHtml(html);
                var nodes = doc.DocumentNode.SelectNodes("//p/strong");
                IPAddress ip;
                foreach (var node in nodes)
                {
                    try
                    {
                        if (IPAddress.TryParse(node.InnerText, out ip))
                        {
                            //lock (locker)
                            //{
                            if (oldIp != null)
                            {
                                {
                                    while (oldIp.ToString() != ip.ToString())
                                    {
                                        counter++;
                                        html = client.DownloadString(url + "?random=" + counter);
                                        doc.LoadHtml(html);
                                        nodes = doc.DocumentNode.SelectNodes("//p/strong");
                                        int index = nodes.FirstOrDefault().InnerText.IndexOf("<");
                                        if (index != -1)
                                            IPAddress.TryParse(nodes.FirstOrDefault().InnerText.Substring(0, index),
                                                out ip);
                                        else
                                        {
                                            IPAddress.TryParse(nodes.FirstOrDefault().InnerText, out ip);
                                        }
                                    }
                                }

                            }
                            oldIp = ip;
                            Console.WriteLine(a + ":::::::::::::::::::::");
                            Console.WriteLine("");
                            if (html.Contains("Congratulations. This browser is configured to use Tor."))
                            {
                                Console.WriteLine("Connected through Tor with IP: " + ip.ToString());
                                // Connect to tor, get a new identity and drop existing circuits
                                Socket server = null;
                                try
                                {
                                    //Authenticate using control password
                                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9151);
                                    server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                                    server.Connect(endPoint);
                                    server.Send(
                                        Encoding.ASCII.GetBytes("AUTHENTICATE \"password\"" + Environment.NewLine));
                                    byte[] data = new byte[1024];
                                    int receivedDataLength = server.Receive(data);
                                    string stringData = Encoding.ASCII.GetString(data, 0, receivedDataLength);

                                    //Request a new Identity
                                    server.Send(Encoding.ASCII.GetBytes("SIGNAL NEWNYM" + Environment.NewLine));
                                    data = new byte[1024];
                                    receivedDataLength = server.Receive(data);
                                    stringData = Encoding.ASCII.GetString(data, 0, receivedDataLength);
                                    if (!stringData.Contains("250"))
                                    {
                                        Console.WriteLine("Unable to signal new user to server.");
                                        server.Shutdown(SocketShutdown.Both);
                                        server.Close();
                                    }
                                    else
                                    {
                                        Console.WriteLine("SIGNAL NEWNYM sent successfully");
                                    }

                                    //Enable circuit events to enable console output
                                    server.Send(Encoding.ASCII.GetBytes("setevents circ" + Environment.NewLine));
                                    data = new byte[1024];
                                    receivedDataLength = server.Receive(data);
                                    stringData = Encoding.ASCII.GetString(data, 0, receivedDataLength);

                                    //Get circuit information
                                    server.Send(Encoding.ASCII.GetBytes("getinfo circuit-status" + Environment.NewLine));
                                    data = new byte[16384];
                                    receivedDataLength = server.Receive(data);
                                    stringData = Encoding.ASCII.GetString(data, 0, receivedDataLength);
                                    stringData = stringData.Substring(stringData.IndexOf("250+circuit-status"),
                                        stringData.IndexOf("250 OK") - stringData.IndexOf("250+circuit-status"));
                                    var stringArray = stringData.Split(new[] { "\r\n" }, StringSplitOptions.None);
                                    foreach (var s in stringArray)
                                    {
                                        if (s.Contains("BUILT"))
                                        {
                                            //Close any existing circuit in order to get a new IP
                                            var circuit = s.Substring(0, s.IndexOf("BUILT")).Trim();
                                            server.Send(
                                                Encoding.ASCII.GetBytes($"closecircuit {circuit}" + Environment.NewLine));
                                            data = new byte[1024];
                                            receivedDataLength = server.Receive(data);
                                            stringData = Encoding.ASCII.GetString(data, 0, receivedDataLength);
                                        }
                                    }
                                }
                                finally
                                {
                                    server.Shutdown(SocketShutdown.Both);
                                    server.Close();
                                }
                            }
                            else
                            {
                                Console.Write("Not connected through Tor with IP: " + ip.ToString());
                            }
                            Console.WriteLine("");
                            Console.WriteLine(a + ":::::::::::::::::::::");
                        }
                        //}
                        else
                        {
                            //lock (locker)
                            //{
                            Console.WriteLine(a + ":::::::::::::::::::::");
                            Console.WriteLine("");
                            Console.Write("IP not found");
                            Console.WriteLine("");
                            Console.WriteLine(a + ":::::::::::::::::::::");
                            //}
                        }
                    }
                    catch { }
                }
            }
        }
    }
}

