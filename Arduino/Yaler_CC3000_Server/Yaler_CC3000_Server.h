// Copyright (c) 2014, Yaler GmbH, Switzerland
// All rights reserved

#ifndef Yaler_CC3000_Server_H
#define Yaler_CC3000_Server_H

#include "Server.h"
#include "Adafruit_CC3000.h"

class Yaler_CC3000_Server : 
public Server {
private:
  const char *_host;
  uint16_t _port;
  const char *_id;
  Adafruit_CC3000_Client _client;
  void accept();
public:
  Yaler_CC3000_Server(const char *host, uint16_t, const char *id);
  Adafruit_CC3000_ClientRef available();  
  virtual void begin();
  virtual size_t write(uint8_t);
  virtual size_t write(const uint8_t *buf, size_t size);
  using Print::write;
};

#endif