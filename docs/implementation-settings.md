# Settings Implementation Plan

## Overview

This document outlines the implementation plan for restructuring the application settings to support:
1. Organized settings sections matching the SettingsDialog tabs
2. Default camera settings that apply when creating new cameras
3. Per-camera overrides stored in cameras.json

## Current State

### Settings Storage
- **Location:** `%ProgramData%\Linksoft\CameraWall\settings.json`
- **Structure:** 6 sections matching SettingsDialog tabs

### Camera Storage
- **Location:** `%ProgramData%\Linksoft\CameraWall\cameras.json`
- **Structure:** Contains cameras, layouts, and startupLayoutId

---

## Settings Structure

### settings.json

```json
{
  "general": {
    "themeBase": "Dark",
    "themeAccent": "Blue",
    "language": "1033",
    "connectCamerasOnStartup": true,
    "startMaximized": false,
    "startRibbonCollapsed": false
  },
  "cameraDisplay": {
    "showOverlayTitle": true,
    "showOverlayDescription": true,
    "showOverlayTime": false,
    "showOverlayConnectionStatus": true,
    "overlayOpacity": 0.7,
    "overlayPosition": "TopLeft",
    "allowDragAndDropReorder": true,
    "autoSaveLayoutChanges": true,
    "snapshotDirectory": null
  },
  "connection": {
    "defaultProtocol": "Rtsp",
    "defaultPort": 554,
    "connectionTimeoutSeconds": 10,
    "reconnectDelaySeconds": 5,
    "maxReconnectAttempts": 3,
    "autoReconnectOnFailure": true,
    "showNotificationOnDisconnect": true,
    "showNotificationOnReconnect": false,
    "playNotificationSound": false
  },
  "performance": {
    "videoQuality": "Auto",
    "hardwareAcceleration": true,
    "lowLatencyMode": false,
    "bufferDurationMs": 500,
    "rtspTransport": "tcp",
    "maxLatencyMs": 500
  },
  "recording": {
    "recordingPath": null,
    "recordingFormat": "mp4",
    "enableRecordingOnMotion": false
  },
  "advanced": {
    "enableDebugLogging": false,
    "logFilePath": null
  }
}
```

---

## Implementation Status

### Legend
- ✅ Implemented
- ❌ Not implemented
- ⚠️ Partially implemented

---

## Models

### Settings Models

| Model | Status | Location | Notes |
|-------|--------|----------|-------|
| `GeneralSettings` | ✅ | `Models/GeneralSettings.cs` | Complete |
| `CameraDisplayAppSettings` | ✅ | `Models/CameraDisplayAppSettings.cs` | Renamed from DisplaySettings, added overlayPosition |
| `ConnectionAppSettings` | ✅ | `Models/ConnectionAppSettings.cs` | App-level connection defaults |
| `PerformanceSettings` | ✅ | `Models/PerformanceSettings.cs` | Complete |
| `RecordingSettings` | ✅ | `Models/RecordingSettings.cs` | Complete |
| `AdvancedSettings` | ✅ | `Models/AdvancedSettings.cs` | Complete |
| `ApplicationSettings` | ✅ | `Models/ApplicationSettings.cs` | All 6 sections included |

### Camera Models

| Model | Status | Location | Notes |
|-------|--------|----------|-------|
| `CameraConfiguration` | ✅ | `Models/CameraConfiguration.cs` | Complete |
| `ConnectionSettings` | ✅ | `Models/ConnectionSettings.cs` | Complete |
| `AuthenticationSettings` | ✅ | `Models/AuthenticationSettings.cs` | Complete |
| `CameraDisplaySettings` | ✅ | `Models/CameraDisplaySettings.cs` | Complete |
| `StreamSettings` | ✅ | `Models/StreamSettings.cs` | Complete |
| `CameraOverrides` | ✅ | `Models/CameraOverrides.cs` | Per-camera overrides with nullable properties, Clone(), CopyFrom(), ValueEquals() |

---

## Services

### IApplicationSettingsService

