// Copyright (c) 2011, Oberon microsystems AG, Switzerland
// All rights reserved

namespace Yaler.Net.Security {
	using System;
	using System.IO;
	using System.Net.Security;
	using System.Net.Sockets;
	using System.Security.Cryptography.X509Certificates;
	using System.Text;
	using System.Threading;

	sealed class AsyncResult : IAsyncResult {
		readonly object state;
		readonly AsyncCallback callback;
		readonly ManualResetEvent waitHandle;
		volatile bool isCompleted;
		Exception exception;
		SslStream result;

		AsyncResult (AsyncCallback callback, object state) {
			this.callback = callback;
			this.state = state;
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
		Socket listener;

		public YalerSslListener (string host, int port, string id) {
			this.host = host;
			this.port = port;
			this.id = id;
		}

		public void Abort () {
			aborted = true;
			try {
				listener.Close();
			} catch {}
		}

		void Find (string pattern, Stream s, out bool found) {
			int[] x = new int[pattern.Length];
			int i = 0, j = 0, t = 0;
			do {
				found = true;
				for (int k = 0; (k != pattern.Length) && found; k++) {
					if (i + k == j) {
						x[j % x.Length] = s.ReadByte();
						j++;
					}
					t = x[(i + k) % x.Length];
					found = pattern[k] == t;
				}
				i++;
			} while (!found && (t != -1));
		}

		void FindLocation (Stream s, out string host, out int port) {
			host = null;
			port = 443;
			bool found;
			Find("\r\nLocation: https://", s, out found);
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

		bool ValidateRemoteCertificate (
			object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)
		{
			return policyErrors == SslPolicyErrors.None;
		}

		public SslStream AcceptSslStream () {
			if (aborted) {
				throw new InvalidOperationException();
			} else {
				string host = this.host;
				int port = this.port;
				SslStream s;
				bool acceptable;
				int[] x = new int[3];
				do {
					listener = new Socket(
						AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					listener.NoDelay = true;
					listener.Connect(host, port);
					s = new SslStream(new NetworkStream(listener, true), false,
						new RemoteCertificateValidationCallback(ValidateRemoteCertificate));
					s.AuthenticateAsClient(host);
					do {
						s.Write(Encoding.ASCII.GetBytes(
							"POST /" + id + " HTTP/1.1\r\n" +
							"Upgrade: PTTH/1.0\r\n" +
							"Connection: Upgrade\r\n" +
							"Host: " + host + "\r\n\r\n"));
						for (int j = 0; j != 12; j++) {
							x[j % 3] = s.ReadByte();
						}
						if ((x[0] == '3') && (x[1] == '0') && (x[2] == '7')) {
							FindLocation(s, out host, out port);
						}
						Find("\r\n\r\n", s, out acceptable);
					} while (acceptable && ((x[0] == '2') && (x[1] == '0') && (x[2] == '4')));
					if (!acceptable || (x[0] != '1') || (x[1] != '0') || (x[2] != '1')) {
						s.Close();
						s = null;
					}
				} while (acceptable && ((x[0] == '3') && (x[1] == '0') && (x[2] == '7')));
				listener = null;
				return s;
			}
		}

		public IAsyncResult BeginAcceptSslStream (AsyncCallback callback, object state) {
			if (aborted) {
				throw new InvalidOperationException();
			} else {
				return AsyncResult.New(this, callback, state);
			}
		}

		public SslStream EndAcceptSslStream (IAsyncResult r) {
			if (aborted) {
				throw new InvalidOperationException();
			} else {
				AsyncResult ar = r as AsyncResult;
				if (ar == null) {
					throw new ArgumentException();
				} else {
					return ar.End();
				}
			}
		}
	}
}