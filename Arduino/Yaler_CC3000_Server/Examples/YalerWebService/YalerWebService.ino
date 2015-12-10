// Copyright (c) 2013-2014, Limor Fried, Kevin Townsend for
// Adafruit Industries & Tony DiCola (tony@tonydicola.com)
//
// Copyright (c) 2015, Yaler GmbH, Switzerland
//
// All rights reserved

#include <SPI.h>
#include <Adafruit_CC3000.h>
#include <Yaler_CC3000_Server.h>

#define WLAN_SSID "SSID" // max 32 char
#define WLAN_PASS "PASSWORD"
#define WLAN_SECURITY WLAN_SEC_WPA2 // WLAN_SEC_UNSEC, WLAN_SEC_WEP, WLAN_SEC_WPA or WLAN_SEC_WPA2

// CS = 10, IRQ = 3 (interrupt pin), VBAT = 5, SCK = 13, MISO = 12, MOSI = 11
Adafruit_CC3000 cc3000 = Adafruit_CC3000(10, 3, 5, SPI_CLOCK_DIVIDER);

// Local Web server at http://LOCAL_IP/ (e.g. http://192.168.0.7/)
//Adafruit_CC3000_Server server(80);

// Get a relay domain at https://yaler.net/ to replace "RELAY_DOMAIN" below
// Access Yaler_CC3000_Server via Yaler at http://RELAY_DOMAIN.try.yaler.io/
Yaler_CC3000_Server server("try.yaler.io", 80, "RELAY_DOMAIN");

void setup (void) {
  Serial.begin(9600);
  Serial.println(F("Initializing CC3000..."));
  if (!cc3000.begin()) {
    Serial.println(F("Begin failed. Check your wiring."));
    while(1);
  }
  Serial.print(F("Connecting to access point..."));
  if (!cc3000.connectToAP(WLAN_SSID, WLAN_PASS, WLAN_SECURITY)) {
    Serial.println(F("Connecting to access point failed."));
    while(1);
  }
  Serial.println(F("Requesting DHCP..."));
  while (!cc3000.checkDHCP()) {
    delay(100); // TODO: DHCP timeout
  }
  uint32_t ipAddress, netmask, gateway, dhcpserv, dnsserv;
  while (!cc3000.getIPAddress(&ipAddress, &netmask, &gateway, &dhcpserv, &dnsserv)) {
    Serial.println(F("Unable to get IP address."));
    delay(1000);
  }
  cc3000.printIPdotsRev(ipAddress);
  Serial.println();
  server.begin();
}

void sendResponse(Adafruit_CC3000_ClientRef client) {
  client.fastrprint(F("HTTP/1.1 200 OK\r\n"));
  client.fastrprint(F("Connection: close\r\n"));
  client.fastrprint(F("Content-Length: 5\r\n"));
  client.fastrprint(F("\r\n"));
  client.fastrprint(F("Hello"));
}

void loop (void) {
  Serial.println(F("Listening..."));
  Adafruit_CC3000_ClientRef client = server.available();
  if (client && client.connected()) {
    Serial.println(F("Client connected."));
    client.find("\r\n\r\n"); // Consume incoming request
    sendResponse(client);
    delay(100); // Data is sent asynchronously
    client.close();
  }
}

