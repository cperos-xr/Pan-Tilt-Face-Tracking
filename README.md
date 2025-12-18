# Pan-Tilt Face Tracker (Unity + ESP32)

This project demonstrates face tracking with a pan-tilt rig using Unity and an ESP32 microcontroller.

- Unity C# scripts for face tracking and pan-tilt control
- Tuning UI for live adjustment of thresholds and step sizes
- ESP32 firmware expects simple JSON commands (see PanTiltController.cs)
- APK available in Releases for quick testing

**Note:** This public version does NOT include the paid OpenCVForUnity asset. You must import it yourself for full functionality.

## Usage

1. Import OpenCVForUnity into your Unity project.
2. Add the provided scripts to your scene.
3. Wire up the PanTiltController and FaceCenterListener as described in the comments.
4. Use the APK for quick testing on Android devices.

## License

MIT for scripts. ESP32 firmware and OpenCVForUnity subject to their own licenses.