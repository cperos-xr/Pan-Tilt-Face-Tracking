#pragma once
#include <Arduino.h>
#include <BLEDevice.h>
#include <BLEServer.h>
#include <BLEUtils.h>
#include <BLE2902.h>
#include <functional>

// Nordic UART Service UUIDs
#define NUS_SERVICE_UUID        "6E400001-B5A3-F393-E0A9-E50E24DCCA9E"
#define NUS_RX_CHARACTERISTIC   "6E400002-B5A3-F393-E0A9-E50E24DCCA9E"  // Write (phone -> ESP)
#define NUS_TX_CHARACTERISTIC   "6E400003-B5A3-F393-E0A9-E50E24DCCA9E"  // Notify (ESP -> phone)

struct BleMeta {
  const char* source = "ble";
  uint32_t timestamp_ms = 0;
  int conn_id = 0;
};

struct BleStats {
  uint32_t rx_bytes = 0;
  uint32_t rx_frames = 0;
  uint32_t tx_bytes = 0;
  uint32_t dropped_frames = 0;
  uint32_t overlong_frames = 0;
  uint32_t flood_drops = 0;
  uint32_t connects = 0;
  uint32_t disconnects = 0;
};

struct BleConfig {
  String device_name = "ESP32-BLE";
  size_t max_frame_len = 512;
  uint32_t flood_max_fps = 60;
  bool require_newline = true;   // true: newline framed. false: each write is a frame.
};

class BLEAdapterUART;

// Callback classes for BLE events
class BleServerCallbacks : public BLEServerCallbacks {
public:
  BleServerCallbacks(BLEAdapterUART* adapter) : adapter_(adapter) {}
  void onConnect(BLEServer* server) override;
  void onDisconnect(BLEServer* server) override;
private:
  BLEAdapterUART* adapter_;
};

class BleRxCallbacks : public BLECharacteristicCallbacks {
public:
  BleRxCallbacks(BLEAdapterUART* adapter) : adapter_(adapter) {}
  void onWrite(BLECharacteristic* chr) override;
private:
  BLEAdapterUART* adapter_;
};

class BLEAdapterUART {
public:
  using FrameHandler = std::function<void(const uint8_t* data, size_t len, const BleMeta& meta)>;
  using EventHandler = std::function<void(const char* event, const BleMeta& meta)>;

  bool begin(const BleConfig& cfg, FrameHandler onFrame, EventHandler onEvent);
  void loop();
  void setEnabled(bool en);
  bool isConnected() const;
  bool sendFrame(const uint8_t* data, size_t len);
  bool sendLine(const String& line);  // convenience: adds \n
  BleStats stats() const { return stats_; }

  // Called by callbacks (internal use)
  void handleConnect_(int connId);
  void handleDisconnect_();
  void handleRxData_(const uint8_t* data, size_t len);

private:
  BleConfig cfg_;
  FrameHandler onFrame_;
  EventHandler onEvent_;
  BleStats stats_;
  bool enabled_ = false;
  bool connected_ = false;
  int connId_ = 0;

  BLEServer* server_ = nullptr;
  BLECharacteristic* txChar_ = nullptr;
  BLECharacteristic* rxChar_ = nullptr;

  // Framing buffer (used when require_newline=true)
  String lineBuf_;

  // Flood tracking
  uint32_t windowStartMs_ = 0;
  uint32_t framesThisWindow_ = 0;

  bool floodAllowed_();
  void processRxByte_(uint8_t b);
};
