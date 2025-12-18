#include "BLEAdapterUART.h"

// -------------------- Callback Implementations --------------------

void BleServerCallbacks::onConnect(BLEServer* server) {
  if (adapter_) adapter_->handleConnect_(server->getConnId());
}

void BleServerCallbacks::onDisconnect(BLEServer* server) {
  if (adapter_) adapter_->handleDisconnect_();
}

void BleRxCallbacks::onWrite(BLECharacteristic* chr) {
  if (!adapter_) return;

  // ESP32 Arduino BLE library returns Arduino String here
  String s = chr->getValue();
  if (s.length() == 0) return;

  adapter_->handleRxData_((const uint8_t*)s.c_str(), s.length());
}

// -------------------- BLEAdapterUART Implementation --------------------

bool BLEAdapterUART::begin(const BleConfig& cfg, FrameHandler onFrame, EventHandler onEvent) {
  cfg_ = cfg;
  onFrame_ = onFrame;
  onEvent_ = onEvent;

  lineBuf_.reserve(cfg_.max_frame_len + 8);
  windowStartMs_ = millis();
  framesThisWindow_ = 0;

  // Initialize BLE
  BLEDevice::init(cfg_.device_name.c_str());

  // Create server
  server_ = BLEDevice::createServer();
  server_->setCallbacks(new BleServerCallbacks(this));

  // Create Nordic UART Service
  BLEService* service = server_->createService(NUS_SERVICE_UUID);

  // TX characteristic (ESP32 -> phone): Notify
  txChar_ = service->createCharacteristic(
    NUS_TX_CHARACTERISTIC,
    BLECharacteristic::PROPERTY_NOTIFY
  );
  txChar_->addDescriptor(new BLE2902());

  // RX characteristic (phone -> ESP32): Write
  rxChar_ = service->createCharacteristic(
    NUS_RX_CHARACTERISTIC,
    BLECharacteristic::PROPERTY_WRITE | BLECharacteristic::PROPERTY_WRITE_NR
  );
  rxChar_->setCallbacks(new BleRxCallbacks(this));

  // Start service
  service->start();

  // Start advertising
  BLEAdvertising* advertising = BLEDevice::getAdvertising();
  advertising->addServiceUUID(NUS_SERVICE_UUID);
  advertising->setScanResponse(true);
  advertising->setMinPreferred(0x06);  // for iPhone
  advertising->setMinPreferred(0x12);
  BLEDevice::startAdvertising();

  enabled_ = true;

  BleMeta meta;
  meta.timestamp_ms = millis();
  if (onEvent_) onEvent_("READY", meta);

  return true;
}

void BLEAdapterUART::setEnabled(bool en) {
  enabled_ = en;
  if (!enabled_) {
    lineBuf_ = "";
  }
}

bool BLEAdapterUART::isConnected() const {
  return enabled_ && connected_;
}

bool BLEAdapterUART::floodAllowed_() {
  uint32_t now = millis();
  if (now - windowStartMs_ >= 1000) {
    windowStartMs_ = now;
    framesThisWindow_ = 0;
  }
  if (framesThisWindow_ >= cfg_.flood_max_fps) {
    stats_.flood_drops++;
    return false;
  }
  framesThisWindow_++;
  return true;
}

void BLEAdapterUART::processRxByte_(uint8_t b) {
  // NOTE: rx_bytes is counted in handleRxData_ as a bulk add
  char ch = (char)b;

  if (ch == '\n') {
    if (!floodAllowed_()) {
      lineBuf_ = "";
      return;
    }

    // Strip optional '\r'
    if (lineBuf_.endsWith("\r")) {
      lineBuf_.remove(lineBuf_.length() - 1);
    }

    // Emit frame
    stats_.rx_frames++;
    if (onFrame_) {
      BleMeta meta;
      meta.timestamp_ms = millis();
      meta.conn_id = connId_;
      onFrame_((const uint8_t*)lineBuf_.c_str(), lineBuf_.length(), meta);
    }
    lineBuf_ = "";
    return;
  }

  // Accumulate
  if (lineBuf_.length() >= cfg_.max_frame_len) {
    stats_.overlong_frames++;
    lineBuf_ = "";
    return;
  }

  lineBuf_ += ch;
}

void BLEAdapterUART::handleConnect_(int connId) {
  connected_ = true;
  connId_ = connId;
  stats_.connects++;
  lineBuf_ = "";

  BleMeta meta;
  meta.timestamp_ms = millis();
  meta.conn_id = connId;
  if (onEvent_) onEvent_("CONNECTED", meta);
}

void BLEAdapterUART::handleDisconnect_() {
  connected_ = false;
  stats_.disconnects++;
  lineBuf_ = "";

  BleMeta meta;
  meta.timestamp_ms = millis();
  meta.conn_id = connId_;
  if (onEvent_) onEvent_("DISCONNECTED", meta);

  connId_ = 0;

  // Restart advertising so another device can connect
  BLEDevice::startAdvertising();
}

void BLEAdapterUART::handleRxData_(const uint8_t* data, size_t len) {
  if (!enabled_) return;

  // Always track raw received bytes here (single source of truth)
  stats_.rx_bytes += len;

  // If newline not required, each write = one frame
  if (!cfg_.require_newline) {
    if (!floodAllowed_()) return;
    stats_.rx_frames++;

    BleMeta meta;
    meta.timestamp_ms = millis();
    meta.conn_id = connId_;

    if (onFrame_) onFrame_(data, len, meta);
    return;
  }

  // Newline framed mode
  for (size_t i = 0; i < len; i++) {
    processRxByte_(data[i]);
  }
}

bool BLEAdapterUART::sendFrame(const uint8_t* data, size_t len) {
  if (!isConnected() || !txChar_) return false;

  // Conservative chunk size (keeps iOS/Android happy without depending on MTU negotiation)
  const size_t chunkSize = 240;
  size_t sent = 0;

  while (sent < len) {
    size_t toSend = (len - sent < chunkSize) ? (len - sent) : chunkSize;

    txChar_->setValue((uint8_t*)(data + sent), toSend);
    txChar_->notify();

    sent += toSend;
    stats_.tx_bytes += toSend;

    // Delay only BETWEEN chunks
    if (sent < len) delay(10);
  }

  return true;
}

bool BLEAdapterUART::sendLine(const String& line) {
  String msg = line + "\n";
  return sendFrame((const uint8_t*)msg.c_str(), msg.length());
}

void BLEAdapterUART::loop() {
  // BLE events handled by callbacks
}
