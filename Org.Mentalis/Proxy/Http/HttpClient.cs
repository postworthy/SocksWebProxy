/*
    Copyright © 2002, The KPD-Team
    All rights reserved.
    http://www.mentalis.org/

  Redistribution and use in source and binary forms, with or without
  modification, are permitted provided that the following conditions
  are met:

    - Redistributions of source code must retain the above copyright
       notice, this list of conditions and the following disclaimer. 

    - Neither the name of the KPD-Team, nor the names of its contributors
       may be used to endorse or promote products derived from this
       software without specific prior written permission. 

  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
  FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL
  THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT,
  INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
  (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
  SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
  HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
  STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
  ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED
  OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Specialized;
using System.Threading;
using Org.Mentalis.Proxy;
using Org.Mentalis.Network.ProxySocket;

namespace Org.Mentalis.Proxy.Http {

///<summary>Relays HTTP data between a remote host and a local client.</summary>
///<remarks>This class supports both HTTP and HTTPS.</remarks>
public class HttpClient : Client {
	///<summary>Initializes a new instance of the HttpClient class.</summary>
	///<param name="ClientSocket">The <see cref ="Socket">Socket</see> connection between this proxy server and the local client.</param>
	///<param name="Destroyer">The callback method to be called when this Client object disconnects from the local client and the remote server.</param>
	public HttpClient(Socket ClientSocket, DestroyDelegate Destroyer) : base(ClientSocket, Destroyer) {}
	///<summary>Gets or sets a StringDictionary that stores the header fields.</summary>
	///<value>A StringDictionary that stores the header fields.</value>
	protected StringDictionary HeaderFields {
		get {
			return m_HeaderFields;
		}
		set {
			m_HeaderFields = value;
		}
	}
	///<summary>Gets or sets the HTTP version the client uses.</summary>
	///<value>A string representing the requested HTTP version.</value>
    protected string HttpVersion
    {
		get {
			return m_HttpVersion;
		}
		set {
			m_HttpVersion = value;
		}
	}
	///<summary>Gets or sets the HTTP request type.</summary>
	///<remarks>
	///Usually, this string is set to one of the three following values:
	///<list type="bullet">
	///<item>GET</item>
	///<item>POST</item>
	///<item>CONNECT</item>
	///</list>
	///</remarks>
	///<value>A string representing the HTTP request type.</value>
    protected string HttpRequestType
    {
		get {
			return m_HttpRequestType;
		}
		set {
			m_HttpRequestType = value;
		}
	}
	///<summary>Gets or sets the requested path.</summary>
	///<value>A string representing the requested path.</value>
    protected string RequestedPath
    {
		get {
			return m_RequestedPath;
		}
		set {
			m_RequestedPath = value;
		}
	}
	///<summary>Gets or sets the query string, received from the client.</summary>
	///<value>A string representing the HTTP query string.</value>
    protected string HttpQuery
    {
		get {
			return m_HttpQuery;
		}
		set {
			if (value == null)
				throw new ArgumentNullException();
			m_HttpQuery = value;
		}
	}
	///<summary>Starts receiving data from the client connection.</summary>
    public override void StartHandshake()
    {
		try {
			ClientSocket.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, new AsyncCallback(this.OnReceiveQuery), ClientSocket);
		} catch {
			Dispose();
		}
	}
	///<summary>Checks whether a specified string is a valid HTTP query string.</summary>
	///<param name="Query">The query to check.</param>
	///<returns>True if the specified string is a valid HTTP query, false otherwise.</returns>
    protected bool IsValidQuery(string Query)
    {
		int index = Query.IndexOf("\r\n\r\n");
		if (index == -1)
			return false;
		HeaderFields = ParseQuery(Query);
		if (HttpRequestType.ToUpper().Equals("POST")) {
			try {
				int length = int.Parse((string)HeaderFields["Content-Length"]);
				return Query.Length >= index + 6 + length;
			} catch {
				SendBadRequest();
				return true;
			}
		} else {
			return true;
		}
	}
	///<summary>Processes a specified query and connects to the requested HTTP web server.</summary>
	///<param name="Query">A string containing the query to process.</param>
	///<remarks>If there's an error while processing the HTTP request or when connecting to the remote server, the Proxy sends a "400 - Bad Request" error to the client.</remarks>
	protected virtual void ProcessQuery(string Query) {
		HeaderFields = ParseQuery(Query);
		if (HeaderFields == null || !HeaderFields.ContainsKey("Host")) {
			SendBadRequest();
			return;
		}
		int Port;
		string Host;
		int Ret;
		if (HttpRequestType.ToUpper().Equals("CONNECT")) { //HTTPS
			Ret = RequestedPath.IndexOf(":");
			if (Ret >= 0) {
				Host = RequestedPath.Substring(0, Ret);
				if (RequestedPath.Length > Ret + 1)
					Port = int.Parse(RequestedPath.Substring(Ret + 1));
				else
					Port = 443;
			} else {
				Host = RequestedPath;
				Port = 443;
			}
		} else { //Normal HTTP
			Ret = ((string)HeaderFields["Host"]).IndexOf(":");
			if (Ret > 0) {
				Host = ((string)HeaderFields["Host"]).Substring(0, Ret);
				Port = int.Parse(((string)HeaderFields["Host"]).Substring(Ret + 1));
			} else {
				Host = (string)HeaderFields["Host"];
				Port = 80;
			}
			if (HttpRequestType.ToUpper().Equals("POST")) {
				int index = Query.IndexOf("\r\n\r\n");
                m_HttpPost = Query.Substring(index + 4);
			}
		}
		try {
			IPEndPoint DestinationEndPoint = new IPEndPoint(Dns.GetHostEntry(Host).AddressList[0], Port);
			DestinationSocket = new Socket(DestinationEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			if (HeaderFields.ContainsKey("Proxy-Connection") && HeaderFields["Proxy-Connection"].ToLower().Equals("keep-alive"))
				DestinationSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
			DestinationSocket.BeginConnect(DestinationEndPoint, new AsyncCallback(this.OnConnected), DestinationSocket);
		} catch {
			SendBadRequest();
			return;
		}
	}
	///<summary>Parses a specified HTTP query into its header fields.</summary>
	///<param name="Query">The HTTP query string to parse.</param>
	///<returns>A StringDictionary object containing all the header fields with their data.</returns>
	///<exception cref="ArgumentNullException">The specified query is null.</exception>
    protected StringDictionary ParseQuery(string Query)
    {
		StringDictionary retdict = new StringDictionary();
		string [] Lines = Query.Replace("\r\n", "\n").Split('\n');
		int Cnt, Ret;
		//Extract requested URL
		if (Lines.Length > 0) {
			//Parse the Http Request Type
			Ret = Lines[0].IndexOf(' ');
			if (Ret > 0) {
				HttpRequestType = Lines[0].Substring(0, Ret);
				Lines[0] = Lines[0].Substring(Ret).Trim();
			}
			//Parse the Http Version and the Requested Path
			Ret = Lines[0].LastIndexOf(' ');
			if (Ret > 0) {
				HttpVersion = Lines[0].Substring(Ret).Trim();
				RequestedPath = Lines[0].Substring(0, Ret);
			} else {
				RequestedPath = Lines[0];
			}
			//Remove http:// if present
			if (RequestedPath.Length >= 7 && RequestedPath.Substring(0, 7).ToLower().Equals("http://")) {
				Ret = RequestedPath.IndexOf('/', 7);
				if (Ret == -1)
					RequestedPath = "/";
				else
					RequestedPath = RequestedPath.Substring(Ret);
			}
		}
		for(Cnt = 1; Cnt < Lines.Length; Cnt++) {
			Ret = Lines[Cnt].IndexOf(":");
			if (Ret > 0 && Ret < Lines[Cnt].Length - 1) {
				try {
					retdict.Add(Lines[Cnt].Substring(0, Ret), Lines[Cnt].Substring(Ret + 1).Trim());
				} catch {}
			}
		}
		return retdict;
	}
	///<summary>Sends a "400 - Bad Request" error to the client.</summary>
    protected void SendBadRequest()
    {
		string brs = "HTTP/1.1 400 Bad Request\r\nConnection: close\r\nContent-Type: text/html\r\n\r\n<html><head><title>400 Bad Request</title></head><body><div align=\"center\"><table border=\"0\" cellspacing=\"3\" cellpadding=\"3\" bgcolor=\"#C0C0C0\"><tr><td><table border=\"0\" width=\"500\" cellspacing=\"3\" cellpadding=\"3\"><tr><td bgcolor=\"#B2B2B2\"><p align=\"center\"><strong><font size=\"2\" face=\"Verdana\">400 Bad Request</font></strong></p></td></tr><tr><td bgcolor=\"#D1D1D1\"><font size=\"2\" face=\"Verdana\"> The proxy server could not understand the HTTP request!<br><br> Please contact your network administrator about this problem.</font></td></tr></table></center></td></tr></table></div></body></html>";
		try {
			ClientSocket.BeginSend(Encoding.ASCII.GetBytes(brs), 0, brs.Length, SocketFlags.None, new AsyncCallback(this.OnErrorSent), ClientSocket);
		} catch {
			Dispose();
		}
	}
	///<summary>Rebuilds the HTTP query, starting from the HttpRequestType, RequestedPath, HttpVersion and HeaderFields properties.</summary>
	///<returns>A string representing the rebuilt HTTP query string.</returns>
    protected string RebuildQuery()
    {
		string ret = HttpRequestType + " " + RequestedPath + " " + HttpVersion + "\r\n";
		if (HeaderFields != null) {
			foreach (string sc in HeaderFields.Keys) {
				if (sc.Length < 6 || !sc.Substring(0, 6).Equals("proxy-"))
					ret += System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(sc) + ": " + (string)HeaderFields[sc] + "\r\n";
			}
			ret += "\r\n";
			if (m_HttpPost != null)
				ret += m_HttpPost;
		}
		return ret;
	}
	///<summary>Returns text information about this HttpClient object.</summary>
	///<returns>A string representing this HttpClient object.</returns>
	public override string ToString() {
		return ToString(false);
	}
	///<summary>Returns text information about this HttpClient object.</summary>
	///<returns>A string representing this HttpClient object.</returns>
	///<param name="WithUrl">Specifies whether or not to include information about the requested URL.</param>
	public string ToString(bool WithUrl) {
		string Ret;
		try {
			if (DestinationSocket == null || DestinationSocket.RemoteEndPoint == null)
				Ret = "Incoming HTTP connection from " + ((IPEndPoint)ClientSocket.RemoteEndPoint).Address.ToString();
			else
				Ret = "HTTP connection from " + ((IPEndPoint)ClientSocket.RemoteEndPoint).Address.ToString() + " to " + ((IPEndPoint)DestinationSocket.RemoteEndPoint).Address.ToString() + " on port " + ((IPEndPoint)DestinationSocket.RemoteEndPoint).Port.ToString();
			if (HeaderFields != null && HeaderFields.ContainsKey("Host") && RequestedPath != null)
				Ret += "\r\n" + " requested URL: http://" + HeaderFields["Host"] + RequestedPath;
		} catch {
			Ret = "HTTP Connection";
		}
		return Ret;
	}
	///<summary>Called when we received some data from the client connection.</summary>
	///<param name="ar">The result of the asynchronous operation.</param>
    protected void OnReceiveQuery(IAsyncResult ar)
    {
		int Ret;
		try {
			Ret = ClientSocket.EndReceive(ar);
		} catch {
			Ret = -1;
		}
		if (Ret <= 0) { //Connection is dead :(
			Dispose();
			return;
		}
		HttpQuery += Encoding.ASCII.GetString(Buffer, 0, Ret);
		//if received data is valid HTTP request...
		if (IsValidQuery(HttpQuery)) {
			ProcessQuery(HttpQuery);
		//else, keep listening
		} else {
			try {
				ClientSocket.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, new AsyncCallback(this.OnReceiveQuery), ClientSocket);
			} catch {
				Dispose();
			}
		}
	}
	///<summary>Called when the Bad Request error has been sent to the client.</summary>
	///<param name="ar">The result of the asynchronous operation.</param>
    protected void OnErrorSent(IAsyncResult ar)
    {
		try {
			ClientSocket.EndSend(ar);
		} catch {}
		Dispose();
	}
	///<summary>Called when we're connected to the requested remote host.</summary>
	///<param name="ar">The result of the asynchronous operation.</param>
    protected void OnConnected(IAsyncResult ar)
    {
		try {
			DestinationSocket.EndConnect(ar);
			string rq;
			if (HttpRequestType.ToUpper().Equals("CONNECT")) { //HTTPS
				rq = HttpVersion + " 200 Connection established\r\nProxy-Agent: Mentalis Proxy Server\r\n\r\n";
				ClientSocket.BeginSend(Encoding.ASCII.GetBytes(rq), 0, rq.Length, SocketFlags.None, new AsyncCallback(this.OnOkSent), ClientSocket);
			} else { //Normal HTTP
				rq = RebuildQuery();
				DestinationSocket.BeginSend(Encoding.ASCII.GetBytes(rq), 0, rq.Length, SocketFlags.None, new AsyncCallback(this.OnQuerySent), DestinationSocket);
			}
		} catch {
			Dispose();
		}
	}
	///<summary>Called when the HTTP query has been sent to the remote host.</summary>
	///<param name="ar">The result of the asynchronous operation.</param>
    protected void OnQuerySent(IAsyncResult ar)
    {
		try {
			if (DestinationSocket.EndSend(ar) == -1) {
				Dispose();
				return;
			}
			StartRelay();
		} catch {
			Dispose();
		}
	}
	///<summary>Called when an OK reply has been sent to the local client.</summary>
	///<param name="ar">The result of the asynchronous operation.</param>
    protected void OnOkSent(IAsyncResult ar)
    {
		try {
			if (ClientSocket.EndSend(ar) == -1) {
				Dispose();
				return;
			}
			StartRelay();
		} catch {
			Dispose();
		}
	}
	// private variables
	/// <summary>Holds the value of the HttpQuery property.</summary>
    protected string m_HttpQuery = "";
	/// <summary>Holds the value of the RequestedPath property.</summary>
    protected string m_RequestedPath = null;
	/// <summary>Holds the value of the HeaderFields property.</summary>
    protected StringDictionary m_HeaderFields = null;
	/// <summary>Holds the value of the HttpVersion property.</summary>
    protected string m_HttpVersion = "";
	/// <summary>Holds the value of the HttpRequestType property.</summary>
    protected string m_HttpRequestType = "";
	/// <summary>Holds the POST data</summary>
    protected string m_HttpPost = null;
}

}
