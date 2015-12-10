// Copyright (c) 2011, Yaler GmbH, Switzerland
// All rights reserved

package org.yaler.example;

import android.app.Service;
import android.content.Intent;
import android.os.IBinder;

import java.io.*;
import java.net.*;
import java.text.*;
import java.util.*;

public class TimeService extends Service {

	static boolean find (InputStream s, String pattern) throws IOException {
		int i = 0, j = 0, k = 0, p = 0, c = 0, x = 0;
		while ((k != pattern.length()) && (c != -1)) {
			if (i + k == j) {
				c = x = s.read();
				p = i;
				j++;
			} else if (i + k == j - 1) {
				c = x;
			} else {
				c = pattern.charAt(i + k - p);
			}
			if (pattern.charAt(k) == c) {
				k++;
			} else {
				k = 0;
				i++;
			}
		}
		return k == pattern.length();
	}

	static InetSocketAddress location (InputStream s) throws IOException {
		InetSocketAddress location = null;
		if (find(s, "\r\nLocation: http://")) {
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
				acceptable = find(i, "\r\n\r\n");
			} while (acceptable && ((x[0] == '2') && (x[1] == '0') && (x[2] == '4')));
			if (!acceptable || (x[0] != '1') || (x[1] != '0') || (x[2] != '1')) {
				s.close();
				s = null;
			}
		} while (acceptable && ((x[0] == '3') && (x[1] == '0') && (x[2] == '7')));
		return s;
	}

	@Override
	public IBinder onBind(Intent intent) {
		return null; // not used
	}

	@Override
	public int onStartCommand(Intent i, int flags, int startId) {
		final String YALER_HOST = "try.yaler.io";
		final String RELAY_DOMAIN = "RELAY_DOMAIN";
		new Thread(
			new Runnable(){
				public void run(){
					try {
						while (true) {
							Socket s = accept(YALER_HOST, 8081, RELAY_DOMAIN);
							PrintStream o = new PrintStream(s.getOutputStream());
							o.print(
								"HTTP/1.1 200 OK\r\n" +
								"Connection: close\r\n" +
								"Content-Length: 8\r\n\r\n" +
								new SimpleDateFormat("HH:mm:ss").format(new Date()));
							o.close();
							s.close();
						}
					} catch (Exception e) {
						e.printStackTrace();
					}
				}
			}
		).start();
		return START_STICKY;
	}

}