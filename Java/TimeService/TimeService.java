import java.io.*;
import java.net.*;
import java.text.*;
import java.util.*;

class TimeService {
	static boolean find (String pattern, InputStream s) throws IOException {
		int[] x = new int[pattern.length()];
		int i = 0, j = 0, t = 0;
		boolean match;
		do {
			match = true;
			for (int k = 0; (k != pattern.length()) && match; k++) {
				if (i + k == j) {
					x[j % x.length] = s.read();
					j++;
				}
				t = x[(i + k) % x.length];
				match = pattern.charAt(k) == t;
			}
			i++;
		} while (!match && (t != -1));
		return match;
	}

	static InetSocketAddress location (InputStream s) throws IOException {
		InetSocketAddress location = null;
		if (find("\r\nLocation: http://", s)) {
			StringBuilder host = new StringBuilder();
			int port = 80;
			int x = s.read();
			while ((x != -1) && (x != ':') && (x != '/')) {
				host.append((char) x);
				x = s.read();
			}
			if (x == ':') {
				port = 0;
				x = s.read();
				while ((x != -1) && (x != '/')) {
					port = 10 * port + x - '0';
					x = s.read();
				}
			}
			location = InetSocketAddress.createUnresolved(host.toString(), port);
		}
		return location;
	}

	static Socket accept (String host, int port, String id) throws IOException {
		Socket s;
		boolean acceptable;
		int[] x = new int[3];
		do {
			s = new Socket(host, port);
			InputStream i = s.getInputStream();
			PrintStream o = new PrintStream(s.getOutputStream());
			do {
				o.print(
					"POST /" + id + " HTTP/1.1\r\n" +
					"Upgrade: PTTH/1.0\r\n" +
					"Connection: Upgrade\r\n" +
					"Host: " + host + "\r\n\r\n");
				for (int j = 0; j != 12; j++) {
					x[j % 3] = i.read();
				}
				if ((x[0] == '3') && (x[1] == '0') && (x[2] == '7')) {
					InetSocketAddress location = location(i);
					host = location != null? location.getHostName(): null;
					port = location != null? location.getPort(): 0;
				}
				acceptable = find("\r\n\r\n", i);
			} while (acceptable && ((x[0] == '2') && (x[1] == '0') && (x[2] == '4')));
			if (!acceptable || (x[0] != '1') || (x[1] != '0') || (x[2] != '1')) {
				s.close();
				s = null;
			}
		} while (acceptable && ((x[0] == '3') && (x[1] == '0') && (x[2] == '7')));
		return s;
	}

	public static void main (String[] args) throws IOException {
		while (true) {
			Socket s = accept(args[0], 80, args[1]);
			PrintStream o = new PrintStream(s.getOutputStream());
			o.print(
				"HTTP/1.1 200 OK\r\n" +
				"Connection: close\r\n" +
				"Content-Length: 8\r\n\r\n" +
				new SimpleDateFormat("HH:mm:ss").format(new Date()));
			o.close();
			s.close();
		}
	}
}
