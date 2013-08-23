// Copyright (c) 2013, Yaler GmbH, Switzerland
// All rights reserved

package org.yaler.util.test;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.Socket;

import junit.framework.TestCase;

import org.yaler.util.KeepAliveSocket;

import android.util.Log;

class KeepAliveTestSocket extends Socket {
	private ByteArrayOutputStream _out = new ByteArrayOutputStream();
	private ByteArrayInputStream _in;

	@Override
	public InputStream getInputStream() throws IOException {
        if (this._in == null) {
            throw new IllegalArgumentException();
        }
        assert this._in != null;
        return this._in;
	}

	@Override
	public OutputStream getOutputStream() throws IOException {
		return this._out;
	}

	public void flip () {
        if (this._in != null) {
            throw new IllegalArgumentException();
        }
		assert this._in == null;
		this._in = new ByteArrayInputStream(this._out.toByteArray());
	}
}

public class KeepAliveTest extends TestCase {
	private static final String TAG = "KeepAliveTest"; //$NON-NLS-1$
	private KeepAliveSocket _socket;
	private InputStream _in;
	private OutputStream _out;
	private byte[] _buf;

	@Override
	protected void setUp () {
		Log.v(TAG, "setUp"); //$NON-NLS-1$
		KeepAliveTestSocket s0 = new KeepAliveTestSocket();
		this._buf = new byte[0x09C7];
		for (int i = 0; i < this._buf.length; i++) {
			this._buf[i] = (byte) i;
		}
		try {
			this._socket = new KeepAliveSocket(s0, 0, null); // gets input stream
			assertNotNull(this._socket);
		} catch (IOException e) {
			fail();
		}
        this._out = this._socket.getOutputStream();
		try {
			this._out.write(this._buf);
		} catch (IOException e) {
			fail();
		}
		s0.flip();
        this._in = this._socket.getInputStream();
	}

//	public final void testAvailable () {
//		Log.v(TAG, "testAvailable");
//		try {
//			//assertEquals(this.buf.length, this.in.available());
//			int totalAvailable = 0;
//			while (this.in.available() > 0) {
//				totalAvailable += this.in.available();
//				//assertTrue(this.buf.length > totalAvailable);
//				this.in.read();
//			}
//			//assertEquals(this.buf.length, totalAvailable);
//			assertTrue(this.buf.length >= totalAvailable);
//		} catch (IOException e) {
//			assertFalse(true);
//		}
//	}

//	public final void testRead () {
//		try {
//			int i = 0;
//			//while (this.in.available() > 0) {
//			while (i < _buf.length) {
//				int ch = _in.read();
//				assertTrue(ch != -1);
//				assertEquals(_buf[i], (byte)ch);
//				i++;
//			}
//		} catch (IOException e) {
//			assertFalse(true);
//		}
//	}

	@Override
	protected void tearDown () {
		Log.v(TAG, "tearDown"); //$NON-NLS-1$
		try {
			this._in.close();
			this._out.close();
		} catch (IOException e) {
			// skip
		}
	}
}
