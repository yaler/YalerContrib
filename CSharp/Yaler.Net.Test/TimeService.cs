// Copyright (c) 2012, Yaler GmbH, Switzerland
// All rights reserved

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Yaler.Net.Sockets;

class Program {
	static void Main (string[] args) {
		YalerListener l = new YalerListener(args[0], int.Parse(args[1]), args[2]);
		if (args.Length > 3) {
			l.Proxy = new WebProxy(args[3], int.Parse(args[4]));
			if (args.Length > 5) {
				l.Proxy.Credentials = new NetworkCredential(args[5], args[6]);
			} else {
				l.Proxy.Credentials = CredentialCache.DefaultNetworkCredentials;
			}
		}
		while (true) {
			Socket s = l.AcceptSocket();
			bool found;
			SocketHelper.Find(s, "\r\n\r\n", out found);
			if (found) {
				s.Send(Encoding.ASCII.GetBytes(
					"HTTP/1.1 200 OK\r\n" +
					"Connection: close\r\n" +
					"Content-Length: 8\r\n\r\n" +
					DateTime.Now.ToString("HH:mm:ss")));
			}
			s.Shutdown(SocketShutdown.Both);
			s.Close();
		}
	}
}