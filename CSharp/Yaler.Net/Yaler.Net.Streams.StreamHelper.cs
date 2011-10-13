// Copyright (c) 2011, Yaler GmbH, Switzerland
// All rights reserved

namespace Yaler.Net.Streams {
	using System.IO;

	public sealed class StreamHelper {
		StreamHelper () {}

		public static void Find (Stream s, string pattern, out bool found) {
			int i = 0, j = 0, k = 0, p = 0, c = 0, x = 0;
			while ((k != pattern.Length) && (c != -1)) {
				if (i + k == j) {
					c = x = s.ReadByte();
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