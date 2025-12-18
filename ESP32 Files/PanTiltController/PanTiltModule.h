#pragma once
#include <Arduino.h>

enum class PanTiltDest : uint8_t { USB = 0, BLE = 1 };
using PanTiltOutputFn = void (*)(PanTiltDest dest, const String& line);

void PanTilt_setOutput(PanTiltOutputFn fn);

// Pins are the ESP32 GPIOs for your servos (same defaults as your PanTilt_JSON sketch).
void PanTilt_begin(int servoXPin = 3, int servoYPin = 4);

// Call from loop() frequently.
void PanTilt_loop();

// Provide one newline-terminated JSON object WITHOUT the newline (the adapters already strip it).
// fromBle controls mirroring: always outputs to USB; also outputs to BLE when fromBle=true.
void PanTilt_handleLine(String line, bool fromBle);