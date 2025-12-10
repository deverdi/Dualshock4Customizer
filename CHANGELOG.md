# Changelog

All notable changes to DS4 Customizer Pro will be documented in this file.

## [0.9.0-beta] - 2024-12-XX

### ?? Initial Beta Release

#### Added
- ? 3D DualShock 4 visualization with texture support
- ? OBJ/MTL model loader with HelixToolkit
- ? RGB LED color control (16M+ colors)
- ? LED effects: Rainbow, Breathing, Pulse, Strobe
- ? Real-time battery monitoring
- ? Low battery warnings with vibration alerts
- ? Profile management system
- ? Profile import/export functionality
- ? Automatic game detection
- ? Multiple controller support
- ? USB and Bluetooth connection support
- ? System tray integration
- ? Profile categories (Game, Movie, Music, Night, Custom)
- ? Favorite profiles
- ? MAC address-based auto-loading
- ? Modern Material Design UI
- ? Mouse-rotatable 3D viewport
- ? Live LED preview on 3D model

#### Technical Details
- Built with .NET 8.0 WPF
- HelixToolkit.Wpf for 3D rendering
- AssimpNet for model loading
- HidLibrary for DS4 communication
- Self-contained executable (no .NET installation required)

#### Known Issues
- ?? Controller might not be detected on first launch (restart app)
- ?? Fast effect transitions may cause LED lag
- ?? Light Bar mesh not yet identified in 3D model
- ?? AssimpNet occasionally fails to load OBJ (HelixToolkit fallback works)

#### System Requirements
- Windows 10/11 (64-bit)
- .NET 8.0 Desktop Runtime (included in self-contained build)
- DualShock 4 Controller (CUH-ZCT1 or CUH-ZCT2)
- ~512 MB RAM
- ~100 MB disk space

---

## [Unreleased]

### Planned Features
- ?? Custom 3D models support
- ?? Button remapping
- ?? Macro recording
- ?? TouchPad configuration
- ?? Gyroscope settings
- ?? Advanced LED patterns
- ?? Cloud profile sync
- ?? Multi-language support
