// Copyright (c) 2013, Oberon microsystems AG, Switzerland
// All rights reserved

#include <SPI.h>
#include <Ethernet.h>
#include <YalerEthernetServer.h>

// Enter a MAC address for your controller below.
// Some Ethernet shields have a MAC address printed on a sticker
byte mac[] = { 0xDE, 0xAD, 0xBE, 0xEF, 0xFE, 0xED };

// Local EthernetServer at http://LOCAL_IP/ (e.g. http://192.168.0.7/)
//EthernetServer server(80);

// Get a free relay domain at http://yaler.net/ to replace RELAY_DOMAIN below
// Public YalerEthernetServer is accessible at http://RELAY_DOMAIN.yaler.net/
YalerEthernetServer server("try.yaler.net", 80, "RELAY_DOMAIN");

void setup() {
  Serial.begin(9600);
  Serial.println("Aquiring IP address...");
  if (Ethernet.begin(mac) == 0) {
    Serial.println("DHCP failed.");
  } else {
    Serial.println(Ethernet.localIP());
    server.begin();
  }
}

void sendResponse(EthernetClient client) {
  client.print("HTTP/1.1 200 OK\r\n");
  client.print("Connection: close\r\n");
  client.print("Content-Length: 5\r\n");
  client.print("\r\n");
  client.print("Hello");
}

void loop() {
  EthernetClient client = server.available();
  if (client && client.connected()) {
    client.find("\r\n\r\n"); // Consume incoming request
    sendResponse(client);
    delay(1); // Give the Web browser time to receive the data
    client.stop();
  }
}
