#include <Arduino.h>
#include "USBAdapter.h"
#include "BLEAdapterUART.h"
#include "PanTiltModule.h"

static const uint32_t BAUD = 115200;

static USBAdapter usb;
static BLEAdapterUART ble;

static void sendUsbJsonLine(const String& line) {
  Serial.print(line);
  Serial.print('\n');
}

static void sendBleJsonLine(const String& line) {
  if (!ble.isConnected()) return;
  ble.sendLine(line);
}

static void panTiltOut(PanTiltDest dest, const String& line) {
  if (dest == PanTiltDest::USB) sendUsbJsonLine(line);
  else sendBleJsonLine(line);
}

static void onUsbFrame(const uint8_t* data, size_t len, const UsbMeta& meta) {
  String payload;
  payload.reserve(len);
  for (size_t i = 0; i < len; i++) payload += (char)data[i];

  // Route everything to pan/tilt for now
  PanTilt_handleLine(payload, false);
}

static void onUsbEvent(const char* event, const UsbMeta& meta) {
  String out;
  out.reserve(160);
  out += "{\"ok\":true,\"event\":\"usb_";
  out += event;
  out += "\",\"ts\":";
  out += String(meta.timestamp_ms);
  out += "}";
  sendUsbJsonLine(out);
}

static void onBleFrame(const uint8_t* data, size_t len, const BleMeta& meta) {
  String payload;
  payload.reserve(len);
  for (size_t i = 0; i < len; i++) payload += (char)data[i];

  // Route everything to pan/tilt for now
  PanTilt_handleLine(payload, true);
}

static void onBleEvent(const char* event, const BleMeta& meta) {
  String out;
  out.reserve(180);
  out += "{\"ok\":true,\"event\":\"ble_";
  out += event;
  out += "\",\"ts\":";
  out += String(meta.timestamp_ms);
  out += ",\"connId\":";
  out += String(meta.conn_id);
  out += "}";

  // Always visible on USB; optionally visible on BLE when connected
  sendUsbJsonLine(out);
  sendBleJsonLine(out);
}

void setup() {
  Serial.begin(BAUD);
  delay(200);

  // Wire pan/tilt output routing
  PanTilt_setOutput(panTiltOut);

  // USB JSONL input
  UsbConfig ucfg;
  ucfg.max_frame_len = 4096;
  ucfg.flood_max_fps = 120;
  ucfg.require_newline = true;
  ucfg.echo = false;
  usb.begin(Serial, ucfg, onUsbFrame, onUsbEvent);

  // BLE JSONL input
  BleConfig bcfg;
  bcfg.device_name = "SailDrone";
  bcfg.max_frame_len = 4096;
  bcfg.flood_max_fps = 120;
  bcfg.require_newline = true;
  ble.begin(bcfg, onBleFrame, onBleEvent);

  // Initialize pan/tilt (servos + config load)
  PanTilt_begin(3, 4);

  sendUsbJsonLine("{\"ok\":true,\"event\":\"fullcontroller_ready\"}");
}

void loop() {
  usb.loop();
  ble.loop();
  PanTilt_loop();

  static uint32_t last = 0;
  uint32_t now = millis();
  if (now - last >= 5000) {
    last = now;
    auto s = ble.stats();

    String out;
    out.reserve(220);
    out += "{\"ok\":true,\"event\":\"stats\",\"ble\":{";
    out += "\"rx_frames\":"; out += String(s.rx_frames);
    out += ",\"rx_bytes\":"; out += String(s.rx_bytes);
    out += ",\"tx_bytes\":"; out += String(s.tx_bytes);
    out += ",\"connects\":"; out += String(s.connects);
    out += ",\"disconnects\":"; out += String(s.disconnects);
    out += "}}";
    sendUsbJsonLine(out);
  }
}
