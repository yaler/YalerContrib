// Copyright (c) 2015, Yaler GmbH, Switzerland
// All rights reserved

#include <SPI.h>
#include <WiFi101.h>
#include <YalerWiFi101Server.h>

char ssid[] = "LOCAL_NETWORK_SSID"; 
char pass[] = "LOCAL_NETWORK_PASSWORD";

int status = WL_IDLE_STATUS;

// Get a free relay domain at http://yaler.net/ to replace RELAY_DOMAIN below
// YalerWiFi101Server becomes accessible at https://RELAY_DOMAIN.yaler.net/
YalerWiFi101Server server("try.yaler.net", 443, "RELAY_DOMAIN");

void setup() {
  Serial.begin(9600);
  Serial.println("Aquiring IP address...");
  if (WiFi.begin(ssid, pass) != WL_CONNECTED) {
    Serial.println("Connecting to WiFi failed.");
  } else {
    Serial.println(WiFi.localIP());
    server.begin();
  }
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
    delay(3000); // Give the Web browser time to receive the data
    client.stop();
  }
}