| Method/Property | Status | Notes |
|-----------------|--------|-------|
| `General` | ✅ | Complete |
| `CameraDisplay` | ✅ | Renamed from Display |
| `Connection` | ✅ | Complete |
| `Performance` | ✅ | Complete |
| `Recording` | ✅ | Complete |
| `Advanced` | ✅ | Complete |
| `SaveGeneral()` | ✅ | Complete |
| `SaveCameraDisplay()` | ✅ | Renamed from SaveDisplay |
| `SaveConnection()` | ✅ | Complete |
| `SavePerformance()` | ✅ | Complete |
| `SaveRecording()` | ✅ | Complete |
| `SaveAdvanced()` | ✅ | Complete |
| `ApplyDefaultsToCamera()` | ✅ | Applies defaults to new cameras |
| `Load()` | ✅ | Complete |
| `Save()` | ✅ | Complete |

### ICameraStorageService

| Method/Property | Status | Notes |
|-----------------|--------|-------|
| `GetAllCameras()` | ✅ | Complete |
| `GetCameraById()` | ✅ | Complete |
| `AddOrUpdateCamera()` | ✅ | Complete |
| `DeleteCamera()` | ✅ | Complete |
| All layout methods | ✅ | Complete |

---

## SettingsDialog Tabs

> **Note:** The "Status" column in this section indicates UI binding status (whether the setting appears in the dialog and is saved to settings.json). See "Runtime Implementation Status" section below for whether each setting is actually used at runtime.

### General Tab
| Setting | UI Status | Bound Property |
|---------|--------|----------------|
| Theme Base | ✅ | `General.ThemeBase` |
| Theme Accent | ✅ | `General.ThemeAccent` |
| Language | ✅ | `General.Language` |
| Connect on Startup | ✅ | `General.ConnectCamerasOnStartup` |
| Start Maximized | ✅ | `General.StartMaximized` |
| Start Ribbon Collapsed | ✅ | `General.StartRibbonCollapsed` |

### Camera Display Tab
| Setting | UI Status | Bound Property |
|---------|--------|----------------|
| Show Overlay Title | ✅ | `CameraDisplay.ShowOverlayTitle` |
| Show Overlay Description | ✅ | `CameraDisplay.ShowOverlayDescription` |
| Show Overlay Time | ✅ | `CameraDisplay.ShowOverlayTime` |
| Show Overlay Connection Status | ✅ | `CameraDisplay.ShowOverlayConnectionStatus` |
| Overlay Opacity | ✅ | `CameraDisplay.OverlayOpacity` |
| Default Overlay Position | ✅ | `CameraDisplay.OverlayPosition` |
| Allow Drag and Drop | ✅ | `CameraDisplay.AllowDragAndDropReorder` |
| Auto Save Layout | ✅ | `CameraDisplay.AutoSaveLayoutChanges` |
| Snapshot Directory | ✅ | `CameraDisplay.SnapshotDirectory` |

### Connection Tab
| Setting | UI Status | Bound Property |
|---------|--------|----------------|
| Default Protocol | ✅ | `Connection.DefaultProtocol` |
| Default Port | ✅ | `Connection.DefaultPort` |
| Connection Timeout | ✅ | `Connection.ConnectionTimeoutSeconds` |
| Reconnect Delay | ✅ | `Connection.ReconnectDelaySeconds` |
| Max Reconnect Attempts | ✅ | `Connection.MaxReconnectAttempts` |
| Auto Reconnect | ✅ | `Connection.AutoReconnectOnFailure` |
| Notify on Disconnect | ✅ | `Connection.ShowNotificationOnDisconnect` |
| Notify on Reconnect | ✅ | `Connection.ShowNotificationOnReconnect` |
| Play Notification Sound | ✅ | `Connection.PlayNotificationSound` |

### Performance Tab
| Setting | UI Status | Bound Property |
|---------|--------|----------------|
| Video Quality | ✅ | `Performance.VideoQuality` |
| Hardware Acceleration | ✅ | `Performance.HardwareAcceleration` |
| Low Latency Mode | ✅ | `Performance.LowLatencyMode` |
| Buffer Duration | ✅ | `Performance.BufferDurationMs` |
| RTSP Transport | ✅ | `Performance.RtspTransport` |
| Max Latency | ✅ | `Performance.MaxLatencyMs` |

