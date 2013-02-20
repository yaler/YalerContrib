#ifndef yalerethernetserver_h
#define yalerethernetserver_h

#include "Server.h"

class EthernetClient;

class YalerEthernetServer : 
public Server {
private:
  const char *_host;
  uint16_t _port;
  const char *_id;
  void accept();
public:
  YalerEthernetServer(const char *host, uint16_t, const char *id);
  EthernetClient available();
  virtual void begin();
  virtual size_t write(uint8_t);
  virtual size_t write(const uint8_t *buf, size_t size);
  using Print::write;
};

#endif