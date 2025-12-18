![App Screenshot](./newColors.png)

# Pan-Tilt Face Tracking App

This app lets you control a pan-tilt camera rig using face tracking, directly from your Android device. No Unity experience required—just install the APK and start using the features!

## Features

- **Live Face Tracking:** Automatically detects and tracks faces in real time.
- **FPS & Resolution Display:** Shows current frame rate and camera resolution.
- **Camera Controls:**
	- Change camera (front/rear)
	- Turn camera feed on/off
	- Fullscreen/regular view toggle
- **Face Tracking Controls:**
	- Enable/disable face tracking
	- X/Y threshold adjustment
	- Step size and min/max step degrees
	- Scale step by error and allow diagonal movement
- **Manual Pan-Tilt Controls:**
	- Arrow buttons for direct movement
	- Sweep X/Y for automated scanning
	- Center and Stop All buttons
- **Status Display:**
	- Face tracking status (ON/OFF)

## How to Use

1. **Install the APK** on your Android device.
2. **Launch the app.**
3. The camera feed will appear at the top, with face tracking enabled by default.
4. Use the on-screen controls to:
	 - Adjust tracking sensitivity and movement
	 - Manually control the pan-tilt rig
	 - Toggle camera and display modes
	 - Change camera source (front/rear)
5. For fullscreen camera view, tap the expand button in the top right corner.
6. To restore regular size, tap the restore button.
7. All controls are touch-friendly and designed for quick access.

## Reference UI

The screenshot below shows all available controls and features:

![App Screenshot](./newColors.png)

## FAQ

- **Do I need Unity or OpenCV knowledge?**
	- No! Just use the APK. All features are available out of the box.
- **Can I use this with my own pan-tilt hardware?**
	- Yes, if your hardware supports the ESP32 JSON command protocol.
- **Where do I get the APK?**
	- Check the Releases section for the latest APK.

## Support

For troubleshooting or hardware setup, see the included documentation or contact the project maintainer.

---
MIT License. ESP32 firmware and third-party assets subject to their own licenses.