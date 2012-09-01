// Copyright (c) 2012, Yaler GmbH, Switzerland
// All rights reserved

namespace Yaler.Net.Streams {

	using System;
	using System.IO;
	using System.Net;
	using System.Net.Security;
	using System.Net.Sockets;
	using System.Text;
	using System.Threading;
	using Yaler.Net.Security;
	using Yaler.Net.Sockets;

	public class YalerKeepAliveStream: Stream {
		static readonly byte[] emptyBuffer = new byte[0];

		readonly object readLockObject = new object();
		readonly object writeLockObject = new object();

		readonly Stream stream;
		readonly int keepAliveInterval;

		int remaining;

		public YalerKeepAliveStream (Stream stream, int keepAliveInterval) {
			this.stream = stream;
			this.keepAliveInterval = keepAliveInterval;
			Thread receiverThread = new Thread(ReceiveKeepAlive);
			receiverThread.IsBackground = true;
			receiverThread.Start();
			if (keepAliveInterval >= 0) {
				Thread senderThread = new Thread(SendKeepAlive);
				senderThread.IsBackground = true;
				senderThread.Start();
			}
		}

		public event EventHandler KeepAliveFailed;

		void SendKeepAlive () {
			try {
				while (true) {
					Write(emptyBuffer, 0, 0);
					Thread.Sleep(keepAliveInterval);
				}
			} catch (ObjectDisposedException) {
				// ignore
			} catch (Exception) {
				EventHandler keepAliveFailed = KeepAliveFailed;
				if (keepAliveFailed != null) {
					keepAliveFailed(this, EventArgs.Empty);
				}
			}
		}

		void ReceiveKeepAlive () {
			try {
				lock (readLockObject) {
					while (remaining >= 0) {
						while (remaining > 0) {
							Monitor.Wait(readLockObject);
						}
						DoRead(emptyBuffer, 0, 0);
					}
				}
			} catch (ObjectDisposedException) {
				// ignore
			} catch (Exception) {
				EventHandler keepAliveFailed = KeepAliveFailed;
				if (keepAliveFailed != null) {
					keepAliveFailed(this, EventArgs.Empty);
				}
			}
		}

		public override bool CanRead {
			get {
				return stream.CanRead;
			}
		}

		public override bool CanSeek {
			get {
				return false;
			}
		}

		public override bool CanWrite {
			get {
				return stream.CanWrite;
			}
		}

		public override bool CanTimeout {
			get {
				return stream.CanTimeout;
			}
		}

		public override int ReadTimeout {
			get {
				return stream.ReadTimeout;
			}
			set {
				stream.ReadTimeout = value;
			}
		}

		public override int WriteTimeout {
			get {
				return stream.WriteTimeout;
			}
			set {
				stream.WriteTimeout = value;
			}
		}

		public override long Length {
			get {
				throw new NotSupportedException();
			}
		}

		public override long Position {
			get {
				throw new NotSupportedException();
			}
			set {
				throw new NotSupportedException();
			}
		}

		public override void Close () {
			lock (writeLockObject) {
				stream.Close();
			}
		}

		public override void Flush () {
			lock (writeLockObject) {
				stream.Flush();
			}
		}

		public override long Seek (long offset, SeekOrigin origin) {
			throw new NotSupportedException();
		}

		public override void SetLength (long length) {
			throw new NotSupportedException();
		}

