// Copyright (c) 2011, Yaler GmbH, Switzerland
// All rights reserved

using System;
using System.Net.Sockets;
using System.Text;

class TimeService {
	static void Find (Socket s, string pattern, out bool found) {
		byte[] b = new byte[1];
		int i = 0, j = 0, k = 0, p = 0, c = 0, x = 0;
		while ((k != pattern.Length) && (c != -1)) {
			if (i + k == j) {
				int n = s.Receive(b);
				c = x = n != 0? b[0]: -1;
				p = i;
				j++;
			} else if (i + k == j - 1) {
				c = x;
			} else {
				c = pattern[i + k - p];
			}
			if (pattern[k] == c) {
				k++;
			} else {
				k = 0;
				i++;
			}
		}
		found = k == pattern.Length;
	}

	static void FindLocation (Socket s, out string host, out int port) {
		host = null;
		port = 80;
		bool found;
		Find(s, "\r\nLocation: http://", out found);
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
		Socket result;
		bool acceptable;
		int[] x = new int[3];
		byte[] b = new byte[1];
		do {
			result = new Socket(
				AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			result.Connect(host, port);
			result.NoDelay = true;
			do {
				result.Send(Encoding.ASCII.GetBytes(
					"POST /" + id + " HTTP/1.1\r\n" +
					"Upgrade: PTTH/1.0\r\n" +
					"Connection: Upgrade\r\n" +
					"Host: " + host + "\r\n\r\n"));
				for (int i = 0; i != 12; i++) {
					int n = result.Receive(b);
					x[i % 3] = n != 0? b[0]: -1;
				}
				if ((x[0] == '3') && (x[1] == '0') && (x[2] == '7')) {
					FindLocation(result, out host, out port);
				}
				Find(result, "\r\n\r\n", out acceptable);
			} while (acceptable && ((x[0] == '2') && (x[1] == '0') && (x[2] == '4')));
			if (!acceptable || (x[0] != '1') || (x[1] != '0') || (x[2] != '1')) {
				result.Close();
				result = null;
			}
		} while (acceptable && ((x[0] == '3') && (x[1] == '0') && (x[2] == '7')));
		return result;
	}

	static void Main (string[] args) {
		while (true) {
			Socket s = AcceptSocket(args[0], 80, args[1]);
			bool found;
			Find(s, "\r\n\r\n", out found);
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