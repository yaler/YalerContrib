// Copyright (c) 2011, Yaler GmbH, Switzerland
// All rights reserved

namespace Yaler.Net.Streams {

	using System;
	using System.IO;
	using System.Net;
	using System.Net.Security;
	using System.Net.Sockets;
	using System.Security.Cryptography.X509Certificates;
	using System.Text;
	using System.Threading;
	using Yaler.Net.Security;
	using Yaler.Net.Sockets;

	public class YalerKeepAliveStream: Stream {
		static readonly byte[] emptyBuffer = new byte[0];

		readonly object lockObject = new object();

		readonly Stream stream;
		readonly int keepAliveInterval;

		int remaining;

		public YalerKeepAliveStream (Stream stream, int keepAliveInterval) {
			this.stream = stream;
			this.keepAliveInterval = keepAliveInterval;
			if (keepAliveInterval >= 0) {
				(new Thread(KeepAlive)).Start();
			}
		}

		void KeepAlive () {
			bool disposed = false;
			do {
				try {
					Write(emptyBuffer, 0, 0);
				} catch (ObjectDisposedException) {
					disposed = true;
				} catch {
					// ignore other exceptions
				}
				Thread.Sleep(keepAliveInterval);
			} while (!disposed);
		}

		public override bool CanRead {
			get {
				return stream.CanRead;
			}
		}

		public override bool CanSeek {
			get {
				return stream.CanSeek;
			}
		}

		public override bool CanWrite {
			get {
				return stream.CanWrite;
			}
		}

		public override long Length {
			get {
				return stream.Length;
			}
		}

		public override long Position {
			get {
				return stream.Position;
			}
			set {
				stream.Position = value;
			}
		}

		public override void Close () {
			stream.Close();
		}

		public override void Flush () {
			stream.Flush();
		}

		public override long Seek (long offset, SeekOrigin origin) {
			return stream.Seek(offset, origin);
		}

		public override void SetLength (long length) {
			stream.SetLength(length);
		}

		public override int Read (byte[] buffer, int offset, int count) {
			int result = 0;
			if (remaining == 0) {
				byte[] lengthBuffer = new byte[4];
				int headerRead;
				do {
					int headerPos = 0;
					do {
						headerRead = stream.Read(
							lengthBuffer, headerPos, lengthBuffer.Length - headerPos);
						headerPos += headerRead;
					} while ((headerRead > 0) && (headerPos != lengthBuffer.Length));
					if (headerPos == lengthBuffer.Length) {
						remaining = BitConverter.ToInt32(lengthBuffer, 0);
					}
				} while ((headerRead > 0) && (remaining == 0));
			}
			if (remaining != 0) {
				int bytesToRead = Math.Min(count, remaining);
				int bufferRead;
				int bufferPos = offset;
				do {
					bufferRead = stream.Read(buffer, bufferPos, bytesToRead - (bufferPos - offset));
					bufferPos += bufferRead;
				} while ((bufferRead > 0) && (bufferPos - offset != bytesToRead));
				result = bufferPos - offset;
				remaining -= result;
			}
			return result;
		}

		public override void Write (byte[] buffer, int offset, int count) {
			lock (lockObject) {
				byte[] lengthBuffer = BitConverter.GetBytes(count);
				stream.Write(lengthBuffer, 0, lengthBuffer.Length);
				if (count > 0) {
					stream.Write(buffer, offset, count);
				}
			}
		}
	}

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
				result = (listener as YalerSslTcpListener).AcceptSslStream();
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

		void DoConnectSslStream (object client) {
			try {
				result = (client as YalerSslTcpClient).ConnectSslStream();
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

		internal static AsyncResult NewListenerResult (YalerSslTcpListener listener,
			AsyncCallback callback, object state)
		{
			AsyncResult result = new AsyncResult(callback, state);
			ThreadPool.QueueUserWorkItem(new WaitCallback(result.DoAcceptSslStream), listener);
			return result;
		}

		internal static AsyncResult NewClientResult (YalerSslTcpClient client,
			AsyncCallback callback, object state)
		{
			AsyncResult result = new AsyncResult(callback, state);
			ThreadPool.QueueUserWorkItem(new WaitCallback(result.DoConnectSslStream), client);
			return result;
		}
	}

	public sealed class YalerSslTcpListener {
		readonly YalerSslListener listener;
		readonly byte[] response;

		public YalerSslTcpListener (YalerSslListener listener) {
			this.listener = listener;
			response = Encoding.ASCII.GetBytes(
				"HTTP/1.1 101 Switching Protocols\r\n" +
				"Upgrade: TCP\r\n" +
				"Connection: Upgrade\r\n\r\n");
		}

		public SslStream AcceptSslStream () {
			SslStream result = listener.AcceptSslStream();
			bool found;
			StreamHelper.Find(result, "\r\n\r\n", out found);
			if (found) {
				result.Write(response, 0, response.Length);
			} else {
				result = null;
			}
			return result;
		}

		public IAsyncResult BeginAcceptSslStream (AsyncCallback callback, object state) {
			return AsyncResult.NewListenerResult(this, callback, state);
		}

		public SslStream EndAcceptSslStream(IAsyncResult r) {
			AsyncResult ar = r as AsyncResult;
			if (ar == null) {
				throw new ArgumentException();
			}
			return ar.End();
		}
	}

	public sealed class YalerSslTcpClient {
		readonly string host, id;
		readonly int port;
		ProxyClient proxyClient;

		public YalerSslTcpClient (string host, int port, string id) {
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

		public SslStream ConnectSslStream () {
			string host = this.host;
			int port = this.port;
			Socket socket;
			SslStream result;
			bool connected;
			int[] x = new int[3];
			do {
				if (proxyClient == null) {
					socket = new Socket(
						AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					socket.Connect(host, port);
				} else {
					socket = proxyClient.ConnectSocket(host, port);
				}
				socket.NoDelay = true;
				result = new SslStream(
					new NetworkStream(socket, true),
					false, ValidateRemoteCertificate);
				result.AuthenticateAsClient(host);
				result.Write(Encoding.ASCII.GetBytes(
					"OPTIONS /" + id + " HTTP/1.1\r\n" +
					"Upgrade: TCP\r\n" +
					"Connection: Upgrade\r\n" +
					"Host: " + host + "\r\n\r\n"));
				for (int i = 0; i != 12; i++) {
					x[i % 3] = result.ReadByte();
				}
				if ((x[0] == '3') && (x[1] == '0') && (x[2] == '7')) {
					FindLocation(result, out host, out port);
				}
				StreamHelper.Find(result, "\r\n\r\n", out connected);
				if (!connected || (x[0] != '1') || (x[1] != '0') || (x[2] != '1')) {
					result.Close();
					result = null;
				}
			} while (connected && ((x[0] == '3') && (x[1] == '0') && (x[2] == '7')));
			return result;
		}

		public IAsyncResult BeginConnectSslStream (AsyncCallback callback, object state) {
			return AsyncResult.NewClientResult(this, callback, state);
		}

		public SslStream EndConnectSslStream(IAsyncResult r) {
			AsyncResult ar = r as AsyncResult;
			if (ar == null) {
				throw new ArgumentException();
			}
			return ar.End();
		}
	}

}