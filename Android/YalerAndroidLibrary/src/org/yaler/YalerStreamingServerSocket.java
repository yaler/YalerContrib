// Copyright (c) 2014, Yaler GmbH, Switzerland
// All rights reserved

package org.yaler;

import java.io.Closeable;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.Socket;
import java.net.SocketException;

import org.yaler.util.StreamHelper;

public final class YalerStreamingServerSocket implements Closeable {
	private final YalerServerSocket _baseListener;

	private volatile boolean _closed;
	private volatile Socket _listener;

	public YalerStreamingServerSocket(YalerServerSocket base) {
		this._baseListener = base;
	}

    public Socket accept() throws IOException {
        return accept(null);
    }

    @SuppressWarnings("resource")
    public Socket accept(AcceptCallback acceptCallback) throws IOException {
   	    if (this._closed) {
			throw new SocketException("YalerStreamingServerSocket is closed"); //$NON-NLS-1$
		}
		try {
			boolean acceptable = false;
			Socket result = null;
			this._listener = this._baseListener.accept(acceptCallback);
			if (!this._closed) {
				this._listener.setSoTimeout(75000);
				result = this._listener;
				InputStream i = result.getInputStream();
				OutputStream o = result.getOutputStream();
				acceptable = StreamHelper.find(i, "\r\n\r\n"); //$NON-NLS-1$
				if (acceptable) {
					o.write((
						"HTTP/1.1 101 Switching Protocols\r\n" + //$NON-NLS-1$
			    		"Upgrade: plainsocket\r\n" + //$NON-NLS-1$
			    		"Connection: Upgrade\r\n\r\n").getBytes()); //$NON-NLS-1$
					result.setSoTimeout(0);
				} else {
					result.close();
					result = null;
				}
			}
			return result;
		} finally {
			this._listener = null;
		}
	}

	@Override
	public void close() {
		this._closed = true;
		try {
			this._listener.close();
		} catch (Throwable t) {
			// ignore
		}
		try {
			this._baseListener.close();
		} catch (Throwable t) {
			// ignore
		}
	}
}
