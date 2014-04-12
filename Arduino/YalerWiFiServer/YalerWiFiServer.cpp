// Copyright (c) 2014, Yaler GmbH, Switzerland
// All rights reserved

#include "WiFiClient.h"
#include "YalerWiFiServer.h"

YalerWiFiServer::YalerWiFiServer(const char *host, uint16_t port, const char *id)
{
  _host = host;
  _port = port;
  _id = id;
}

void YalerWiFiServer::begin()
{
  // skip
}

WiFiClient YalerWiFiServer::available()
{
  WiFiClient client;
  boolean acceptable;
  const char *host = _host;
  uint16_t port = _port;
  int x[3];
  //Serial.println("connecting");
  acceptable = client.connect(host, port);
  if (acceptable) {
    //Serial.println("connected");
    do {
      //Serial.println("sending request");
      client.print("POST /");
      client.print(_id);
      client.print(" HTTP/1.1\r\n");
      client.print("Upgrade: PTTH/1.0\r\n");
      client.print("Connection: Upgrade\r\n");
      client.print("Host: ");
      client.print(host);
      client.print("\r\n\r\n");
      while (!client.available()) {}
      //Serial.println("receiving response");
      for (int j = 0; j != 12; j++) {
        x[j % 3] = client.read();
        //Serial.print((char) x[j % 3]);
      }
      //Serial.println();
      acceptable = client.find("\r\n\r\n");
    } while (acceptable && ((x[0] == '2') && (x[1] == '0') && (x[2] == '4')));
    if (!acceptable || (x[0] != '1') || (x[1] != '0') || (x[2] != '1')) {
      client.stop();
      client = NULL;
    }
  }
  return client;
}

size_t YalerWiFiServer::write(uint8_t b) 
{
  return write(&b, 1);
}

size_t YalerWiFiServer::write(const uint8_t *buffer, size_t size) 
{
  size_t n = 0;
  // TODO
  return n;
}