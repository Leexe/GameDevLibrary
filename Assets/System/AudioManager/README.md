# Audio Manager v1.2

## v1.2
- Added event that triggers on timeline marker hit
- FIxed delay on audio pause by calling the API directly
- Simplified FrequencyPeak bool

## v1.1
- Added audio visualization system with FFT spectrum data, RMS, and peak level metering.
- Replaced channel-specific volume methods with a generic bus type approach using AudioBusType enum.
- Added VCA (Voltage Controlled Amplifier) support for volume control.
- Removed redundant per-channel getter/setter methods in favor of GetBusByType.