### Recording Tab
| Setting | UI Status | Bound Property |
|---------|--------|----------------|
| Recording Path | ✅ | `Recording.RecordingPath` |
| Recording Format | ✅ | `Recording.RecordingFormat` |
| Record on Motion | ✅ | `Recording.EnableRecordingOnMotion` |

### Advanced Tab
| Setting | UI Status | Bound Property |
|---------|--------|----------------|
| Debug Logging | ✅ | `Advanced.EnableDebugLogging` |
| Log File Path | ✅ | `Advanced.LogFilePath` |
| Restore Defaults | ✅ | Command only |

---

## Runtime Implementation Status

This section documents whether each setting is actually used at runtime (not just stored/displayed in the settings dialog).

### Legend
- ✅ **Implemented** - Setting is used at runtime
- ❌ **Not Implemented** - Setting is stored but not used (UI only)
- ⚠️ **Partial** - Setting is partially implemented

### General Settings

| Setting | Runtime Status | Usage Location | Notes |
|---------|---------------|----------------|-------|
| ThemeBase | ✅ | `App.xaml.cs` | Applied via `ThemeManagerHelper.SetThemeAndAccent()` on startup |
| ThemeAccent | ✅ | `App.xaml.cs` | Applied alongside ThemeBase on startup |
| Language | ✅ | `App.xaml.cs` | Parsed as LCID, set via `CultureManager.SetCultures()` |
| ConnectCamerasOnStartup | ✅ | `CameraWallManager.cs`, `CameraGrid.xaml.cs`, `CameraTile.xaml.cs` | Controls `AutoConnectOnLoad` property; cameras stay disconnected if false |
| StartMaximized | ✅ | `App.xaml.cs` | Sets window state to Maximized on startup |
| StartRibbonCollapsed | ✅ | `MainWindowViewModel.cs` | Sets `IsRibbonMinimized` initial state |

### Camera Display Settings

| Setting | Runtime Status | Usage Location | Notes |
|---------|---------------|----------------|-------|
| ShowOverlayTitle | ✅ | `CameraWallManager.cs`, `CameraGrid.xaml` | Applied to CameraGrid, supports per-camera overrides |
| ShowOverlayDescription | ✅ | `CameraWallManager.cs`, `CameraGrid.xaml` | Applied to CameraGrid, supports per-camera overrides |
| ShowOverlayTime | ✅ | `CameraWallManager.cs`, `CameraGrid.xaml` | Applied to CameraGrid, supports per-camera overrides |
| ShowOverlayConnectionStatus | ✅ | `CameraWallManager.cs`, `CameraGrid.xaml` | Applied to CameraGrid, supports per-camera overrides |
| OverlayOpacity | ✅ | `CameraWallManager.cs`, `CameraGrid.xaml` | Applied to CameraGrid, supports per-camera overrides |
| OverlayPosition | ✅ | `ApplicationSettingsService.cs`, `CameraTile.xaml.cs`, `FullScreenCameraWindow.xaml.cs` | Applied as default to new cameras; used at runtime in grid view and full-screen mode |
| AllowDragAndDropReorder | ✅ | `CameraWallManager.cs` | Applied to CameraGrid |
| AutoSaveLayoutChanges | ✅ | `CameraWallManager.cs` | Applied to CameraGrid as `AutoSave` property |
| SnapshotDirectory | ✅ | `CameraWallManager.cs`, `CameraTile.xaml.cs` | Used as initial directory in snapshot save dialog |

### Connection Settings

