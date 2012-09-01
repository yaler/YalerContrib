// Copyright (c) 2012, Yaler GmbH, Switzerland
// All rights reserved

namespace Yaler.Net.Sockets {

	using System;
	using System.Text;
	using System.Collections;
	using System.Net;
	using System.Net.Sockets;

	public sealed class ProxyClient {
		const int WSAEACCES = 10013;

		readonly IWebProxy proxy;
		volatile bool aborted;
		volatile Socket socket;

		public ProxyClient (IWebProxy proxy) {
			if (proxy == null) {
				throw new ArgumentNullException();
			}
			this.proxy = proxy;
		}

		public IWebProxy Proxy {
			get {
				return proxy;
			}
		}

		static bool IsSchemeChar (char c) {
			return (32 < c) && (c < 127) && (c != '(') && (c != ')') && (c != '<') && (c != '>') && (c != '@')
				&& (c != ',') && (c != ';') && (c != ':') && (c != '\\') && (c != '"') && (c != '/') && (c != '[')
				&& (c != ']') && (c != '?') && (c != '=') && (c != '{') && (c != '}');
		}

		static string Scheme (string challenge) {
			string result;
			int i, j = 0, n = challenge.Length;
			while ((j != n) && !IsSchemeChar(challenge[j])) {
				j++;
			}
			if (j != n) {
				i = j;
				do {
					j++;
				} while ((j != n) && IsSchemeChar(challenge[j]));
				result = challenge.Substring(i, j - i);
			} else {
				result = null;
			}
			return result;
		}

		static string NtlmToken (Uri uri, Uri proxyUri,
			NtlmContext c, string challenge)
		{
			byte[] token;
			if (challenge == null) {
				c.Authorize(null, out token);
			} else {
				challenge = challenge.Split(' ')[1];
				c.Authorize(Convert.FromBase64String(challenge), out token);
			}
			return Convert.ToBase64String(token);
		}

		static string DigestToken (Uri uri, Uri proxyUri, NetworkCredential nc,
			DigestContext c, string challenge)
		{
			string scheme;
			Hashtable parameters;
			c.TokenizeChallenge(challenge, out scheme, out parameters);
			return c.Response(
				"CONNECT", uri.Host + ":" + uri.Port, nc.UserName, nc.Password, parameters);
		}

		static string BasicToken (Uri uri, Uri proxyUri, NetworkCredential nc) {
			return Convert.ToBase64String(Encoding.ASCII.GetBytes(nc.UserName + ":" + nc.Password));
		}

		public Socket ConnectSocket (string host, int port) {
			if (aborted) {
				throw new InvalidOperationException();
			}
			try {
				Socket s = null;
				const string Basic = "Basic", Digest = "Digest", Ntlm = "NTLM";
				const int Authorizing = 0, Authorized = 1, Unauthorized = 2, Error = 3;
				socket = new Socket(
					AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				socket.ReceiveTimeout = 75000;
				if (!aborted) {
					s = socket;
					Uri uri = new Uri("http://" + host + ":" + port + "/");
					Uri proxyUri = proxy.GetProxy(uri);
					if (proxyUri.Equals(uri)) {
						s.Connect(uri.Host, uri.Port);
					} else {
						s.Connect(proxyUri.Host, proxyUri.Port);
						string authType = Ntlm;
						string challenge = null;
						NtlmContext ntlmContext = null;
						DigestContext digestContext = null;
						int state = Authorizing;
						do {
							string token = null;
							if ((proxy.Credentials == CredentialCache.DefaultNetworkCredentials)
								&& (authType == Ntlm))
							{
								if (ntlmContext == null) {
									ntlmContext = new NtlmContext(null, null, null);
								}
								token = NtlmToken(uri, proxyUri, ntlmContext, challenge);
							} else if (proxy.Credentials != null) {
								NetworkCredential c = proxy.Credentials.GetCredential(uri, authType);
								if (c != null) {
									if (authType == Ntlm) {
										if (ntlmContext == null) {
											ntlmContext = new NtlmContext(c.UserName, c.Domain, c.Password);
										}
										token = NtlmToken(uri, proxyUri, ntlmContext, challenge);
									} else if (authType == Digest) {
										if (digestContext == null) {
											digestContext = new DigestContext();
										}
										token = DigestToken(uri, proxyUri, c, digestContext, challenge);
									} else if (authType == Basic) {
										token = BasicToken(uri, proxyUri, c);
									}
								}
							}
							s.Send(Encoding.ASCII.GetBytes(
								"CONNECT " + uri.Host + ":" + uri.Port.ToString() + " HTTP/1.1\r\n" +
								"Host: " + proxyUri.Host + "\r\n" +
								(token != null ? "Proxy-Authorization: " + authType + " " + token + "\r\n" : "") +
								"\r\n"));
							int socketError, parserError, statusCode;
							string httpVersion, reasonPhrase, responseAuthType, responseAuthChallenge;
							HttpParser.ReceiveStatusLine(s, out httpVersion, out statusCode,
								out reasonPhrase, out socketError, out parserError);
							if ((socketError == 0) && (parserError == 0)) {
								Hashtable headers;
								HttpParser.ReceiveHeaders(s, out headers, out socketError, out parserError);
								if ((socketError == 0) && (parserError == 0)) {
									int contentLength;
									HttpParser.GetContentLength(headers, out contentLength, out parserError);
									if (parserError == 0) {
										HttpParser.SkipBody(s, contentLength, out socketError);
										if (socketError == 0) {
											if (statusCode == 200) {
												state = Authorized;
											} else if (statusCode == 407) {
												responseAuthChallenge = headers["Proxy-Authenticate"] as string;
												if (responseAuthChallenge != null) {
													responseAuthType = Scheme(responseAuthChallenge);
													if (responseAuthType == Ntlm) {
														if (challenge == null) {
															challenge = responseAuthChallenge;
														} else {
															state = Unauthorized;
														}
													} else if (responseAuthType == Digest) {
														if (authType != Digest) {
															authType = Digest;
															challenge = responseAuthChallenge;
														} else {
															state = Unauthorized;
														}
													} else if (responseAuthType == Basic) {
														if (authType != Basic) {
															authType = Basic;
															challenge = null;
														} else {
															state = Unauthorized;
														}
													} else { state = Error; }
												} else { state = Error; }
											} else { state = Error; }
										} else { state = Error; }
									} else { state = Error; }
								} else { state = Error; }
							} else { state = Error; }
						} while (state == Authorizing);
						if (ntlmContext != null) {
							ntlmContext.Dispose();
						}
						if (state != Authorized) {
							s.Close();
							throw new SocketException(WSAEACCES);
						}
					}
				}
				return s;
			} finally {
				socket = null;
			}
		}

		public void Abort () {
			aborted = true;
			try {
				socket.Close();
			} catch {}
		}

	}

}