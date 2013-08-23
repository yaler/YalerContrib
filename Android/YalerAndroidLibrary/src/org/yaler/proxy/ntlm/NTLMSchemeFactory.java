// Copyright (c) 2013, Yaler GmbH, Switzerland
// All rights reserved

package org.yaler.proxy.ntlm;

import org.apache.http.auth.AuthScheme;
import org.apache.http.auth.AuthSchemeFactory;
import org.apache.http.params.HttpParams;

public class NTLMSchemeFactory implements AuthSchemeFactory {

    @Override
	public AuthScheme newInstance(final HttpParams params) {
    	throw new UnsupportedOperationException(); // Not yet implemented
    }

}
