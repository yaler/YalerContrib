/*
** Copyright (c) 2016, Yaler GmbH, Switzerland
** All rights reserved
*/

#include <errno.h>
#include <fcntl.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>

#ifdef _WIN32
	#include <winsock2.h>
	#include <ws2tcpip.h>
	#define SHUT_WR SD_SEND
	#define close closesocket
	#define snprintf _snprintf
	typedef SOCKET socket_t;
#else
	#include <netdb.h>
	#include <netinet/in.h>
	#include <sys/socket.h>
	#include <unistd.h>
	#define INVALID_SOCKET -1
	#define SOCKET_ERROR -1
	typedef int socket_t;
#endif

#define TIMEOUT 75

void* alloc (void* ptr, size_t size) {
	void *r = realloc(ptr, size);
	if (r == 0) {
		exit(EXIT_FAILURE);
	}
	return r;
}

int set_nonblocking (socket_t s) {
	int r;
	#ifdef _WIN32
		u_long v = 1;
		r = ioctlsocket(s, FIONBIO, &v);
	#else
		r = fcntl(s, F_SETFL, O_NONBLOCK);
	#endif
	return r;
}

int connect_socket (socket_t s, struct sockaddr *addr, size_t addrlen) {
	int r = connect(s, addr, addrlen);
	if (r == SOCKET_ERROR) {
		#ifdef _WIN32
			if (WSAGetLastError() == WSAEWOULDBLOCK) {
				r = 0;
			}
		#else
			if (errno == EINPROGRESS) {
				r = 0;
			}
		#endif
	}
	return r;
}

int recv_char (socket_t s) {
	int r, n; fd_set readfds; struct timeval tv; char c;
	r = SOCKET_ERROR;
	FD_ZERO(&readfds);
	FD_SET(s, &readfds);
	tv.tv_sec = TIMEOUT;
	tv.tv_usec = 0;
	n = select(s + 1, &readfds, 0, 0, &tv);
	if (n > 0) {
		n = recv(s, &c, 1, 0);
		if (n > 0) {
			r = c;
		}
	}
	return r;
}

int send_buffer (socket_t s, char *buffer, size_t length) {
	size_t i; int n; fd_set writefds; struct timeval tv;
	i = 0; n = 1;
	while ((i != length) && (n > 0)) {
		FD_ZERO(&writefds);
		FD_SET(s, &writefds);
		tv.tv_sec = TIMEOUT;
		tv.tv_usec = 0;
		n = select(s + 1, 0, &writefds, 0, &tv);
		if (n > 0) {
			n = send(s, &buffer[i], length - i, 0);
			if (n > 0) {
				i += n;
			}
		}
	}
	return i == length? 0: SOCKET_ERROR;
}

int find (socket_t s, char* pattern) {
	int i, j, k, l, p, c, x;
	i = 0; j = 0; k = 0; l = strlen(pattern); p = 0; c = 0; x = 0;
	while ((k != l) && (c >= 0)) {
		if (i + k == j) {
			c = x = recv_char(s);
			p = i;
			j++;
		} else if (i + k == j - 1) {
			c = x;
		} else {
			c = pattern[i + k - p];
		}
		if (pattern[k] == c) {
			k++;
		} else {
			k = 0;
			i++;
		}
	}
	return k == l;
}

void get_location (socket_t s, char** host, char** port) {
	int n = 128;
	*host = (char *) alloc(0, n);
	*port = (char *) alloc(0, 6);
	if (find(s, "\r\nLocation: http://")) {
		int i, x;
		i = 0;
		x = recv_char(s);
		while ((x >= 0) && (x != ':') && (x != '/')) {
			if (i == n - 1) {
				n *= 2;
				*host = (char *) alloc(*host, n);
			}
			(*host)[i] = (char) x;
			i++;
			x = recv_char(s);
		}
		(*host)[i] = 0;
		if (x == ':') {
			i = 0;
			x = recv_char(s);
			while ((i != 5) && ('0' <= x) && (x <= '9')) {
				(*port)[i] = (char) x;
				i++;
				x = recv_char(s);
			}
			(*port)[i] = 0;
		} else {
			(*port)[0] = '8';
			(*port)[1] = '0';
			(*port)[2] = 0;
		}
	} else {
		(*host)[0] = 0;
		(*port)[0] = 0;
	}
}

