// Copyright (c) 2011, Yaler GmbH, Switzerland
// All rights reserved

using System;
using System.Net;
using System.Net.Security;
using System.Text;
using Yaler.Net.Security;
using Yaler.Net.Streams;

class Program {
	static void Main (string[] args) {
		YalerSslListener l = new YalerSslListener(args[0], int.Parse(args[1]), args[2]);
		if (args.Length > 3) {
			l.Proxy = new WebProxy(args[3], int.Parse(args[4]));
			if (args.Length > 5) {
				l.Proxy.Credentials = new NetworkCredential(args[5], args[6]);
			}
		}
		while (true) {
			SslStream s = l.AcceptSslStream();
			bool found;
			StreamHelper.Find(s, "\r\n\r\n", out found);
			if (found) {
				s.Write(Encoding.ASCII.GetBytes(
					"HTTP/1.1 200 OK\r\n" +
					"Connection: close\r\n" +
					"Content-Length: 8\r\n\r\n" +
					DateTime.Now.ToString("HH:mm:ss")));
			}
			s.Close();
		}
	}
}