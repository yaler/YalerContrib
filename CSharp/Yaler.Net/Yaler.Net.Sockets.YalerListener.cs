// Copyright (c) 2012, Yaler GmbH, Switzerland
// All rights reserved

namespace Yaler.Net.Sockets {
	using System;
	using System.IO;
	using System.Net;
	using System.Net.Sockets;
	using System.Text;
	using System.Threading;

	sealed class AsyncResult: IAsyncResult {
		readonly object state;
		readonly AsyncCallback callback;
		readonly ManualResetEvent waitHandle;
		volatile bool isCompleted;
		Exception exception;
		Socket result;

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

		void DoAcceptSocket (object listener) {
			try {
				result = (listener as YalerListener).AcceptSocket();
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

		internal Socket End () {
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

		internal static AsyncResult New (YalerListener listener, AsyncCallback callback, object state) {
			AsyncResult result = new AsyncResult(callback, state);
			ThreadPool.QueueUserWorkItem(new WaitCallback(result.DoAcceptSocket), listener);
			return result;
		}
	}

	public sealed class YalerListener {
		readonly string host, id;
		readonly int port;
		volatile bool aborted;
		volatile Socket listener;
		volatile ProxyClient proxyClient;

		public YalerListener (string host, int port, string id) {
			this.host = host;
			this.port = port;
			this.id = id;
		}

		static void FindLocation (Socket s, out string host, out int port) {
			host = null;
			port = 80;
			bool found;
			SocketHelper.Find(s, "\r\nLocation: http://", out found);
			if (found) {
				StringBuilder h = new StringBuilder();
				byte[] x = new byte[1];
				int n = s.Receive(x);
				while ((n != 0) && (x[0] != ':') && (x[0] != '/')) {
					h.Append((char) x[0]);
					n = s.Receive(x);
				}
				if (x[0] == ':') {
					port = 0;
					n = s.Receive(x);
					while ((n != 0) && (x[0] != '/')) {
						port = 10 * port + x[0] - '0';
						n = s.Receive(x);
					}
				}
				host = h.ToString();
			}
		}

		public IWebProxy Proxy {
			get {
				return proxyClient != null? proxyClient.Proxy: null;
			}
			set {
				proxyClient = value != null? new ProxyClient(value): null;
			}
		}

		public Socket AcceptSocket () {
			if (aborted) {
				throw new InvalidOperationException();
			}
			try {
				string host = this.host;
				int port = this.port;
				Socket result = null;
				bool acceptable = false;
				int[] x = new int[3];
				byte[] b = new byte[1];
				do {
					if (proxyClient == null) {
						listener = new Socket(
							AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
						listener.ReceiveTimeout = 75000;
						if (!aborted) {
							listener.Connect(host, port);
						}
					} else {
						listener = proxyClient.ConnectSocket(host, port);
					}
					if (!aborted) {
						listener.NoDelay = true;
						result = listener;
						do {
							result.Send(Encoding.ASCII.GetBytes(
								"POST /" + id + " HTTP/1.1\r\n" +
								"Upgrade: PTTH/1.0\r\n" +
								"Connection: Upgrade\r\n" +
								"Host: " + host + "\r\n\r\n"));
							for (int i = 0; i != 12; i++) {
								int n = result.Receive(b);
								x[i % 3] = n != 0? b[0]: -1;
							}
							if ((x[0] == '3') && (x[1] == '0') && (x[2] == '7')) {
								FindLocation(result, out host, out port);
							}
							SocketHelper.Find(result, "\r\n\r\n", out acceptable);
						} while (acceptable && ((x[0] == '2') && (x[1] == '0') && (x[2] == '4')));
						if (acceptable && (x[0] == '1') && (x[1] == '0') && (x[2] == '1')) {
							listener.ReceiveTimeout = 0;
						} else {
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

		public IAsyncResult BeginAcceptSocket (AsyncCallback callback, object state) {
			if (aborted) {
				throw new InvalidOperationException();
			}
			return AsyncResult.New(this, callback, state);
		}

		public Socket EndAcceptSocket (IAsyncResult r) {
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