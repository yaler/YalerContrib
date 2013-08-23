// Copyright (c) 2013, Yaler GmbH, Switzerland
// All rights reserved

package org.yaler.test;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.MalformedURLException;
import java.net.Socket;
import java.net.URL;
import java.net.URLConnection;
import java.util.UUID;

import junit.framework.TestCase;

import org.yaler.YalerServerSocket;
import org.yaler.YalerSocketFactory;

import android.os.SystemClock;
import android.util.Log;

public abstract class YalerServerSocketTestBase extends TestCase {
	private static final String TAG = "YalerServerSocketTestBase"; //$NON-NLS-1$

	protected String _relayProtocol;
	protected String _relayHost;
	protected String _unreachableRelayHost;
	protected String _relayDomain;
	protected YalerServerSocket _server;
	protected YalerServerSocket _unreachableServer;
	protected YalerSocketFactory _socketFactory;

	protected YalerServerSocketTestBase () {
		super();
	}

	@Override
	protected void setUp () {
		this._relayHost = "try.yaler.net"; //$NON-NLS-1$
		this._unreachableRelayHost = "try.yaler.invalid";// see http://tools.ietf.org/html/rfc2606 //$NON-NLS-1$
		this._relayDomain = String.format("difian-%s", UUID.randomUUID()); //$NON-NLS-1$
		Log.i(TAG, this._relayDomain);
	}

	public final void testAcceptFail () {
		try {
			this._unreachableServer.accept();
			fail();
		} catch (IOException e) {
			// expected
		}
	}

	public final void testConnection () {
		assertNotNull(this._relayProtocol);
		assertNotNull(this._server);
		Thread acceptThread = new Thread(new Runnable() {
			@Override
			public void run() {
				try {
					Socket socket = YalerServerSocketTestBase.this._server.accept();
					OutputStream stream = socket.getOutputStream();
					stream.write((
							"HTTP/1.1 200 OK\r\n" + //$NON-NLS-1$
							"Connection: close\r\n" + //$NON-NLS-1$
							"Content-Length: 5\r\n\r\n" + //$NON-NLS-1$
							"Hello" //$NON-NLS-1$
						).getBytes());
					stream.flush();
					Thread.sleep(100); // TODO
					stream.close();
					socket.close();
				} catch (IOException e) {
					e.printStackTrace();
				} catch (InterruptedException e) {
					e.printStackTrace();
				}
			}
		});
		try {
			acceptThread.start();
			URLConnection conn;
			URL url = new URL(
				String.format("%s://%s/%s", //$NON-NLS-1$
					this._relayProtocol,
					this._relayHost,
					this._relayDomain));
			int responseCode;
			do {
				conn = url.openConnection();
				assertTrue(conn instanceof HttpURLConnection);
				responseCode = ((HttpURLConnection) conn).getResponseCode();
				if (responseCode == 307) {
					url = new URL(conn.getHeaderField("Location")); //$NON-NLS-1$
				}
			} while ((responseCode == 504) || (responseCode == 307));
			assertEquals(HttpURLConnection.HTTP_OK, responseCode);
			InputStream in = conn.getInputStream();
			byte[] response = "Hello".getBytes(); //$NON-NLS-1$
			for (byte b : response) {
				assertTrue(b == in.read());
			}
			assertTrue(-1 == in.read());
	        in.close();
	        acceptThread.join();
		} catch (MalformedURLException e) {
			fail();
		} catch (IOException e) {
			e.printStackTrace();
			fail();
		} catch (InterruptedException e) {
			fail();
		}
	}

	public final void testLatency () {
		int n = 10;
		long[] dt = new long[n];
		for (int i = 0; i < n; i++) {
			long t0 = SystemClock.elapsedRealtime();
			testConnection(); // TODO
			dt[i] = SystemClock.elapsedRealtime() - t0;
		}
		long avg = 0;
		long min = Long.MAX_VALUE;
		long max = 0;
		for (long t : dt) {
			avg += t;
			min = Math.min(min, t);
			max = Math.max(max, t);
		}
		avg = avg / n;
		Log.v(TAG,
			"n=" + n + //$NON-NLS-1$
			", avg=" + avg + //$NON-NLS-1$
			", min=" + min + //$NON-NLS-1$
			", max=" + max + //$NON-NLS-1$
			", " + this._relayProtocol + //$NON-NLS-1$
			"://" + this._relayHost + //$NON-NLS-1$
			"/"); //$NON-NLS-1$
	}

	public final void testClose () {
		assertNotNull(this._relayProtocol);
		assertNotNull(this._server);
		Thread connectThread = new Thread(new Runnable() {
			@Override
			public void run() {
				try {
					Thread.sleep(1337);
					Socket socket = YalerServerSocketTestBase.this._socketFactory.createSocket(
						null, YalerServerSocketTestBase.this._relayHost, 
						YalerServerSocketTestBase.this._relayProtocol.equals("http") ? 80 : 443, 5000); //$NON-NLS-1$
					OutputStream out = socket.getOutputStream();
					out.write((
						"GET /" + YalerServerSocketTestBase.this._relayDomain + " HTTP/1.1\r\n" + //$NON-NLS-1$ //$NON-NLS-2$
						"Host: " + YalerServerSocketTestBase.this._relayHost + "\r\n\r\n").getBytes()); //$NON-NLS-1$ //$NON-NLS-2$
					out.flush();
					InputStream in = socket.getInputStream();
					in.read(); // blocking
					in.close();
					out.close();
				} catch (IOException e) {
					e.printStackTrace();
				} catch (InterruptedException e) {
					e.printStackTrace();
				}
			}
		});
		connectThread.start();
		try {
			Socket socket = this._server.accept();
			assertNotNull(socket);
			assertTrue(!socket.isClosed());
			this._server.close();
			assertTrue(!socket.isClosed());
			socket.close();
			assertTrue(socket.isClosed());
			connectThread.join();
		} catch (IOException e) {
			fail();
		} catch (InterruptedException e) {
			fail();
		}
	}

	@Override
	protected void tearDown() throws Exception {
		this._server.close();
		this._unreachableServer.close();
	}
}
