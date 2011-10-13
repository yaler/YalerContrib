// Copyright (c) 2011, Yaler GmbH, Switzerland
// All rights reserved

namespace Yaler.Net.Sockets {

	using System;
	using System.Collections;
	using System.Security.Cryptography;
	using System.Text;

	sealed class DigestContext {
		readonly RNGCryptoServiceProvider nonceGenerator = new RNGCryptoServiceProvider();
		readonly MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();

		bool IsTokenChar (char c) {
			return (32 < c) && (c < 127) && (c != '(') && (c != ')') && (c != '<') && (c != '>') && (c != '@')
				&& (c != ',') && (c != ';') && (c != ':') && (c != '\\') && (c != '"') && (c != '/') && (c != '[')
				&& (c != ']') && (c != '?') && (c != '=') && (c != '{') && (c != '}');
		}

		void TokenizeQop (string qop, out ArrayList options) {
			options = new ArrayList();
			int i, j = 0, n = qop.Length;
			do {
				while ((j != n) && !IsTokenChar(qop[j])) {
					j++;
				}
				if (j != n) {
					i = j;
					do {
						j++;
					} while ((j != n) && IsTokenChar(qop[j]));
					options.Add(qop.Substring(i, j - i));
				}
			} while (j != n);
		}

		string Nonce () {
			byte[] nonce = new byte[24];
			nonceGenerator.GetBytes(nonce);
			return Convert.ToBase64String(nonce);
		}

		string Hash (string s) {
			byte[] hash = md5.ComputeHash(Encoding.ASCII.GetBytes(s));
			StringBuilder sb = new StringBuilder(hash.Length * 2);
			foreach (byte h in hash) {
				sb.AppendFormat("{0:x2}", h);
			}
			return sb.ToString();
		}

		internal string Response (
			string method, string uri, string username, string password, Hashtable parameters)
		{
			string
				realm = parameters["realm"] as string,
				nonce = parameters["nonce"] as string,
				opaque = parameters["opaque"] as string,
				algorithm = parameters["algorithm"] as string,
				qop = parameters["qop"] as string,
				cnonce = Nonce(),
				nc = "00000001";
			if (qop != null) {
				ArrayList options;
				TokenizeQop(qop, out options);
				qop =
					options.Contains("auth")?
						"auth":
					options.Contains("auth-int")?
						"auth-int":
					null;
			}
			string a1 =
				(algorithm == "MD5") || (algorithm == null)?
					username + ":" + realm + ":" + password:
				algorithm == "MD5-sess"?
					Hash(username + ":" + realm + ":" + password) + ":" + nonce + ":" + cnonce:
				"";
			string a2 =
				(qop == "auth") || (qop == null)?
					method + ":" + uri:
				qop == "auth-int"?
					method + ":" + uri + ":" + Hash(""):
				"";
			string response =
				(qop == "auth") || (qop == "auth-int")?
					Hash(Hash(a1) + ":" + nonce + ":" + nc + ":" + cnonce + ":" + qop + ":" + Hash(a2)):
				qop == null?
					Hash(Hash(a1) + ":" + nonce + ":" + Hash(a2)):
				"";
			string digest =
				"username=\"" + username + "\"" +
				",realm=\"" + realm + "\"" +
				",nonce=\"" + nonce + "\"" +
				",uri=\"" + uri + "\"" +
				",response=\"" + response + "\"";
			if (algorithm != null) {
				digest += ",algorithm=" + algorithm;
			}
			if (opaque != null) {
				digest += ",opaque=\"" + opaque + "\"";
			}
			if (qop != null) {
				digest += ",qop=" + qop + ",cnonce=\"" + cnonce + "\"" + ",nc=" + nc;
			}
			return digest;
		}

		internal void TokenizeChallenge (string challenge,
			out string scheme, out Hashtable parameters)
		{
			scheme = null;
			parameters = new Hashtable();
			int i, j = 0, n = challenge.Length;
			while ((j != n) && !IsTokenChar(challenge[j])) {
				j++;
			}
			if (j != n) {
				i = j;
				do {
					j++;
				} while ((j != n) && IsTokenChar(challenge[j]));
				scheme = challenge.Substring(i, j - i);
				while (j != n) {
					do {
						j++;
					} while ((j != n) && !IsTokenChar(challenge[j]));
					if (j != n) {
						i = j;
						do {
							j++;
						} while ((j != n) && IsTokenChar(challenge[j]));
						string name = challenge.Substring(i, j - i);
						while ((j != n) && (challenge[j] != '"') && !IsTokenChar(challenge[j])) {
							j++;
						}
						if (j != n) {
							if (challenge[j] == '"') {
								i = j + 1;
								do {
									j++;
									if ((j != n) && (challenge[j] == '\\')) {
										j++;
										if (j != n) {
											j++;
										}
									}
								} while ((j != n) && (challenge[j] != '"'));
							} else {
								i = j;
								do {
									j++;
								} while ((j != n) && IsTokenChar(challenge[j]));
							}
							parameters[name] = challenge.Substring(i, j - i);
						}
					}
				}
			}
		}
	}

}