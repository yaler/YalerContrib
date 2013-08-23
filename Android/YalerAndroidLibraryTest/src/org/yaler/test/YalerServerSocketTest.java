// Copyright (c) 2013, Yaler GmbH, Switzerland
// All rights reserved

package org.yaler.test;

import org.yaler.YalerServerSocket;
import org.yaler.YalerSocketFactory;

public class YalerServerSocketTest extends YalerServerSocketTestBase {
	@Override
	protected void setUp () {
		super.setUp();
		this._relayProtocol = "http"; //$NON-NLS-1$
		this._server = new YalerServerSocket(this._relayHost, 80, this._relayDomain);
		this._unreachableServer = new YalerServerSocket(
				this._unreachableRelayHost, 80, this._relayDomain);
		this._socketFactory = YalerSocketFactory.getInstance();
	}
}
