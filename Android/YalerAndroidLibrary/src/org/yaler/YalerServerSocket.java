// Copyright (c) 2014, Yaler GmbH, Switzerland
// All rights reserved

package org.yaler;

import java.io.Closeable;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.InetSocketAddress;
import java.net.Proxy;
import java.net.Socket;
import java.net.SocketException;

import org.yaler.proxy.ProxyHelper;
import org.yaler.util.StreamHelper;

public class YalerServerSocket implements Closeable {
	private final String _host;
	private final int _port;
	private final String _id;
	private final Proxy _proxy;

	private volatile boolean _closed;
	private volatile Socket _listener;

	public YalerServerSocket(String host, int port, String id) {
		this(host, port, id, ProxyHelper.selectProxy(host, port));
	}

	public YalerServerSocket(String host, int port, String id, Proxy proxy) {
		this._host = host;
		this._port = port;
		this._id = id;
		this._proxy = proxy;
	}

	@SuppressWarnings("static-method")
	public YalerSocketFactory getYalerSocketFactory() {
		return YalerSocketFactory.getInstance();
	}

	@SuppressWarnings("static-method")
	public InetSocketAddress findLocation(InputStream stream) throws IOException {
		return StreamHelper.findLocation(stream, "http", 80); //$NON-NLS-1$
	}

    public Socket accept() throws IOException {
        return accept(null);
    }

	@SuppressWarnings("resource")
	public Socket accept(AcceptCallback acceptCallback) throws IOException {
		if (this._closed) {
			throw new SocketException("YalerServerSocket is closed"); //$NON-NLS-1$
		}
		if (acceptCallback != null) {
            acceptCallback.statusChanged(AcceptCallbackState.Undefined);
        }
		try {
		    String host = this._host;
	        int port = this._port;
	        Socket result = null;
	        boolean acceptable = false;
	        int[] x = new int[3];
			do {
				this._listener = getYalerSocketFactory().createSocket(this._proxy, host, port, 5000);
				if (!this._closed) {
					this._listener.setSoTimeout(75000);
					this._listener.setTcpNoDelay(true);
					result = this._listener;
					InputStream i = result.getInputStream();
					OutputStream o = result.getOutputStream();
		            do {
		            	o.write((
		            		"POST /" + this._id + " HTTP/1.1\r\n" + //$NON-NLS-1$ //$NON-NLS-2$
		            		"Upgrade: PTTH/1.0\r\n" + //$NON-NLS-1$
                            "Connection: Upgrade\r\n" + //$NON-NLS-1$
                            "Host: " + host + "\r\n\r\n").getBytes()); //$NON-NLS-1$ //$NON-NLS-2$
		            	for (int j = 0; j != 12; j++) {
							x[j % 3] = i.read();
						}
		            	if ((x[0] == '3') && (x[1] == '0') && (x[2] == '7')) {
		            		InetSocketAddress address = findLocation(i);
		            		host = address.getHostName();
		            		port = address.getPort();
		            	}
		            	acceptable = StreamHelper.find(i, "\r\n\r\n"); //$NON-NLS-1$
		            	if ((acceptCallback != null) &&
                            acceptable && ((x[0] == '2') && (x[1] == '0') && (x[2] == '4')))
                        {
                            acceptCallback.statusChanged(AcceptCallbackState.Accessible);
                        }
		            } while (acceptable && ((x[0] == '2') && (x[1] == '0') && (x[2] == '4')));
		            if (acceptable && (x[0] == '1') && (x[1] == '0') &&(x[2] == '1')) {
                        result.setSoTimeout(0);
                        if (acceptCallback != null) {
                            acceptCallback.statusChanged(AcceptCallbackState.Connected);
                        }
		            } else {
		            	result.close();
		            	result = null;
		            }
	            }
			} while (acceptable && ((x[0] == '3') && (x[1] == '0') && (x[2] == '7')));
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
	}
}
