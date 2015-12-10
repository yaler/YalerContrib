// Copyright (c) 2013, Yaler GmbH, Switzerland
// All rights reserved

package org.yaler.test;

import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.MalformedURLException;
import java.net.Socket;
import java.util.UUID;

import junit.framework.TestCase;

import org.yaler.YalerSSLServerSocket;
import org.yaler.YalerStreamingClient;
import org.yaler.YalerStreamingServerSocket;

import android.util.Log;

public class YalerStreamingTest extends TestCase {
	private static final String TAG = "YalerStreamingTest"; //$NON-NLS-1$

	private String _relayProtocol;
	private String _unreachableRelayHost;

	byte[] _testBuffer;
	String _relayHost;
	String _relayDomain;
	YalerStreamingServerSocket _server;
	YalerStreamingServerSocket _unreachableServer;

	@SuppressWarnings("resource")
	@Override
	protected void setUp () {
		this._testBuffer = new byte[1024];
		for (int i=0; i < this._testBuffer.length; i++) {
			this._testBuffer[i] = (byte) (Math.random() * 255);
		}
		this._relayHost = "try.yaler.io"; //$NON-NLS-1$
		this._unreachableRelayHost = "try.yaler.invalid"; // see http://tools.ietf.org/html/rfc2606 //$NON-NLS-1$
		this._relayDomain = String.format("difian-%s", UUID.randomUUID()); //$NON-NLS-1$
		Log.i(TAG, this._relayDomain);
		this._relayProtocol = "https"; //$NON-NLS-1$
		this._server = new YalerStreamingServerSocket(new YalerSSLServerSocket(
				this._relayHost, 443, this._relayDomain));
		this._unreachableServer = new YalerStreamingServerSocket(new YalerSSLServerSocket(
				this._unreachableRelayHost, 443, this._relayDomain));
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
					Socket socket = YalerStreamingTest.this._server.accept();
					OutputStream stream = socket.getOutputStream();
					stream.write(YalerStreamingTest.this._testBuffer);
					stream.close();
					socket.close();
				} catch (IOException e) {
					e.printStackTrace();
				}
			}
		});
		try {
			acceptThread.start();
			Thread.sleep(1500);
			Socket socket = YalerStreamingClient.connectSSLSocket(
				this._relayHost, 443, this._relayDomain, null);
			assertNotNull(socket);
			InputStream in = socket.getInputStream();
			int ch = in.read();
			int i = 0;
			while (ch != -1) {
				assertTrue((byte) ch == this._testBuffer[i]);
				ch = in.read();
				i++;
			}
	        in.close();
	        socket.close();
	        acceptThread.join();
		} catch (MalformedURLException e) {
			fail();
		} catch (IOException e) {
			fail();
		} catch (InterruptedException e) {
			fail();
		}
	}

	public final void testClose () {
		assertNotNull(this._relayProtocol);
		assertNotNull(this._server);
		Thread connectThread = new Thread(new Runnable() {
			@Override
			public void run() {
				try {
					Thread.sleep(1337);
					Socket socket = YalerStreamingClient.connectSSLSocket(
						YalerStreamingTest.this._relayHost, 443,
						YalerStreamingTest.this._relayDomain, null);
					InputStream in = socket.getInputStream();
					in.read(); // blocking
			        in.close();
			        socket.close();
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
