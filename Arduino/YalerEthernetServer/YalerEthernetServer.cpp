// Copyright (c) 2014, Yaler GmbH, Switzerland
// All rights reserved

#include "EthernetClient.h"
#include "YalerEthernetServer.h"

YalerEthernetServer::YalerEthernetServer(const char *host, uint16_t port, const char *id)
{
  _host = host;
  _port = port;
  _id = id;
}

void YalerEthernetServer::begin()
{
  // skip
}

EthernetClient YalerEthernetServer::available()
{
  EthernetClient client;
  boolean acceptable;
  const char *host = _host;
  uint16_t port = _port;
  int x[3];
  //Serial.println("connecting...");
  acceptable = client.connect(host, port);
  if (acceptable) {
    //Serial.println("connected");
    do {
      //Serial.println("sending request...");
      client.print("POST /");
      client.print(_id);
      client.print(" HTTP/1.1\r\n");
      client.print("Upgrade: PTTH/1.0\r\n");
      client.print("Connection: Upgrade\r\n");
      client.print("Host: ");
      client.print(host);
      client.print("\r\n\r\n");
      //Serial.println("receiving response...");
      boolean timeout = false;
      for (int j = 0; !timeout && (j != 12); j++) {
        int count = -32500;
        while (!timeout && !client.available()) {
          delay(1);
          if (count == 32500) { // 75000 ms
            timeout = true;
            //Serial.print("timeout!");
          }
          count++;
        }
        if (!timeout) {
          x[j % 3] = client.read();
          //Serial.print((char) x[j % 3]);
        }
      }
      //Serial.println();
      acceptable = (!timeout && client.find("\r\n\r\n"));
    } while (acceptable && ((x[0] == '2') && (x[1] == '0') && (x[2] == '4')));
    if (!acceptable || (x[0] != '1') || (x[1] != '0') || (x[2] != '1')) {
      client.stop();
      client = NULL;
      //Serial.println("client stopped");
    }
  }
  return client;
}

size_t YalerEthernetServer::write(uint8_t b) 
{
  return write(&b, 1);
}

size_t YalerEthernetServer::write(const uint8_t *buffer, size_t size) 
{
  size_t n = 0;
  // TODO
  return n;
}