| Setting | Runtime Status | Usage Location | Notes |
|---------|---------------|----------------|-------|
| DefaultProtocol | ✅ | `ApplicationSettingsService.cs` | Applied to new cameras via `ApplyDefaultsToCamera()` |
| DefaultPort | ✅ | `ApplicationSettingsService.cs` | Applied to new cameras via `ApplyDefaultsToCamera()` |
| ConnectionTimeoutSeconds | ✅ | `CameraWallManager.cs`, `CameraTile.xaml.cs` | Used in reconnect status check timer; supports per-camera overrides |
| ReconnectDelaySeconds | ✅ | `CameraWallManager.cs`, `CameraTile.xaml.cs` | Delay before auto-reconnect attempt; supports per-camera overrides |
| MaxReconnectAttempts | ✅ | `CameraWallManager.cs`, `CameraTile.xaml.cs` | Limits auto-reconnect attempts; supports per-camera overrides |
| AutoReconnectOnFailure | ✅ | `CameraWallManager.cs`, `CameraTile.xaml.cs` | Enables/disables auto-reconnect on failure; supports per-camera overrides |
| ShowNotificationOnDisconnect | ✅ | `CameraWallManager.cs`, `CameraTile.xaml.cs` | Shows notification when camera disconnects; supports per-camera overrides |
| ShowNotificationOnReconnect | ✅ | `CameraWallManager.cs`, `CameraTile.xaml.cs` | Shows notification when camera reconnects; supports per-camera overrides |
| PlayNotificationSound | ✅ | `CameraWallManager.cs`, `CameraTile.xaml.cs` | Plays system sound on notifications; supports per-camera overrides |

### Performance Settings

| Setting | Runtime Status | Usage Location | Notes |
|---------|---------------|----------------|-------|
| VideoQuality | ✅ | `CameraWallManager.cs`, `CameraTile.xaml.cs` | Maps to FlyleafLib `MaxVerticalResolutionCustom` (Auto=0, Low=480, Medium=720, High=1080, Ultra=2160); supports per-camera overrides |
| HardwareAcceleration | ✅ | `CameraWallManager.cs`, `CameraTile.xaml.cs` | Applied to FlyleafLib `Config.Video.VideoAcceleration`; supports per-camera overrides |
| LowLatencyMode | ✅ | `ApplicationSettingsService.cs`, `CameraTile.xaml.cs` | Applied to stream settings; enables buffer/transport optimizations |
| BufferDurationMs | ✅ | `ApplicationSettingsService.cs`, `CameraTile.xaml.cs` | Used in demuxer config when LowLatencyMode enabled |
| RtspTransport | ✅ | `ApplicationSettingsService.cs`, `CameraTile.xaml.cs` | Used in demuxer format options (tcp/udp) |
| MaxLatencyMs | ✅ | `ApplicationSettingsService.cs`, `CameraTile.xaml.cs` | Used in Player config's MaxLatency property |

### Recording Settings

| Setting | Runtime Status | Usage Location | Notes |
|---------|---------------|----------------|-------|
| RecordingPath | ❌ | - | **Not implemented** - no recording functionality exists |
| RecordingFormat | ❌ | - | **Not implemented** - no recording functionality exists |
| EnableRecordingOnMotion | ❌ | - | **Not implemented** - no motion detection or recording exists |

### Advanced Settings

| Setting | Runtime Status | Usage Location | Notes |
|---------|---------------|----------------|-------|
| EnableDebugLogging | ✅ | `App.xaml.cs` | Enables Serilog file logging when true |
| LogFilePath | ✅ | `App.xaml.cs` | Directory for log files; defaults to `ApplicationPaths.DefaultLogsPath` |

### Per-Camera Override Runtime Status

