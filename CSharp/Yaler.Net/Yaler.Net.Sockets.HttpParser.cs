// Copyright (c) 2012, Yaler GmbH, Switzerland
// All rights reserved

namespace Yaler.Net.Sockets {

	using System;
	using System.Collections;
	using System.Net.Sockets;

	sealed class HttpParser {

		HttpParser () {}

		static readonly char[] whitespace = new char[] {'\t', ' '};

		internal static readonly int ResponseTooLong = 1, InvalidResponse = 2, InvalidHeader = 3;

		internal static void GetContentLength (Hashtable headers,
			out int contentLength, out int parserError)
		{
			contentLength = 0;
			parserError = 0;
			string value = headers["Content-Length"] as string;
			if (value != null) {
				bool valid = Int32.TryParse(value, out contentLength);
				if (!valid || (contentLength < 0)) {
					parserError = InvalidHeader;
				}
			}
		}

		static void ReceiveLine (Socket s, out string line, out int socketError, out int parserError) {
			line = null;
			socketError = 0;
			parserError = 0;
			byte[] buffer = new byte[1];
			char[] lineBuffer = new char[4096];
			int lineLength = 0;
			do {
				try {
					s.Receive(buffer);
				} catch (SocketException e) {
					socketError = e.ErrorCode;
				}
				if (socketError == 0) {
					if (lineLength != lineBuffer.Length) {
						lineBuffer[lineLength] = (char) buffer[0];
						lineLength++;
					} else {
						parserError = ResponseTooLong;
					}
				}
			} while ((socketError == 0) && (parserError == 0) && (buffer[0] != '\n'));
			if ((socketError == 0) && (parserError == 0)) {
				if ((lineLength >= 2) && (lineBuffer[lineLength - 2] == '\r')) {
					line = new String(lineBuffer, 0, lineLength - 2);
				} else {
					line = new String(lineBuffer, 0, lineLength - 1);
				}
			}
		}

		internal static void ReceiveStatusLine (Socket s,
			out string httpVersion, out int statusCode, out string reasonPhrase,
			out int socketError, out int parserError)
		{
			httpVersion = null;
			statusCode = 0;
			reasonPhrase = null;
			string line;
			ReceiveLine(s, out line, out socketError, out parserError);
			if ((socketError == 0) && (parserError == 0) && (line.Length >= 12)) {
				httpVersion = line.Substring(0, 8);
				bool valid = Int32.TryParse(line.Substring(9, 3), out statusCode);
				if (!valid || (statusCode < 100)) {
					parserError = InvalidResponse;
				}
				if ((parserError == 0) && (line.Length > 13)) {
					reasonPhrase = line.Substring(13, line.Length - 13);
				}
			} else {
				parserError = InvalidResponse;
			}
		}

		internal static void ReceiveHeaders (Socket s,
			out Hashtable headers, out int socketError, out int parserError)
		{
			headers = new Hashtable();
			string line, name = null, value = null;
			ReceiveLine(s, out line, out socketError, out parserError);
			while ((socketError == 0) && (parserError == 0) && (line != "")) {
				int i = line.IndexOf(':');
				if (i != -1) {
					name = line.Substring(0, i);
					value = line.Substring(i + 1).Trim(whitespace);
					ReceiveLine(s, out line, out socketError, out parserError);
					while ((socketError == 0) && (parserError == 0) && (line != "")
						&& ((line[0] == '\t') || (line[0] == ' '))) {
						value += " " + line.Trim(whitespace);
						ReceiveLine(s, out line, out socketError, out parserError);
					}
					if ((socketError == 0) && (parserError == 0)) {
						headers[name] = value;
					}
				} else {
					parserError = InvalidHeader;
				}
			}
		}

		internal static void SkipBody (Socket s, int contentLength, out int socketError) {
			socketError = 0;
			byte[] buffer = new byte[1];
			while ((socketError == 0) && (contentLength != 0)) {
				try {
					s.Receive(buffer);
				} catch (SocketException e) {
					socketError = e.ErrorCode;
				}
				if (socketError == 0) {
					contentLength--;
				}
			}
		}
	}

}