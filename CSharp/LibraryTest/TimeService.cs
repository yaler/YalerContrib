using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Yaler.Net.Sockets;

class Program {
	static void Main (string[] args) {
		YalerListener l = new YalerListener(args[0], 80, args[1]);
		while (true) {
			Socket s = l.AcceptSocket();
			s.Send(Encoding.ASCII.GetBytes(
				"HTTP/1.1 200 OK\r\n" +
				"Connection: close\r\n" +
				"Content-Length: 8\r\n\r\n" +
				DateTime.Now.ToString("HH:mm:ss")));
			Thread.Sleep(1);
			s.Shutdown(SocketShutdown.Both);
			s.Close();
		}
	}
}