| Override Property | Runtime Status | Notes |
|-------------------|---------------|-------|
| ShowOverlayTitle | ✅ | Applied in grid view (`CameraGrid.xaml`) and full-screen mode (`FullScreenCameraWindow`) |
| ShowOverlayDescription | ✅ | Applied in grid view (`CameraGrid.xaml`) and full-screen mode (`FullScreenCameraWindow`) |
| ShowOverlayTime | ✅ | Applied in grid view (`CameraGrid.xaml`) and full-screen mode (`FullScreenCameraWindow`) |
| ShowOverlayConnectionStatus | ✅ | Applied in grid view (`CameraGrid.xaml`) and full-screen mode (`FullScreenCameraWindow`) |
| OverlayOpacity | ✅ | Applied in grid view (`CameraGrid.xaml`) and full-screen mode (`FullScreenCameraWindow`) |
| ConnectionTimeoutSeconds | ✅ | Applied via `OverrideOrDefaultMultiConverter` in `CameraGrid.xaml` |
| ReconnectDelaySeconds | ✅ | Applied via `OverrideOrDefaultMultiConverter` in `CameraGrid.xaml` |
| MaxReconnectAttempts | ✅ | Applied via `OverrideOrDefaultMultiConverter` in `CameraGrid.xaml` |
| AutoReconnectOnFailure | ✅ | Applied via `OverrideOrDefaultMultiConverter` in `CameraGrid.xaml` |
| ShowNotificationOnDisconnect | ✅ | Applied via `OverrideOrDefaultMultiConverter` in `CameraGrid.xaml` |
| ShowNotificationOnReconnect | ✅ | Applied via `OverrideOrDefaultMultiConverter` in `CameraGrid.xaml` |
| PlayNotificationSound | ✅ | Applied via `OverrideOrDefaultMultiConverter` in `CameraGrid.xaml` |
| VideoQuality | ✅ | Applied via `OverrideOrDefaultMultiConverter` in `CameraGrid.xaml`; controls max resolution |
| HardwareAcceleration | ✅ | Applied via `OverrideOrDefaultMultiConverter` in `CameraGrid.xaml`; enables/disables GPU decoding |
| RecordingPath | ❌ | Stored but recording not implemented |
| RecordingFormat | ❌ | Stored but recording not implemented |
| EnableRecordingOnMotion | ❌ | Stored but recording/motion detection not implemented |

### Summary Statistics

| Category | Implemented | Not Implemented | Total | Percentage |
|----------|-------------|-----------------|-------|------------|
| General | 6 | 0 | 6 | **100%** |
| Camera Display | 9 | 0 | 9 | **100%** |
| Connection | 9 | 0 | 9 | **100%** |
| Performance | 6 | 0 | 6 | **100%** |
| Recording | 0 | 3 | 3 | 0% |
| Advanced | 2 | 0 | 2 | **100%** |
| **Total** | **32** | **3** | **35** | **91%** |

---

## CameraConfigurationDialog Integration

### Current Behavior
- ✅ New cameras receive defaults from application settings via `ApplyDefaultsToCamera()`
- ✅ Default protocol, port, overlay position, and stream settings are applied
- ✅ All values are stored per-camera

### Defaults Applied to New Cameras
| Setting | Source | Status |
|---------|--------|--------|
| Protocol | `Connection.DefaultProtocol` | ✅ |
| Port | `Connection.DefaultPort` | ✅ |
| Overlay Position | `CameraDisplay.OverlayPosition` | ✅ |
| Low Latency Mode | `Performance.LowLatencyMode` | ✅ |
| Max Latency | `Performance.MaxLatencyMs` | ✅ |
| RTSP Transport | `Performance.RtspTransport` | ✅ |
| Buffer Duration | `Performance.BufferDurationMs` | ✅ |

---

## Completed Tasks

### Phase 1: Settings Model Restructure ✅
- ✅ Create `ConnectionAppSettings` model (app-level connection defaults)
- ✅ Create `PerformanceSettings` model
- ✅ Create `RecordingSettings` model
- ✅ Create `AdvancedSettings` model
- ✅ Rename `DisplaySettings` to `CameraDisplayAppSettings` (avoid conflict)
- ✅ Add `OverlayPosition` to camera display settings
- ✅ Update `ApplicationSettings` to include all sections

### Phase 2: Service Updates ✅
- ✅ Update `IApplicationSettingsService` interface with new sections
- ✅ Update `ApplicationSettingsService` implementation
- ✅ Add `ApplyDefaultsToCamera()` method
- ✅ Add individual Save methods for each section