		int DoRead (byte[] buffer, int offset, int count) {
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
					} else {
						remaining = -1;
					}
				} while (remaining == 0);
			}
			if (remaining > 0) {
				int bytesToRead = Math.Min(count, remaining);
				int bufferRead;
				int bufferPos = offset;
				do {
					bufferRead = stream.Read(
						buffer, bufferPos, bytesToRead - (bufferPos - offset));
					bufferPos += bufferRead;
				} while ((bufferRead > 0) && (bufferPos - offset != bytesToRead));
				result = bufferPos - offset;
				remaining -= result;
			}
			return result;
		}

		public override int Read (byte[] buffer, int offset, int count) {
			lock (readLockObject) {
				try {
					return DoRead(buffer, offset, count);
				} finally {
					if (remaining <= 0) {
						Monitor.Pulse(readLockObject);
					}
				}
			}
		}

		public override void Write (byte[] buffer, int offset, int count) {
			lock (writeLockObject) {
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

		void DoAcceptSslStream (object arg) {
			try {
				object[] args = arg as object[];
				YalerSslStreamListener listener =
					args[0] as YalerSslStreamListener;
				result = listener.AcceptSslStream(
					args[1] as RemoteCertificateValidationCallback);
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

		void DoConnectSslStream (object arg) {
			try {
				object[] args = arg as object[];
				YalerSslStreamClient client =
					args[0] as YalerSslStreamClient;
				result = client.ConnectSslStream(
					args[1] as RemoteCertificateValidationCallback);
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

		internal static AsyncResult NewListenerResult (
			YalerSslStreamListener listener,
			RemoteCertificateValidationCallback userCertificateValidationCallback,
			AsyncCallback asyncCallback, object state)
		{
			AsyncResult result = new AsyncResult(asyncCallback, state);
			ThreadPool.QueueUserWorkItem(
				new WaitCallback(result.DoAcceptSslStream),
				new object[] {listener, userCertificateValidationCallback});
			return result;
		}

		internal static AsyncResult NewClientResult (
			YalerSslStreamClient client,
			RemoteCertificateValidationCallback userCertificateValidationCallback,
			AsyncCallback asyncCallback, object state)
		{
			AsyncResult result = new AsyncResult(asyncCallback, state);
			ThreadPool.QueueUserWorkItem(
				new WaitCallback(result.DoConnectSslStream),
				new object[] {client, userCertificateValidationCallback});
			return result;
		}
	}

	public sealed class YalerSslStreamListener {
		readonly YalerSslListener listener;
		readonly byte[] response;

		public YalerSslStreamListener (YalerSslListener listener) {
			this.listener = listener;
			response = Encoding.ASCII.GetBytes(
				"HTTP/1.1 101 Switching Protocols\r\n" +
				"Upgrade: plainsocket\r\n" +
				"Connection: Upgrade\r\n\r\n");
		}

		public SslStream AcceptSslStream (
			RemoteCertificateValidationCallback userCertificateValidationCallback) 
		{
			SslStream result = listener.AcceptSslStream(
				userCertificateValidationCallback);
			if (result != null) {
				bool found;
				StreamHelper.Find(result, "\r\n\r\n", out found);
				if (found) {
					result.Write(response, 0, response.Length);
					result.Flush();
				} else {
					result = null;
				}
			}
			return result;
		}

		public IAsyncResult BeginAcceptSslStream (
			RemoteCertificateValidationCallback userCertificateValidationCallback,
			AsyncCallback asyncCallback, object state) {
			return AsyncResult.NewListenerResult(
				this, userCertificateValidationCallback, asyncCallback, state);
		}

		public SslStream EndAcceptSslStream(IAsyncResult r) {
			AsyncResult ar = r as AsyncResult;
			if (ar == null) {
				throw new ArgumentException();
			}
			return ar.End();
		}
	}

	public sealed class YalerSslStreamClient {
		readonly string host, id;
		readonly int port;
		ProxyClient proxyClient;

		public YalerSslStreamClient (string host, int port, string id) {
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

		public IWebProxy Proxy {
			get {
				return proxyClient != null? proxyClient.Proxy: null;
			}
			set {
				proxyClient = value != null? new ProxyClient(value): null;
			}
		}

		public SslStream ConnectSslStream (
			RemoteCertificateValidationCallback userCertificateValidationCallback) {
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
					false, userCertificateValidationCallback);
				result.AuthenticateAsClient(host);
				result.Write(Encoding.ASCII.GetBytes(
					"OPTIONS /" + id + " HTTP/1.1\r\n" +
					"Upgrade: plainsocket\r\n" +
					"Connection: Upgrade\r\n" +
					"Host: " + host + "\r\n\r\n"));
				result.Flush();
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

		public IAsyncResult BeginConnectSslStream (
			RemoteCertificateValidationCallback userCertificateValidationCallback, 
			AsyncCallback asyncCallback, object state)
		{
			return AsyncResult.NewClientResult(this,
				userCertificateValidationCallback, asyncCallback, state);
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