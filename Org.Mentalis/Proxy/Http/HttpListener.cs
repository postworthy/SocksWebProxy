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
using System.Net.Sockets;
using Org.Mentalis.Proxy;

namespace Org.Mentalis.Proxy.Http {

///<summary>Listens on a specific port on the proxy server and forwards all incoming HTTP traffic to the appropriate server.</summary>
public class HttpListener : Listener {
	///<summary>Initializes a new instance of the HttpListener class.</summary>
	///<param name="Port">The port to listen on.</param>
	///<remarks>The HttpListener will start listening on all installed network cards.</remarks>
	public HttpListener(int Port) : this(IPAddress.Any, Port) {}
	///<summary>Initializes a new instance of the HttpListener class.</summary>
	///<param name="Port">The port to listen on.</param>
	///<param name="Address">The address to listen on. You can specify IPAddress.Any to listen on all installed network cards.</param>
	///<remarks>For the security of your server, try to avoid to listen on every network card (IPAddress.Any). Listening on a local IP address is usually sufficient and much more secure.</remarks>
	public HttpListener(IPAddress Address, int Port) : base(Port, Address) {}
	///<summary>Called when there's an incoming client connection waiting to be accepted.</summary>
	///<param name="ar">The result of the asynchronous operation.</param>
	public override void OnAccept(IAsyncResult ar) {
		try {
			Socket NewSocket = ListenSocket.EndAccept(ar);
			if (NewSocket != null) {
				HttpClient NewClient = new HttpClient(NewSocket, new DestroyDelegate(this.RemoveClient));
				AddClient(NewClient);
				NewClient.StartHandshake();
			}
		} catch {}
		try {
			//Restart Listening
			ListenSocket.BeginAccept(new AsyncCallback(this.OnAccept), ListenSocket);
		} catch {
			Dispose();
		}
	}
	///<summary>Returns a string representation of this object.</summary>
	///<returns>A string with information about this object.</returns>
	public override string ToString() {
		return "HTTP service on " + Address.ToString() + ":" + Port.ToString();
	}
	///<summary>Returns a string that holds all the construction information for this object.</summary>
	///<value>A string that holds all the construction information for this object.</value>
	public override string ConstructString {
		get {
			return "host:" + Address.ToString() + ";int:" + Port.ToString();
		}
	}
}

}
