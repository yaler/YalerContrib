using System;
using System.Net.Security;
using System.Text;
using System.Threading;
using Yaler.Net.Security;

class Program {
	static void Main (string[] args) {
		YalerSslListener l = new YalerSslListener(args[0], 443, args[1]);
		while (true) {
			SslStream s = l.AcceptSslStream();
			s.Write(Encoding.ASCII.GetBytes(
				"HTTP/1.1 200 OK\r\n" +
				"Connection: close\r\n" +
				"Content-Length: 8\r\n\r\n" +
				DateTime.Now.ToString("HH:mm:ss")));
			Thread.Sleep(1);
			s.Close();
		}
	}
}