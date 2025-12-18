#pragma once
#include <Arduino.h>
#include <functional>

struct UsbMeta {
  const char* source = "usb";
  uint32_t timestamp_ms = 0;
};

struct UsbStats {
  uint32_t rx_bytes = 0;
  uint32_t rx_frames = 0;
  uint32_t dropped_frames = 0;
  uint32_t overlong_frames = 0;
  uint32_t flood_drops = 0;
};

struct UsbConfig {
  size_t max_frame_len = 256;
  uint32_t flood_max_fps = 60;
  bool require_newline = true;
  bool echo = false;     // useful for consoles
};

class USBAdapter {
public:
  using FrameHandler = std::function<void(const uint8_t* data, size_t len, const UsbMeta& meta)>;
  using EventHandler = std::function<void(const char* event, const UsbMeta& meta)>;

  // Pass in the stream you want to read/write (Serial, Serial0, USBSerial, etc.)
  bool begin(Stream& io, const UsbConfig& cfg, FrameHandler onFrame, EventHandler onEvent);
  void loop();
  void setEnabled(bool en);
  bool sendFrame(const uint8_t* data, size_t len);
  UsbStats stats() const { return stats_; }

private:
  Stream* io_ = nullptr;
  UsbConfig cfg_;
  FrameHandler onFrame_;
  EventHandler onEvent_;
  UsbStats stats_;
  bool enabled_ = false;

  String lineBuf_;

  uint32_t windowStartMs_ = 0;
  uint32_t framesThisWindow_ = 0;

  bool floodAllowed_();
};
