// Copyright (c) 2011, Yaler GmbH, Switzerland
// All rights reserved

namespace Yaler.Net.Security {
	using System;
	using System.IO;
	using System.Net;
	using System.Net.Security;
	using System.Net.Sockets;
	using System.Security.Cryptography.X509Certificates;
	using System.Text;
	using System.Threading;
	using Yaler.Net.Sockets;
	using Yaler.Net.Streams;

	sealed class AsyncResult: IAsyncResult {
		readonly object state;
		readonly AsyncCallback callback;
		readonly ManualResetEvent waitHandle;
		volatile bool isCompleted;
		Exception exception;
		SslStream result;

		AsyncResult (AsyncCallback callback, object state) {
			this.state = state;
			this.callback = callback;
			waitHandle = new ManualResetEvent(false);
		}

		bool IAsyncResult.IsCompleted {
			get {
				return isCompleted;
			}
		}

		bool IAsyncResult.CompletedSynchronously {
			get {
				return false;
			}
		}

		WaitHandle IAsyncResult.AsyncWaitHandle {
			get {
				return waitHandle;
			}
		}

		object IAsyncResult.AsyncState {
			get {
				return state;
			}
		}

		void DoAcceptSslStream (object listener) {
			try {
				result = (listener as YalerSslListener).AcceptSslStream();
			} catch (Exception e) {
				exception = e;
			} finally {
				isCompleted = true;
				waitHandle.Set();
				if (callback != null) {
					callback(this);
				}
			}
		}

		internal SslStream End () {
			if (!isCompleted) {
				waitHandle.WaitOne();
			}
			try {
				waitHandle.Close();
			} catch {}
			if (exception == null) {
				return result;
			} else {
				throw exception;
			}
		}

		internal static AsyncResult New (YalerSslListener listener, AsyncCallback callback, object state) {
			AsyncResult result = new AsyncResult(callback, state);
			ThreadPool.QueueUserWorkItem(new WaitCallback(result.DoAcceptSslStream), listener);
			return result;
		}
	}

	public sealed class YalerSslListener {
		readonly string host, id;
		readonly int port;
		volatile bool aborted;
		volatile Socket listener;
		volatile ProxyClient proxyClient;

		public YalerSslListener (string host, int port, string id) {
			this.host = host;
			this.port = port;
			this.id = id;
		}

		static void FindLocation (Stream s, out string host, out int port) {
			host = null;
			port = 443;
			bool found;
			StreamHelper.Find(s, "\r\nLocation: https://", out found);
			if (found) {
				StringBuilder h = new StringBuilder();
				int x = s.ReadByte();
				while ((x != -1) && (x != ':') && (x != '/')) {
					h.Append((char) x);
					x = s.ReadByte();
				}
				if (x == ':') {
					port = 0;
					x = s.ReadByte();
					while ((x != -1) && (x != '/')) {
						port = 10 * port + x - '0';
						x = s.ReadByte();
					}
				}
				host = h.ToString();
			}
		}

		static bool ValidateRemoteCertificate (
			object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)
		{
			return policyErrors == SslPolicyErrors.None;
		}

		public IWebProxy Proxy {
			get {
				return proxyClient != null? proxyClient.Proxy: null;
			}
			set {
				proxyClient = value != null? new ProxyClient(value): null;
			}
		}

		public SslStream AcceptSslStream () {
			if (aborted) {
				throw new InvalidOperationException();
			}
			try {
				string host = this.host;
				int port = this.port;
				SslStream result = null;
				bool acceptable = false;
				int[] x = new int[3];
				do {
					if (proxyClient == null) {
						listener = new Socket(
							AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
						if (!aborted) {
							listener.Connect(host, port);
						}
					} else {
						listener = proxyClient.ConnectSocket(host, port);
					}
					if (!aborted) {
						listener.NoDelay = true;
						result = new SslStream(
							new NetworkStream(listener, true),
							false, ValidateRemoteCertificate);
						result.AuthenticateAsClient(host);
						do {
							result.Write(Encoding.ASCII.GetBytes(
								"POST /" + id + " HTTP/1.1\r\n" +
								"Upgrade: PTTH/1.0\r\n" +
								"Connection: Upgrade\r\n" +
								"Host: " + host + "\r\n\r\n"));
							for (int i = 0; i != 12; i++) {
								x[i % 3] = result.ReadByte();
							}
							if ((x[0] == '3') && (x[1] == '0') && (x[2] == '7')) {
								FindLocation(result, out host, out port);
							}
							StreamHelper.Find(result, "\r\n\r\n", out acceptable);
						} while (acceptable && ((x[0] == '2') && (x[1] == '0') && (x[2] == '4')));
						if (!acceptable || (x[0] != '1') || (x[1] != '0') || (x[2] != '1')) {
							result.Close();
							result = null;
						}
					}
				} while (acceptable && ((x[0] == '3') && (x[1] == '0') && (x[2] == '7')));
				return result;
			} finally {
				listener = null;
			}
		}

		public IAsyncResult BeginAcceptSslStream (AsyncCallback callback, object state) {
			if (aborted) {
				throw new InvalidOperationException();
			}
			return AsyncResult.New(this, callback, state);
		}

		public SslStream EndAcceptSslStream (IAsyncResult r) {
			if (aborted) {
				throw new InvalidOperationException();
			}
			AsyncResult ar = r as AsyncResult;
			if (ar == null) {
				throw new ArgumentException();
			}
			return ar.End();
		}

		public void Abort () {
			aborted = true;
			try {
				listener.Close();
			} catch {}
			try {
				proxyClient.Abort();
			} catch {}
		}
	}
}