// Copyright (c) 2011, Yaler GmbH, Switzerland
// All rights reserved

namespace Yaler.Net.Sockets {

	using System;
	using System.Runtime.InteropServices;

	sealed class SSPI {
		SSPI () {}

		internal static readonly int
			MAX_TOKEN_SIZE = 2888,
			SEC_I_COMPLETE_NEEDED = 0x90313,
			SEC_I_COMPLETE_AND_CONTINUE = 0x90314,
			SEC_WINNT_AUTH_IDENTITY_UNICODE = 2;

		internal static readonly uint
			ISC_REQ_REPLAY_DETECT = 4,
			ISC_REQ_SEQUENCE_DETECT = 8,
			ISC_REQ_CONFIDENTIALITY = 16,
			ISC_REQ_INTEGRITY = 65536,
			SECPKG_CRED_OUTBOUND = 2,
			SECBUFFER_TOKEN = 2,
			SECURITY_NETWORK_DREP = 0,
			STANDARD_CONTEXT_ATTRIBUTES =
				ISC_REQ_REPLAY_DETECT |
				ISC_REQ_SEQUENCE_DETECT |
				ISC_REQ_CONFIDENTIALITY |
				ISC_REQ_INTEGRITY;

		[StructLayout(LayoutKind.Sequential)]
		internal struct SecBuffer {
			internal int size;
			internal uint type;
			internal IntPtr buffer;

			internal SecBuffer (int length) {
				size = length;
				type = SECBUFFER_TOKEN;
				buffer = Marshal.AllocHGlobal(size);
			}

			internal SecBuffer (byte[] b) {
				size = b.Length;
				type = SECBUFFER_TOKEN;
				buffer = Marshal.AllocHGlobal(size);
				Marshal.Copy(b, 0, buffer, size);
			}

