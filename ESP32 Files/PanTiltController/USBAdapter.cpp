#include "USBAdapter.h"

bool USBAdapter::begin(Stream& io, const UsbConfig& cfg, FrameHandler onFrame, EventHandler onEvent) {
  io_ = &io;
  cfg_ = cfg;
  onFrame_ = onFrame;
  onEvent_ = onEvent;

  enabled_ = true;
  lineBuf_.reserve(cfg_.max_frame_len + 8);
  windowStartMs_ = millis();
  framesThisWindow_ = 0;

  UsbMeta meta; meta.timestamp_ms = millis();
  if (onEvent_) onEvent_("READY", meta);
  return true;
}

void USBAdapter::setEnabled(bool en) {
  enabled_ = en;
  if (!enabled_) lineBuf_ = "";
}

bool USBAdapter::floodAllowed_() {
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

bool USBAdapter::sendFrame(const uint8_t* data, size_t len) {
  if (!enabled_ || !io_) return false;
  // For USB serial this is typically fine; keep payload reasonable.
  size_t w = io_->write(data, len);
  return w == len;
}

void USBAdapter::loop() {
  if (!enabled_ || !io_) return;

  UsbMeta meta;
  meta.timestamp_ms = millis();

  while (io_->available()) {
    int b = io_->read();
    if (b < 0) break;

    stats_.rx_bytes++;

    char ch = (char)b;

    if (cfg_.echo) {
      io_->write((uint8_t*)&ch, 1);
    }

    if (ch == '\n') {
      if (!floodAllowed_()) {
        lineBuf_ = "";
        continue;
      }

      if (lineBuf_.endsWith("\r")) lineBuf_.remove(lineBuf_.length() - 1);

      stats_.rx_frames++;
      if (onFrame_) {
        onFrame_((const uint8_t*)lineBuf_.c_str(), lineBuf_.length(), meta);
      }
      lineBuf_ = "";
      continue;
    }

    if (lineBuf_.length() >= cfg_.max_frame_len) {
      stats_.overlong_frames++;
      lineBuf_ = "";
      continue;
    }

    lineBuf_ += ch;
  }
}