int yaler_accept (char* host, char* port, char* id) {
	int done, r, i, n; char *b, *h, *p; socket_t s;
	s = INVALID_SOCKET;
	n = 128;
	b = (char *) alloc(0, n);
	h = (char *) alloc(0, strlen(host) + 1);
	p = (char *) alloc(0, strlen(port) + 1);
	strcpy(h, host);
	strcpy(p, port);
	do {
		struct addrinfo hints, *ai;
		done = 0;
		memset(&hints, 0, sizeof hints);
		hints.ai_family = AF_UNSPEC;
		hints.ai_socktype = SOCK_STREAM;
		r = getaddrinfo(h, p, &hints, &ai);
		if (r == 0) {
			s = socket(ai->ai_family, ai->ai_socktype, ai->ai_protocol);
			if (s != INVALID_SOCKET) {
				r = set_nonblocking(s);
				if (r != SOCKET_ERROR) {
					r = connect_socket(s, ai->ai_addr, ai->ai_addrlen);
					if (r != SOCKET_ERROR) {
						do {
							do {
								r = snprintf(b, n,
									"POST /%s HTTP/1.1\r\n"
									"Upgrade: PTTH/1.0\r\n"
									"Connection: Upgrade\r\n"
									"Host: %s:%s\r\n\r\n",
									id, h, p);
								if ((r < 0) || (n <= r)) {
									r = -1;
									n *= 2;
									b = (char *) alloc(b, n);
								}
							} while (r < 0);
							r = send_buffer(s, b, r);
							if (r != SOCKET_ERROR) {
								i = 0;
								do {
									r = recv_char(s);
									if (r != SOCKET_ERROR) {
										b[i % 3] = (char) r;
										i++;
									}
								} while ((r != SOCKET_ERROR) && (i != 12));
								if (r != SOCKET_ERROR) {
									if ((b[0] == '3') && (b[1] == '0') && (b[2] == '7')) {
										free(h);
										free(p);
										get_location(s, &h, &p);
									}
									done = find(s, "\r\n\r\n");
								}
							}
						} while (done && (b[0] == '2') && (b[1] == '0') && (b[2] == '4'));
					}
				}
				if (!done || (b[0] != '1') || (b[1] != '0') || (b[2] != '1')) {
					close(s);
					s = INVALID_SOCKET;
				}
			}
			freeaddrinfo(ai);
		}
	} while (done && (b[0] == '3') && (b[1] == '0') && (b[2] == '7'));
	free(p);
	free(h);
	free(b);
	return s;
}

int main (int argc, char* argv[]) {
	#ifdef _WIN32
		WSADATA d;
		if (WSAStartup(MAKEWORD(1, 1), &d) != 0) {
			exit(EXIT_FAILURE);
		}
	#endif
	if (argc >= 3) {
		socket_t s = yaler_accept(argv[1], "80", argv[2]);
		while (s != INVALID_SOCKET) {
			if (find(s, "\r\n\r\n")) {
				time_t t; struct tm *lt; char b[66]; int r;
				t = time(0);
				lt = localtime(&t);
				r = snprintf(b, sizeof b,
					"HTTP/1.1 200 OK\r\n"
					"Connection: close\r\n"
					"Content-Length: 8\r\n\r\n"
					"%02d:%02d:%02d",
					lt->tm_hour, lt->tm_min, lt->tm_sec);
				r = send_buffer(s, b, r);
				if (r != SOCKET_ERROR) {
					shutdown(s, SHUT_WR);
				}
			}
			close(s);
			s = yaler_accept(argv[1], "80", argv[2]);
		}
	}
	#ifdef _WIN32
		WSACleanup();
	#endif
	exit(EXIT_FAILURE);
}