### Phase 3: SettingsDialog Updates ✅
- ✅ Update SettingsDialogViewModel to use new settings structure
- ✅ Add missing settings to Connection tab (DefaultProtocol, DefaultPort)
- ✅ Add missing settings to Performance tab (RtspTransport, MaxLatencyMs)
- ✅ Add Default Overlay Position to Camera Display tab
- ✅ Add Grid Layout settings to Camera Display tab (AllowDragAndDropReorder, AutoSaveLayoutChanges)
- ✅ Update Save logic for new sections

### Phase 4: Camera Dialog Integration ✅
- ✅ Update DialogService to apply defaults to new cameras
- ✅ Defaults are applied before CameraConfigurationDialog opens

### Phase 5: Runtime Integration ✅
- ✅ CameraWallManager uses CameraDisplay settings
- ✅ Display settings applied to CameraGrid on initialization

---

## Future Enhancements

### Per-Camera Overrides ✅
- ✅ Create `CameraOverrides` model for per-camera setting overrides
- ✅ Add `Overrides` property to `CameraConfiguration`
- ✅ Add "Use Default" checkbox UI for overridable settings in CameraConfigurationDialog
- ✅ Update camera JSON serialization to handle overrides
- ✅ Create `GetEffectiveValue()` / `GetEffectiveStringValue()` methods for merging defaults with overrides

### Additional Features ❌
- ❌ Settings import/export functionality
- ❌ Settings backup before major changes
- ❌ Settings profiles for different use cases

### Not Implemented Settings (Requires Development)

#### Recording System
- **Effort:** High
- **Settings:** RecordingPath, RecordingFormat, EnableRecordingOnMotion
- **Location:** New `IRecordingService` + FlyleafLib recording APIs
- **Implementation:**
  - Manual recording: Add record button to context menu, use FlyleafLib recording
  - Motion detection: Requires frame analysis (significantly complex)

#### Debug Logging ✅ IMPLEMENTED
- **Status:** Implemented using Serilog
- **Settings:** EnableDebugLogging, LogFilePath
- **Location:** `App.xaml.cs`
- **Implementation:**
  - Serilog configured with file sink at startup
  - Settings loaded early (before Host builder) via `LoadAdvancedSettingsForLogging()`
  - Daily rolling log files with 7-day retention
  - Default path: `ApplicationPaths.DefaultLogsPath` (`%ProgramData%\Linksoft\CameraWall\logs`)

---

## File Changes Summary

### New Files Created
- ✅ `Models/ConnectionAppSettings.cs`
- ✅ `Models/PerformanceSettings.cs`
- ✅ `Models/RecordingSettings.cs`
- ✅ `Models/AdvancedSettings.cs`
- ✅ `Models/CameraOverrides.cs` - Per-camera setting overrides
- ✅ `Factories/DropDownItemsFactory.cs` - Centralized dropdown items and defaults
- ✅ `Helpers/ApplicationPaths.cs` - Default paths for logs, snapshots, recordings, settings

### Build Infrastructure Files
- ✅ `version.json` - NBGV configuration
- ✅ `.github/workflows/pr-validation.yml` - PR validation workflow
- ✅ `.github/workflows/ci.yml` - Main branch CI workflow
- ✅ `.github/workflows/release.yml` - Release workflow
- ✅ `setup/Directory.Build.props` - Disable analyzers for WiX
- ✅ `setup/Linksoft.CameraWall.Installer/Linksoft.CameraWall.Installer.wixproj` - WiX project
- ✅ `setup/Linksoft.CameraWall.Installer/Package.wxs` - Main installer definition
- ✅ `setup/Linksoft.CameraWall.Installer/Directories.wxs` - Installation paths
- ✅ `setup/Linksoft.CameraWall.Installer/Components.wxs` - Application files
- ✅ `setup/Linksoft.CameraWall.Installer/Shortcuts.wxs` - Start menu/desktop shortcuts
- ✅ `setup/Linksoft.CameraWall.Installer/License.rtf` - License for installer UI

