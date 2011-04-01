// Copyright (c) 2011, Oberon microsystems AG, Switzerland
// All rights reserved

namespace Yaler.Net.Sockets {
	using System;
	using System.IO;
	using System.Net.Sockets;
	using System.Text;
	using System.Threading;

	sealed class AsyncResult : IAsyncResult {
		readonly object state;
		readonly AsyncCallback callback;
		readonly ManualResetEvent waitHandle;
		volatile bool isCompleted;
		Exception exception;
		Socket result;

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
		Socket listener;

		public YalerListener (string host, int port, string id) {
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

		void Find (string pattern, Socket s, out bool found) {
			int[] x = new int[pattern.Length];
			byte[] b = new byte[1];
			int i = 0, j = 0, t = 0;
			do {
				found = true;
				for (int k = 0; (k != pattern.Length) && found; k++) {
					if (i + k == j) {
						int n = s.Receive(b);
						x[j % x.Length] = n != 0? b[0]: -1;
						j++;
					}
					t = x[(i + k) % x.Length];
					found = pattern[k] == t;
				}
				i++;
			} while (!found && (t != -1));
		}

		void FindLocation (Socket s, out string host, out int port) {
			host = null;
			port = 80;
			bool found;
			Find("\r\nLocation: http://", s, out found);
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

		public Socket AcceptSocket () {
			if (aborted) {
				throw new InvalidOperationException();
			} else {
				string host = this.host;
				int port = this.port;
				Socket s;
				bool acceptable;
				int[] x = new int[3];
				byte[] b = new byte[1];
				do {
					listener = new Socket(
						AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					listener.NoDelay = true;
					listener.Connect(host, port);
					s = listener;
					do {
						s.Send(Encoding.ASCII.GetBytes(
							"POST /" + id + " HTTP/1.1\r\n" +
							"Upgrade: PTTH/1.0\r\n" +
							"Connection: Upgrade\r\n" +
							"Host: " + host + "\r\n\r\n"));
						for (int j = 0; j != 12; j++) {
							int n = s.Receive(b);
							x[j % 3] = n != 0? b[0]: -1;
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

		public IAsyncResult BeginAcceptSocket (AsyncCallback callback, object state) {
			if (aborted) {
				throw new InvalidOperationException();
			} else {
				return AsyncResult.New(this, callback, state);
			}
		}

		public Socket EndAcceptSocket (IAsyncResult r) {
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