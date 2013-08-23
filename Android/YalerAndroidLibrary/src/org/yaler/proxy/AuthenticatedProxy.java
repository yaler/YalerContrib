// Copyright (c) 2013, Yaler GmbH, Switzerland
// All rights reserved

package org.yaler.proxy;

import java.net.Proxy;
import java.net.InetSocketAddress;

import org.apache.http.auth.Credentials;

public class AuthenticatedProxy extends Proxy {

	public AuthenticatedProxy(Type type, InetSocketAddress sa, Credentials auth) {
		super(type, sa);
		this._address = sa;
		this._authentication = auth;
	}

	private final Credentials _authentication;
	private final InetSocketAddress _address;

	public InetSocketAddress getInetSocketAddress() {
		return this._address;
	}

	public Credentials getCredentials() {
		return this._authentication;
	}

}
