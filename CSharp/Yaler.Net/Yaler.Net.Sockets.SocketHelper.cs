// Copyright (c) 2012, Yaler GmbH, Switzerland
// All rights reserved

namespace Yaler.Net.Sockets {
	using System.Net.Sockets;

	public sealed class SocketHelper {
		SocketHelper () {}

		public static void Find (Socket s, string pattern, out bool found) {
			byte[] b = new byte[1];
			int i = 0, j = 0, k = 0, p = 0, c = 0, x = 0;
			while ((k != pattern.Length) && (c != -1)) {
				if (i + k == j) {
					int n = s.Receive(b);
					c = x = n != 0? b[0]: -1;
					p = i;
					j++;
				} else if (i + k == j - 1) {
					c = x;
				} else {
					c = pattern[i + k - p];
				}
				if (pattern[k] == c) {
					k++;
				} else {
					k = 0;
					i++;
				}
			}
			found = k == pattern.Length;
		}
	}
}