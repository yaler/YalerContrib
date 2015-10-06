// Copyright (c) 2015, Yaler GmbH, Switzerland
// All rights reserved

#ifndef yalerWiFiserver_h
#define yalerWiFiserver_h

#include "Server.h"

class WiFiClient;

class YalerESP8266WiFiServer : 
public Server {
private:
  const char *_host;
  uint16_t _port;
  const char *_id;
  void accept();
public:
  YalerESP8266WiFiServer(const char *host, uint16_t, const char *id);
  WiFiClient available();
  virtual void begin();
  virtual size_t write(uint8_t);
  virtual size_t write(const uint8_t *buf, size_t size);
  using Print::write;
};

#endif