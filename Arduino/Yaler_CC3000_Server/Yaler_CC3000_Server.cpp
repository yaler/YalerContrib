// Copyright (c) 2014, Yaler GmbH, Switzerland
// All rights reserved

#include "Adafruit_CC3000.h"
#include "Yaler_CC3000_Server.h"

Yaler_CC3000_Server::Yaler_CC3000_Server(const char *host, uint16_t port, const char *id)
{
  _host = host;
  _port = port;
  _id = id;
}

void Yaler_CC3000_Server::begin()
{
  // skip
}

Adafruit_CC3000_ClientRef Yaler_CC3000_Server::available()
{
  if (_client.connected()) {
    return Adafruit_CC3000_ClientRef(NULL);
  }
  Adafruit_CC3000_Client client;
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
      client.fastrprint(F("POST /"));
      client.fastrprint(_id);
      client.fastrprint(F(" HTTP/1.1\r\n"));
      client.fastrprint(F("Upgrade: PTTH/1.0\r\n"));
      client.fastrprint(F("Connection: Upgrade\r\n"));
      client.fastrprint(F("Host: "));
      client.fastrprint(host);
      client.fastrprint(F("\r\n\r\n"));
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
      // assert !client.connected();
      //Serial.println("client stopped");
    }
  }
  _client = client;
  return Adafruit_CC3000_ClientRef(&_client);
}

size_t Yaler_CC3000_Server::write(uint8_t b) 
{
  return write(&b, 1);
}

size_t Yaler_CC3000_Server::write(const uint8_t *buffer, size_t size) 
{
  size_t n = 0;
  // TODO
  return n;
}