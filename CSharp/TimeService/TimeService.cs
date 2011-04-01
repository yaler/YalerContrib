using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

class TimeService {
	static void Find (string pattern, Socket s, out bool found) {
		int[] x = new int[pattern.Length];
		byte[] b = new byte[1];
		int i = 0, j = 0, t = 0;
		do {
			found = true;
			for (int k = 0; (k != pattern.Length) && found; k++) {
				if (i + k == j) {
					int n = s.Receive(b);
					x[j % x.Length] = n != 0? b[0]: -1;
					j++;
				}
				t = x[(i + k) % x.Length];
				found = pattern[k] == t;
			}
			i++;
		} while (!found && (t != -1));
	}

	static void FindLocation (Socket s, out string host, out int port) {
		host = null;
		port = 80;
		bool found;
		Find("\r\nLocation: http://", s, out found);
		if (found) {
			StringBuilder h = new StringBuilder();
			byte[] x = new byte[1];
			int n = s.Receive(x);
			while ((n != 0) && (x[0] != ':') && (x[0] != '/')) {
				h.Append((char) x[0]);
				n = s.Receive(x);
			}
			if (x[0] == ':') {
				port = 0;
				n = s.Receive(x);
				while ((n != 0) && (x[0] != '/')) {
					port = 10 * port + x[0] - '0';
					n = s.Receive(x);
				}
			}
			host = h.ToString();
		}
	}

	static Socket AcceptSocket (string host, int port, string id) {
		Socket s;
		bool acceptable;
		int[] x = new int[3];
		byte[] b = new byte[1];
		do {
			s = new Socket(
				AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			s.NoDelay = true;
			s.Connect(host, port);
			do {
				s.Send(Encoding.ASCII.GetBytes(
					"POST /" + id + " HTTP/1.1\r\n" +
					"Upgrade: PTTH/1.0\r\n" +
					"Connection: Upgrade\r\n" +
					"Host: " + host + "\r\n\r\n"));
				for (int j = 0; j != 12; j++) {
					int n = s.Receive(b);
					x[j % 3] = n != 0? b[0]: -1;
				}
				if ((x[0] == '3') && (x[1] == '0') && (x[2] == '7')) {
					FindLocation(s, out host, out port);
				}
				Find("\r\n\r\n", s, out acceptable);
			} while (acceptable && ((x[0] == '2') && (x[1] == '0') && (x[2] == '4')));
			if (!acceptable || (x[0] != '1') || (x[1] != '0') || (x[2] != '1')) {
				s.Close();
				s = null;
			}
		} while (acceptable && ((x[0] == '3') && (x[1] == '0') && (x[2] == '7')));
		return s;
	}

	static void Main (string[] args) {
		while (true) {
			Socket s = AcceptSocket(args[0], 80, args[1]);
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