			internal void Dispose () {
				if (buffer != IntPtr.Zero) {
					Marshal.FreeHGlobal(buffer);
					buffer = IntPtr.Zero;
				}
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct SecBufferDesc {
			internal int version;
			internal int count;
			internal IntPtr buffers;

			internal SecBufferDesc (int size) {
				version = 0;
				count = 1;
				SecBuffer sb = new SecBuffer(size);
				buffers = Marshal.AllocHGlobal(Marshal.SizeOf(sb));
				Marshal.StructureToPtr(sb, buffers, false);
			}

			internal SecBufferDesc (byte[] b) {
				version = 0;
				count = 1;
				SecBuffer sb = new SecBuffer(b);
				buffers = Marshal.AllocHGlobal(Marshal.SizeOf(sb));
				Marshal.StructureToPtr(sb, buffers, false);
			}

			internal void Dispose () {
				if (buffers != IntPtr.Zero) {
					SecBuffer sb = (SecBuffer) Marshal.PtrToStructure(buffers, typeof(SecBuffer));
					sb.Dispose();
					Marshal.FreeHGlobal(buffers);
					buffers = IntPtr.Zero;
				}
			}

			internal byte[] ToByteArray () {
				byte[] b = null;
				if (buffers != IntPtr.Zero) {
					SecBuffer sb = (SecBuffer) Marshal.PtrToStructure(buffers, typeof(SecBuffer));
					b = new byte[sb.size];
					Marshal.Copy(sb.buffer, b, 0, sb.size);
				}
				return b;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct SecHandle {
			IntPtr lower;
			IntPtr upper;
		}

		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
		internal struct SEC_WINNT_AUTH_IDENTITY {
			internal string User;
			internal int UserLength;
			internal string Domain;
			internal int DomainLength;
			internal string Password;
			internal int PasswordLength;
			internal int Flags;
		}

		[DllImport("secur32.dll", CharSet=CharSet.Auto, SetLastError = true)]
		internal static extern int AcquireCredentialsHandle (
			string principal,
			string package,
			uint credentialUse,
			IntPtr logonId,
			IntPtr authData,
			IntPtr getKeyFunction,
			IntPtr getKeyArgument,
			out SecHandle credential,
			out ulong expiry);

		[DllImport("secur32.dll", CharSet=CharSet.Auto, SetLastError = true)]
		internal static extern int AcquireCredentialsHandle (
			string principal,
			string package,
			uint credentialUse,
			IntPtr logonId,
			ref SEC_WINNT_AUTH_IDENTITY authData,
			IntPtr getKeyFunction,
			IntPtr getKeyArgument,
			out SecHandle credential,
			out ulong expiry);

		[DllImport("secur32.dll", CharSet=CharSet.Auto, SetLastError = true)]
		internal static extern int InitializeSecurityContext (
			ref SecHandle credential,
			IntPtr context,
			string targetName,
			uint contextReq,
			uint reserved1,
			uint targetDataRep,
			IntPtr input,
			uint reserved2,
			out SecHandle newContext,
			out SecBufferDesc output,
			out uint contextAttr,
			out ulong expiry);

		[DllImport("secur32.dll", CharSet=CharSet.Auto, SetLastError = true)]
		internal static extern int InitializeSecurityContext (
			ref SecHandle credential,
			ref SecHandle context,
			string targetName,
			uint contextReq,
			uint reserved1,
			uint targetDataRep,
			ref SecBufferDesc input,
			uint reserved2,
			out SecHandle newContext,
			out SecBufferDesc output,
			out uint contextAttr,
			out ulong expiry);

		[DllImport("secur32.dll", SetLastError = true)]
		internal static extern int CompleteAuthToken (
			ref SecHandle context,
			ref SecBufferDesc token);

		[DllImport("secur32.dll", SetLastError = true)]
		internal static extern int FreeCredentialsHandle (
			ref SecHandle credential);

		[DllImport("secur32.dll", SetLastError = true)]
		internal static extern int DeleteSecurityContext (
			ref SecHandle context);
	}

	sealed class NtlmContext: IDisposable {
		SSPI.SEC_WINNT_AUTH_IDENTITY authData;
		SSPI.SecHandle credential, context;
		bool disposed;

		internal NtlmContext (string user, string domain, string password) {
			ulong expiry;
			if ((user != null) || (domain != null) || (password != null)) {
				authData.User = user;
				authData.UserLength = user == null? 0: user.Length;
				authData.Domain = domain;
				authData.DomainLength = domain == null? 0: domain.Length;
				authData.Password = password;
				authData.PasswordLength = password == null? 0: password.Length;
				authData.Flags = SSPI.SEC_WINNT_AUTH_IDENTITY_UNICODE;
				SSPI.AcquireCredentialsHandle(
					null, "NTLM", SSPI.SECPKG_CRED_OUTBOUND,
					IntPtr.Zero, ref authData, IntPtr.Zero, IntPtr.Zero,
					out credential, out expiry);
			} else {
				SSPI.AcquireCredentialsHandle(
					null, "NTLM", SSPI.SECPKG_CRED_OUTBOUND,
					IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
					out credential, out expiry);
			}
		}

		public void Dispose () {
			if (!disposed) {
				SSPI.FreeCredentialsHandle(ref credential);
				SSPI.DeleteSecurityContext(ref context);
				disposed = true;
			}
		}

		~NtlmContext () {
			Dispose();
		}

		internal void Authorize (byte[] inToken, out byte[] outToken) {
			SSPI.SecBufferDesc output = new SSPI.SecBufferDesc(SSPI.MAX_TOKEN_SIZE);
			uint attributes;
			ulong expiry;
			int r;
			if (inToken == null) {
				r = SSPI.InitializeSecurityContext(
					ref credential, IntPtr.Zero, null, SSPI.STANDARD_CONTEXT_ATTRIBUTES, 0,
					SSPI.SECURITY_NETWORK_DREP, IntPtr.Zero, 0,
					out context, out output, out attributes, out expiry);
			} else {
				SSPI.SecBufferDesc input = new SSPI.SecBufferDesc(inToken);
				r = SSPI.InitializeSecurityContext(
					ref credential, ref context, null, SSPI.STANDARD_CONTEXT_ATTRIBUTES, 0,
					SSPI.SECURITY_NETWORK_DREP, ref input, 0,
					out context, out output, out attributes, out expiry);
				input.Dispose();
			}
			if ((r == SSPI.SEC_I_COMPLETE_NEEDED)
				|| (r == SSPI.SEC_I_COMPLETE_AND_CONTINUE))
			{
				SSPI.CompleteAuthToken(ref context, ref output);
			}
			outToken = output.ToByteArray();
			output.Dispose();
		}
	}

}