// Copyright (c) 2015, Yaler GmbH, Switzerland
// All rights reserved

#ifndef YalerWiFi101Server_h
#define YalerWiFi101Server_h

#include "Server.h"

class WiFiClient;

class YalerWiFi101Server : 
public Server {
private:
  const char *_host;
  uint16_t _port;
  const char *_id;
  void accept();
public:
  YalerWiFi101Server(const char *host, uint16_t, const char *id);
  WiFiClient available();
  virtual void begin();
  virtual size_t write(uint8_t);
  virtual size_t write(const uint8_t *buf, size_t size);
  using Print::write;
};

#endif