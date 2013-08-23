// Copyright (c) 2013, Yaler GmbH, Switzerland
// All rights reserved

package org.yaler.test;

import org.yaler.YalerSSLServerSocket;
import org.yaler.YalerSSLSocketFactory;

public class YalerSSLServerSocketTest extends YalerServerSocketTestBase {
	@Override
	protected void setUp () {
		super.setUp();
		this._relayProtocol = "https"; //$NON-NLS-1$
		this._server = new YalerSSLServerSocket(this._relayHost, 443, this._relayDomain);
		this._unreachableServer = new YalerSSLServerSocket(
			this._unreachableRelayHost, 443, this._relayDomain);
		this._socketFactory = YalerSSLSocketFactory.getInstance();
	}
}
