/*
** Copyright (c) 2012, Yaler GmbH, Switzerland
** All rights reserved
*/

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>

#ifdef _WIN32
#include <winsock2.h>
#define SHUT_WR SD_SEND
#define close closesocket
#define snprintf _snprintf
#else
#include <netdb.h>
#include <netinet/in.h>
#include <sys/socket.h>
#include <unistd.h>
#endif

void* alloc (void* ptr, size_t size) {
	void *r;
	r = realloc(ptr, size);
	if (r == 0) {
		exit(EXIT_FAILURE);
	}
	return r;
}

int recvc (int socket) {
	char c; int n;
	n = recv(socket, &c, 1, 0);
	return n == 1? c: -1;
}

int find (int socket, char* pattern) {
	int i, j, k, l, p, c, x;
	i = 0; j = 0; k = 0; l = strlen(pattern); p = 0; c = 0; x = 0;
	while ((k != l) && (c != -1)) {
		if (i + k == j) {
			c = x = recvc(socket);
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

void get_location (int socket, char** host, int* port) {
	int n;
	n = 128;
	*host = (char*) alloc(0, n);
	if (find(socket, "\r\nLocation: http://")) {
		int i, x;
		i = 0;
		x = recvc(socket);
		while ((x != -1) && (x != ':') && (x != '/')) {
			if (i == n - 1) {
				n *= 2;
				*host = (char*) alloc(*host, n);
			}
			(*host)[i] = (char) x;
			i++;
			x = recvc(socket);
		}
		(*host)[i] = 0;
		*port = 80;
		if (x == ':') {
			x = recvc(socket);
			if (('0' <= x) && (x <= '9')) {
				*port = x - '0';
				x = recvc(socket);
				while (('0' <= x) && (x <= '9') && (*port != -1)) {
					*port = 10 * *port + x - '0';
					if (*port > 65535) {
						*port = -1;
					}
					x = recvc(socket);
				}
			}
		}
	} else {
		(*host)[0] = 0;
		*port = -1;
	}
}

int yaler_accept (char* host, int port, char* id) {
	int done, s, n; char *b, *h;
	s = -1; n = 128;
	b = (char*) alloc(0, n);
	h = (char*) alloc(0, strlen(host) + 1);
	strcpy(h, host);
	do {
		struct hostent *e;
		done = 0;
		e = gethostbyname(h);
		if (e != 0) {
			struct sockaddr_in a;
			memset(&a, 0, sizeof a);
			memcpy(&a.sin_addr, e->h_addr_list[0], e->h_length);
			a.sin_port = htons((unsigned short) port);
			a.sin_family = AF_INET;
			s = socket(AF_INET, SOCK_STREAM, 0);
			connect(s, (struct sockaddr *) &a, sizeof (struct sockaddr));
			do {
				int r, i;
				do {
					r = snprintf(b, n,
						"POST /%s HTTP/1.1\r\n"
						"Upgrade: PTTH/1.0\r\n"
						"Connection: Upgrade\r\n"
						"Host: %s:%d\r\n\r\n",
						id, h, port);
					if ((r < 0) || (n <= r)) {
						r = -1;
						n *= 2;
						b = (char*) alloc(b, n);
					}
				} while (r == -1);
				send(s, b, r, 0);
				for (i = 0; i != 12; i++) {
					b[i % 3] = (char) recvc(s);
				}
				if ((b[0] == '3') && (b[1] == '0') && (b[2] == '7')) {
					free(h);
					get_location(s, &h, &port);
				}
				done = find(s, "\r\n\r\n");
			} while (done && ((b[0] == '2') && (b[1] == '0') && (b[2] == '4')));
			if (!done || (b[0] != '1') || (b[1] != '0') || (b[2] != '1')) {
				close(s);
				s = -1;
			}
		}
	} while (done && ((b[0] == '3') && (b[1] == '0') && (b[2] == '7')));
	free(h);
	free(b);
	return s;
}

int main (int argc, char* argv[]) {
#ifdef _WIN32
	WSADATA d;
	if (WSAStartup(MAKEWORD(1,1), &d) != 0) {
		exit(EXIT_FAILURE);
	}
#endif
	if (argc >= 3) {
		int s;
		s = yaler_accept(argv[1], 80, argv[2]);
		while (s != -1) {
			if (find(s, "\r\n\r\n")) {
				time_t t; struct tm *lt; char b[66]; int n;
				t = time(0);
				lt = localtime(&t);
				n = snprintf(b, sizeof b,
					"HTTP/1.1 200 OK\r\n"
					"Connection: close\r\n"
					"Content-Length: 8\r\n\r\n"
					"%02d:%02d:%02d",
					lt->tm_hour, lt->tm_min, lt->tm_sec);
				send(s, b, n, 0);
				shutdown(s, SHUT_WR);
			}
			close(s);
			s = yaler_accept(argv[1], 80, argv[2]);
		}
	}
#ifdef _WIN32
	WSACleanup();
#endif
	exit(EXIT_FAILURE);
}