### Modified Files
- ✅ `Models/ApplicationSettings.cs` - Added all 6 sections
- ✅ `Models/CameraDisplayAppSettings.cs` - Renamed from DisplaySettings, added OverlayPosition
- ✅ `Services/IApplicationSettingsService.cs` - Added new properties/methods
- ✅ `Services/ApplicationSettingsService.cs` - Implemented new structure
- ✅ `Services/CameraWallManager.cs` - Updated to use CameraDisplay
- ✅ `Services/DialogService.cs` - Apply defaults to new cameras
- ✅ `Dialogs/SettingsDialogViewModel.cs` - Use new settings structure
- ✅ `Dialogs/SettingsDialog.xaml` - Added all settings UI including Grid Layout group
- ✅ `Resources/Translations.resx` - Added new translation keys
- ✅ `Resources/Translations.da-DK.resx` - Added Danish translations
- ✅ `Resources/Translations.de-DE.resx` - Added German translations
- ✅ `Directory.Build.props` - Added NBGV package reference
- ✅ `Linksoft.CameraWall.slnx` - Added comment about WiX installer
- ✅ `src/Linksoft.Wpf.CameraWall/Linksoft.Wpf.CameraWall.csproj` - Added NuGet package metadata
- ✅ `src/Linksoft.Wpf.CameraWall.App/Linksoft.Wpf.CameraWall.App.csproj` - Added icon content for installer
- ✅ `Dialogs/FullScreenCameraWindow.xaml` - Updated overlay bindings to respect per-camera settings
- ✅ `Dialogs/FullScreenCameraWindow.xaml.cs` - Added time display timer and overlay background opacity
- ✅ `Dialogs/FullScreenCameraWindowViewModel.cs` - Added overlay setting properties
- ✅ `Themes/FullScreenStyles.xaml` - Added BooleanToVisibilityConverter

---

## Per-Camera Overrides

### Overview
Per-camera overrides allow individual cameras to deviate from application-level defaults. When an override is set (non-null), that value is used instead of the application default. When null, the application default is used.

### Available Overrides

| Category | Override Property | Type | Description |
|----------|-------------------|------|-------------|
| **Display** | ShowOverlayTitle | bool? | Show camera title in overlay |
| | ShowOverlayDescription | bool? | Show description in overlay |
| | ShowOverlayTime | bool? | Show timestamp in overlay |
| | ShowOverlayConnectionStatus | bool? | Show connection status in overlay |
| | OverlayOpacity | double? | Overlay background opacity (0.0-1.0) |
| **Connection** | ConnectionTimeoutSeconds | int? | Connection timeout |
| | ReconnectDelaySeconds | int? | Delay between reconnection attempts |
| | MaxReconnectAttempts | int? | Maximum reconnection attempts |
| | AutoReconnectOnFailure | bool? | Auto-reconnect when connection fails |
| | ShowNotificationOnDisconnect | bool? | Show notification on disconnect |
| | ShowNotificationOnReconnect | bool? | Show notification on reconnect |
| | PlayNotificationSound | bool? | Play sound for notifications |
| **Performance** | VideoQuality | string? | Video quality setting |
| | HardwareAcceleration | bool? | Enable hardware acceleration |
| **Recording** | RecordingPath | string? | Custom recording path |
| | RecordingFormat | string? | Recording format (mp4, mkv, avi) |
| | EnableRecordingOnMotion | bool? | Record on motion detection |

### Usage
```csharp
// Get effective value (override or app default)
var showTitle = settingsService.GetEffectiveValue(
    camera,
    settingsService.CameraDisplay.ShowOverlayTitle,
    o => o?.ShowOverlayTitle);

var recordingPath = settingsService.GetEffectiveStringValue(
    camera,
    settingsService.Recording.RecordingPath,
    o => o?.RecordingPath);
```

### UI
The CameraConfigurationDialog includes an "Overrides" section with "Use Default" checkboxes for each overridable setting. When "Use Default" is checked, the setting uses the application default. When unchecked, a custom value can be specified.

