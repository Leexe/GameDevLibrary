# Audio Manager v1.1

## v1.1
- Added audio visualization system with FFT spectrum data, RMS, and peak level metering.
- Replaced channel-specific volume methods with a generic bus type approach using AudioBusType enum.
- Added VCA (Voltage Controlled Amplifier) support for volume control.
- Removed redundant per-channel getter/setter methods in favor of GetBusByType.
