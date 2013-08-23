// Copyright (c) 2013, Yaler GmbH, Switzerland
// All rights reserved

package org.yaler.proxy;

import java.net.Proxy;
import java.net.ProxySelector;
import java.net.URI;
import java.net.URISyntaxException;
import java.util.List;

public final class ProxyHelper {
	private ProxyHelper() {}

	public static final Proxy selectProxy(String hostname, int port) {
		URI uri;
		try {
			uri = new URI("http", null, hostname, port, null, null, null); //$NON-NLS-1$
			return selectProxy(uri);
		} catch (URISyntaxException e) {
			return Proxy.NO_PROXY;
		}
	}

	public static final Proxy selectProxy(URI uri) {
		String prevValue = System.getProperty("java.net.useSystemProxies", "false"); //$NON-NLS-1$ //$NON-NLS-2$
        System.setProperty("java.net.useSystemProxies", "true"); //$NON-NLS-1$ //$NON-NLS-2$
		List<Proxy> pl = ProxySelector.getDefault().select(uri);
		Proxy selectedProxy = null;
		if (pl == null || pl.size() < 1) {
			selectedProxy = Proxy.NO_PROXY;
		} else {
			selectedProxy = pl.get(0);
		}
        System.setProperty("java.net.useSystemProxies", prevValue); //$NON-NLS-1$
		return selectedProxy;
	}

}