### Full-Screen Mode
Per-camera overlay settings are applied in full-screen mode (`FullScreenCameraWindow`). The `DialogService.ShowFullScreenCamera()` method computes effective overlay settings by merging camera overrides with app defaults:
- `ShowOverlayTitle` - Controls visibility of camera name
- `ShowOverlayDescription` - Controls visibility of camera description
- `ShowOverlayTime` - Shows current time in overlay
- `ShowOverlayConnectionStatus` - Shows connection status indicator
- `OverlayOpacity` - Controls overlay background transparency
- `OverlayPosition` - Uses camera's configured position (TopLeft, TopRight, BottomLeft, BottomRight)

---

## Build & Release Infrastructure

### GitHub Workflows ✅

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `pr-validation.yml` | Pull requests to main | Build, test, installer validation |
| `ci.yml` | Push to main | Build, test, upload test results |
| `release.yml` | Tag push (v*) | Build, test, NuGet publish, MSI build, GitHub Release |

### Version Management ✅
- **Tool:** Nerdbank.GitVersioning (NBGV)
- **Config:** `version.json` in repository root
- **Base Version:** 1.0
- **Release Trigger:** Tag push matching `v*` (e.g., `v1.0.0`)

### MSI Installer ✅
- **Location:** `setup/Linksoft.CameraWall.Installer/`
- **Technology:** WiX Toolset 5.0.2 (SDK-style)
- **Build:** Command line only (not in VS solution due to SDK compatibility)
- **Build Command:** `dotnet build setup/Linksoft.CameraWall.Installer/Linksoft.CameraWall.Installer.wixproj`

### Required GitHub Secrets

| Secret | Description |
|--------|-------------|
| `NUGET_API_KEY` | API key from nuget.org (glob pattern: `Linksoft.*`) |

---

## DropDownItemsFactory

The `DropDownItemsFactory` class centralizes all dropdown items and default values used across dialogs:

### Dropdown Items
| Property | Description |
|----------|-------------|
| `VideoQualityItems` | Auto, 1080p, 720p, 480p, 360p |
| `RtspTransportItems` | TCP, UDP |
| `RecordingFormatItems` | MP4, MKV, AVI |
| `OverlayPositionItems` | TopLeft, TopRight, BottomLeft, BottomRight |
| `OverlayOpacityItems` | 0% to 100% in 10% increments |
| `ProtocolItems` | RTSP, HTTP |

### Default Values
| Constant | Value |
|----------|-------|
| `DefaultVideoQuality` | "Auto" |
| `DefaultRtspTransport` | "tcp" |
| `DefaultRecordingFormat` | "mp4" |
| `DefaultOverlayPosition` | "TopLeft" |
| `DefaultOverlayOpacity` | "0.7" |
| `DefaultProtocol` | "Rtsp" |

### Utility Methods
- `GetMaxResolutionFromQuality(string)` - Converts quality setting to max vertical resolution (Auto=0, 1080p=1080, etc.)

---

## Override Change Behavior

When camera overrides are changed in CameraConfigurationDialog:

| Override Category | Reconnect Required | Notes |
|-------------------|-------------------|-------|
| **Display Overrides** | ❌ No | ShowOverlayTitle, ShowOverlayDescription, ShowOverlayTime, ShowOverlayConnectionStatus, OverlayOpacity - handled via bindings |
| **Performance Overrides** | ✅ Yes | VideoQuality, HardwareAcceleration - require player recreation |
| **Connection Overrides** | ✅ Yes | Handled via existing Connection/Stream change detection |
| **Recording Overrides** | ❌ No | Recording not implemented |

---

## Notes

1. **Backward Compatibility:** The application handles legacy settings.json files gracefully. Missing sections use default values.

2. **Default Values:** All settings have sensible defaults defined in the model classes.

3. **Validation:** Numeric inputs use LabelIntegerBox with min/max constraints.

4. **JSON Serialization:** Uses camelCase naming policy and JsonStringEnumConverter for enums.

5. **WiX Installer:** Excluded from Visual Studio solution due to SDK compatibility issues. Build via command line or GitHub Actions.
