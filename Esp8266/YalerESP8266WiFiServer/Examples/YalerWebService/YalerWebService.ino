// Copyright (c) 2015, Yaler GmbH, Switzerland
// All rights reserved

#include <SPI.h>
#include <ESP8266WiFi.h>
#include <YalerESP8266WiFiServer.h>

char ssid[] = "LOCAL_NETWORK_SSID"; 
char pass[] = "LOCAL_NETWORK_PASSWORD";

int status = WL_IDLE_STATUS;

// Local WiFiServer at http://LOCAL_IP/ (e.g. http://192.168.0.7/)
//WiFiServer server(80);

// Get a relay domain at https://yaler.net/ to replace RELAY_DOMAIN below
// YalerESP8266WiFiServer is accessible at http://RELAY_DOMAIN.try.yaler.io/
YalerESP8266WiFiServer server("try.yaler.io", 80, "RELAY_DOMAIN");

void setup() {
  Serial.begin(115200);
  Serial.print("\nAcquiring IP address...");
  WiFi.begin(ssid, pass);
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
  }
  Serial.println(WiFi.localIP());
  server.begin();
}

void sendResponse(WiFiClient client) {
  client.print("HTTP/1.1 200 OK\r\n");
  client.print("Connection: close\r\n");
  client.print("Content-Length: 5\r\n");
  client.print("\r\n");
  client.print("Hello");
}

void loop() {
  WiFiClient client = server.available();
  if (client && client.connected()) {
    client.find("\r\n\r\n"); // Consume incoming request
    sendResponse(client);
    delay(500); // Give the Web browser time to receive the data
    client.stop();
  }
}
