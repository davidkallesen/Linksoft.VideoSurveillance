# Settings Implementation Plan

## Overview

This document outlines the implementation plan for restructuring the application settings to support:
1. Organized settings sections matching the SettingsDialog tabs
2. Default camera settings that apply when creating new cameras
3. Per-camera overrides stored in cameras.json

## Current State

### Settings Storage
- **Location:** `%ProgramData%\Linksoft\CameraWall\settings.json`
- **Structure:** 7 sections matching SettingsDialog tabs

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
  "motionDetection": {
    "sensitivity": 30,
    "minimumChangePercent": 0.5,
    "analysisFrameRate": 30,
    "analysisWidth": 800,
    "analysisHeight": 600,
    "postMotionDurationSeconds": 10,
    "cooldownSeconds": 5,
    "boundingBox": {
      "showInGrid": false,
      "showInFullScreen": false,
      "color": "Red",
      "thickness": 2,
      "minArea": 10,
      "padding": 4,
      "smoothing": 0.3
    }
  },
  "recording": {
    "recordingPath": "%ProgramData%\\Linksoft\\CameraWall\\recordings",
    "recordingFormat": "mp4",
    "enableRecordingOnMotion": false,
    "enableRecordingOnConnect": false,
    "enableHourlySegmentation": true,
    "maxRecordingDurationMinutes": 60,
    "thumbnailTileCount": 4,
    "cleanup": {
      "schedule": "Disabled",
      "recordingRetentionDays": 30,
      "includeSnapshots": false,
      "snapshotRetentionDays": 7
    },
    "playbackOverlay": {
      "showFilename": true,
      "filenameColor": "White",
      "showTimestamp": true,
      "timestampColor": "White"
    }
  },
  "advanced": {
    "enableDebugLogging": false,
    "logPath": "%ProgramData%\\Linksoft\\CameraWall\\logs"
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
| `GeneralSettings` | ✅ | `Models/Settings/GeneralSettings.cs` | Complete |
| `CameraDisplayAppSettings` | ✅ | `Models/Settings/CameraDisplayAppSettings.cs` | Renamed from DisplaySettings, added overlayPosition |
| `ConnectionAppSettings` | ✅ | `Models/Settings/ConnectionAppSettings.cs` | App-level connection defaults |
| `PerformanceSettings` | ✅ | `Models/Settings/PerformanceSettings.cs` | Complete |
| `MotionDetectionSettings` | ✅ | `Models/Settings/MotionDetectionSettings.cs` | Analysis parameters, post-motion duration, cooldown |
| `BoundingBoxSettings` | ✅ | `Models/Settings/BoundingBoxSettings.cs` | Nested in MotionDetectionSettings for visual display options |
| `RecordingSettings` | ✅ | `Models/Settings/RecordingSettings.cs` | Extended with segmentation, cleanup, and playback overlay |
| `MediaCleanupSettings` | ✅ | `Models/Settings/MediaCleanupSettings.cs` | Nested in RecordingSettings for automatic cleanup |
| `PlaybackOverlaySettings` | ✅ | `Models/Settings/PlaybackOverlaySettings.cs` | Nested in RecordingSettings for playback display |
| `AdvancedSettings` | ✅ | `Models/Settings/AdvancedSettings.cs` | Complete |
| `ApplicationSettings` | ✅ | `Models/Settings/ApplicationSettings.cs` | All 7 sections included |

### Enums

| Enum | Status | Location | Notes |
|------|--------|----------|-------|
| `MediaCleanupSchedule` | ✅ | `Enums/MediaCleanupSchedule.cs` | Disabled, OnStartup, OnStartupAndPeriodically |

### Camera Models

| Model | Status | Location | Notes |
|-------|--------|----------|-------|
| `CameraConfiguration` | ✅ | `Models/CameraConfiguration.cs` | Complete |
| `ConnectionSettings` | ✅ | `Models/Settings/ConnectionSettings.cs` | Complete |
| `AuthenticationSettings` | ✅ | `Models/Settings/AuthenticationSettings.cs` | Complete |
| `CameraDisplaySettings` | ✅ | `Models/Settings/CameraDisplaySettings.cs` | Complete |
| `StreamSettings` | ✅ | `Models/Settings/StreamSettings.cs` | Complete |
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
| `MotionDetection` | ✅ | Motion detection analysis and bounding box settings |
| `Recording` | ✅ | Extended with cleanup and playback overlay |
| `Advanced` | ✅ | Complete |
| `SaveGeneral()` | ✅ | Complete |
| `SaveCameraDisplay()` | ✅ | Renamed from SaveDisplay |
| `SaveConnection()` | ✅ | Complete |
| `SavePerformance()` | ✅ | Complete |
| `SaveMotionDetection()` | ✅ | Complete |
| `SaveRecording()` | ✅ | Complete |
| `SaveAdvanced()` | ✅ | Complete |
| `ApplyDefaultsToCamera()` | ✅ | Applies defaults to new cameras |
| `GetEffectiveValue<T>()` | ✅ | Gets camera override or app default for value types |
| `GetEffectiveStringValue()` | ✅ | Gets camera override or app default for strings |
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

### Motion Detection Tab

**Analysis Settings Group**
| Setting | UI Status | Bound Property |
|---------|--------|----------------|
| Sensitivity | ✅ | `MotionDetection.Sensitivity` |
| Minimum Change Percent | ✅ | `MotionDetection.MinimumChangePercent` |
| Analysis Frame Rate | ✅ | `MotionDetection.AnalysisFrameRate` |
| Analysis Width | ✅ | `MotionDetection.AnalysisWidth` |
| Analysis Height | ✅ | `MotionDetection.AnalysisHeight` |
| Post Motion Duration | ✅ | `MotionDetection.PostMotionDurationSeconds` |
| Cooldown | ✅ | `MotionDetection.CooldownSeconds` |

**Bounding Box Settings Group**
| Setting | UI Status | Bound Property |
|---------|--------|----------------|
| Show in Grid | ✅ | `MotionDetection.BoundingBox.ShowInGrid` |
| Show in Full Screen | ✅ | `MotionDetection.BoundingBox.ShowInFullScreen` |
| Color | ✅ | `MotionDetection.BoundingBox.Color` |
| Thickness | ✅ | `MotionDetection.BoundingBox.Thickness` |
| Min Area | ✅ | `MotionDetection.BoundingBox.MinArea` |
| Padding | ✅ | `MotionDetection.BoundingBox.Padding` |
| Smoothing | ✅ | `MotionDetection.BoundingBox.Smoothing` |

### Recording Tab

**General Settings Group**
| Setting | UI Status | Bound Property |
|---------|--------|----------------|
| Recording Path | ✅ | `Recording.RecordingPath` |
| Recording Format | ✅ | `Recording.RecordingFormat` |
| Record on Motion | ✅ | `Recording.EnableRecordingOnMotion` |
| Record on Connect | ✅ | `Recording.EnableRecordingOnConnect` |

**Segmentation Settings Group**
| Setting | UI Status | Bound Property |
|---------|--------|----------------|
| Enable Hourly Segmentation | ✅ | `Recording.EnableHourlySegmentation` |
| Max Recording Duration | ✅ | `Recording.MaxRecordingDurationMinutes` |
| Thumbnail Tile Count | ✅ | `Recording.ThumbnailTileCount` |

**Media Cleanup Settings Group**
| Setting | UI Status | Bound Property |
|---------|--------|----------------|
| Cleanup Schedule | ✅ | `Recording.Cleanup.Schedule` |
| Recording Retention Days | ✅ | `Recording.Cleanup.RecordingRetentionDays` |
| Include Snapshots | ✅ | `Recording.Cleanup.IncludeSnapshots` |
| Snapshot Retention Days | ✅ | `Recording.Cleanup.SnapshotRetentionDays` |

**Playback Overlay Settings Group**
| Setting | UI Status | Bound Property |
|---------|--------|----------------|
| Show Filename | ✅ | `Recording.PlaybackOverlay.ShowFilename` |
| Filename Color | ✅ | `Recording.PlaybackOverlay.FilenameColor` |
| Show Timestamp | ✅ | `Recording.PlaybackOverlay.ShowTimestamp` |
| Timestamp Color | ✅ | `Recording.PlaybackOverlay.TimestampColor` |

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

### Motion Detection Settings

| Setting | Runtime Status | Usage Location | Notes |
|---------|---------------|----------------|-------|
| Sensitivity | ✅ | `MotionDetectionService.cs` | Threshold for motion detection triggering |
| MinimumChangePercent | ✅ | `MotionDetectionService.cs` | Minimum pixel change to register motion |
| AnalysisFrameRate | ✅ | `MotionDetectionService.cs` | Frame analysis frequency |
| AnalysisWidth | ✅ | `MotionDetectionService.cs` | Analysis resolution width |
| AnalysisHeight | ✅ | `MotionDetectionService.cs` | Analysis resolution height |
| PostMotionDurationSeconds | ✅ | `MotionDetectionService.cs`, `RecordingService.cs` | Continue recording after motion stops |
| CooldownSeconds | ✅ | `MotionDetectionService.cs` | Cooldown before new motion trigger |
| BoundingBox.ShowInGrid | ✅ | `CameraTile.xaml.cs` | Display bounding boxes in main grid |
| BoundingBox.ShowInFullScreen | ✅ | `FullScreenCameraWindow.xaml.cs` | Display bounding boxes in full screen |
| BoundingBox.Color | ✅ | `CameraTile.xaml.cs`, `FullScreenCameraWindow.xaml.cs` | Bounding box border color |
| BoundingBox.Thickness | ✅ | `CameraTile.xaml.cs`, `FullScreenCameraWindow.xaml.cs` | Bounding box border thickness |
| BoundingBox.MinArea | ✅ | `MotionDetectionService.cs` | Minimum area to display |
| BoundingBox.Padding | ✅ | `MotionDetectionService.cs` | Padding around detected area |
| BoundingBox.Smoothing | ✅ | `MotionDetectionService.cs` | Position smoothing factor |

### Recording Settings

| Setting | Runtime Status | Usage Location | Notes |
|---------|---------------|----------------|-------|
| RecordingPath | ✅ | `RecordingService.cs` | Used as base path for recording files; supports per-camera overrides |
| RecordingFormat | ✅ | `RecordingService.cs` | Recording format (mp4, mkv, avi); supports per-camera overrides |
| EnableRecordingOnMotion | ✅ | `CameraWallManager.cs`, `CameraTile.xaml.cs` | Triggers recording on motion detection; supports per-camera overrides |
| EnableRecordingOnConnect | ✅ | `CameraWallManager.cs`, `CameraTile.xaml.cs` | Auto-starts recording when camera connects; supports per-camera overrides |
| EnableHourlySegmentation | ✅ | `RecordingService.cs` | Splits recordings at hour boundaries |
| MaxRecordingDurationMinutes | ✅ | `RecordingService.cs` | Maximum segment length before auto-split |
| ThumbnailTileCount | ✅ | `RecordingService.cs` | 1=single image, 4=2x2 grid thumbnail |
| Cleanup.Schedule | ✅ | `MediaCleanupService.cs` | When to run automatic cleanup |
| Cleanup.RecordingRetentionDays | ✅ | `MediaCleanupService.cs` | Days to keep recordings |
| Cleanup.IncludeSnapshots | ✅ | `MediaCleanupService.cs` | Include snapshots in cleanup |
| Cleanup.SnapshotRetentionDays | ✅ | `MediaCleanupService.cs` | Days to keep snapshots |
| PlaybackOverlay.ShowFilename | ✅ | `FullScreenRecordingWindow.xaml.cs` | Show filename in playback |
| PlaybackOverlay.FilenameColor | ✅ | `FullScreenRecordingWindow.xaml.cs` | Filename text color |
| PlaybackOverlay.ShowTimestamp | ✅ | `FullScreenRecordingWindow.xaml.cs` | Show timestamp in playback |
| PlaybackOverlay.TimestampColor | ✅ | `FullScreenRecordingWindow.xaml.cs` | Timestamp text color |

### Advanced Settings

| Setting | Runtime Status | Usage Location | Notes |
|---------|---------------|----------------|-------|
| EnableDebugLogging | ✅ | `App.xaml.cs` | Enables Serilog file logging when true |
| LogPath | ✅ | `App.xaml.cs` | Directory for log files; defaults to `ApplicationPaths.DefaultLogsPath` |

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
| RecordingPath | ✅ | Used by RecordingService for per-camera recording location |
| RecordingFormat | ✅ | Used by RecordingService for per-camera format |
| EnableRecordingOnMotion | ✅ | Applied via `OverrideOrDefaultMultiConverter` in `CameraGrid.xaml` |
| EnableRecordingOnConnect | ✅ | Applied via `OverrideOrDefaultMultiConverter` in `CameraGrid.xaml`; auto-starts recording on connect |

### Summary Statistics

| Category | Implemented | Not Implemented | Total | Percentage |
|----------|-------------|-----------------|-------|------------|
| General | 6 | 0 | 6 | **100%** |
| Camera Display | 9 | 0 | 9 | **100%** |
| Connection | 9 | 0 | 9 | **100%** |
| Performance | 6 | 0 | 6 | **100%** |
| Motion Detection | 14 | 0 | 14 | **100%** |
| Recording | 15 | 0 | 15 | **100%** |
| Advanced | 2 | 0 | 2 | **100%** |
| **Total** | **61** | **0** | **61** | **100%** |

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
- ✅ Add `GetEffectiveValue<T>()` and `GetEffectiveStringValue()` methods

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

### Phase 6: Motion Detection Settings ✅
- ✅ Create `MotionDetectionSettings` model
- ✅ Create `BoundingBoxSettings` model (nested)
- ✅ Add Motion Detection tab to SettingsDialog
- ✅ Implement analysis settings UI
- ✅ Implement bounding box display settings UI
- ✅ Add `SaveMotionDetection()` method to service

### Phase 7: Extended Recording Settings ✅
- ✅ Create `MediaCleanupSettings` model (nested)
- ✅ Create `PlaybackOverlaySettings` model (nested)
- ✅ Add segmentation settings to Recording tab
- ✅ Add media cleanup settings to Recording tab
- ✅ Add playback overlay settings to Recording tab

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

### Recording System ✅ IMPLEMENTED

- **Status:** Implemented using FlyleafLib recording APIs
- **Settings:** RecordingPath, RecordingFormat, EnableRecordingOnMotion, EnableRecordingOnConnect, EnableHourlySegmentation, MaxRecordingDurationMinutes, ThumbnailTileCount
- **Location:** `IRecordingService`, `RecordingService.cs`, `IMotionDetectionService` (Core, aliased in WPF), `MotionDetectionService.cs`
- **Implementation:**
  - Manual recording: Context menu "Start Recording" / "Stop Recording"
  - Auto-record on connect: Global setting with per-camera overrides
  - Motion detection: Frame analysis with configurable sensitivity
  - Recording indicator: Visual indicator on camera tile when recording
  - Hourly segmentation: Automatic splits at hour boundaries
  - Thumbnail generation: Single or 2x2 grid thumbnails

### Media Cleanup ✅ IMPLEMENTED

- **Status:** Implemented with automatic cleanup service
- **Settings:** Schedule, RecordingRetentionDays, IncludeSnapshots, SnapshotRetentionDays
- **Location:** `IMediaCleanupService`, `MediaCleanupService.cs`
- **Implementation:**
  - Schedule options: Disabled, OnStartup, OnStartupAndPeriodically (6 hours)
  - Configurable retention periods for recordings and snapshots
  - Safe deletion with age verification

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
- ✅ `Models/Settings/ConnectionAppSettings.cs`
- ✅ `Models/Settings/PerformanceSettings.cs`
- ✅ `Models/Settings/RecordingSettings.cs`
- ✅ `Models/Settings/AdvancedSettings.cs`
- ✅ `Models/Settings/MotionDetectionSettings.cs` - Motion detection analysis parameters
- ✅ `Models/Settings/BoundingBoxSettings.cs` - Bounding box display options
- ✅ `Models/Settings/MediaCleanupSettings.cs` - Automatic cleanup configuration
- ✅ `Models/Settings/PlaybackOverlaySettings.cs` - Playback overlay display options
- ✅ `Models/CameraOverrides.cs` - Per-camera setting overrides
- ✅ `Enums/MediaCleanupSchedule.cs` - Cleanup schedule options
- ✅ `Factories/DropDownItemsFactory.cs` - Centralized dropdown items and defaults
- ✅ `Helpers/ApplicationPaths.cs` - Default paths for logs, snapshots, recordings, settings
- ✅ `Windows/FullScreenCameraWindow.xaml/.cs` - Moved from Dialogs/
- ✅ `Windows/FullScreenCameraWindowViewModel.cs` - Moved from ViewModels/
- ✅ `Windows/FullScreenRecordingWindow.xaml/.cs` - Moved from Dialogs/
- ✅ `Windows/FullScreenRecordingWindowViewModel.cs` - Moved from ViewModels/
- ✅ `Dialogs/Parts/Settings/GeneralAppearanceSettings.xaml/.cs`
- ✅ `Dialogs/Parts/Settings/GeneralStartupSettings.xaml/.cs`
- ✅ `Dialogs/Parts/Settings/CameraDisplayOverlaySettings.xaml/.cs`
- ✅ `Dialogs/Parts/Settings/CameraDisplayGridLayoutSettings.xaml/.cs`
- ✅ `Dialogs/Parts/Settings/CameraDisplaySnapshotSettings.xaml/.cs`
- ✅ `Dialogs/Parts/Settings/ConnectionDefaultCameraSettings.xaml/.cs`
- ✅ `Dialogs/Parts/Settings/ConnectionConnectionSettings.xaml/.cs`
- ✅ `Dialogs/Parts/Settings/ConnectionNotificationsSettings.xaml/.cs`
- ✅ `Dialogs/Parts/Settings/PerformanceVideoSettings.xaml/.cs`
- ✅ `Dialogs/Parts/Settings/PerformanceStreamingSettings.xaml/.cs`
- ✅ `Dialogs/Parts/Settings/MotionDetectionAnalysisSettings.xaml/.cs` - Analysis parameters UI
- ✅ `Dialogs/Parts/Settings/MotionDetectionBoundingBoxSettings.xaml/.cs` - Bounding box UI
- ✅ `Dialogs/Parts/Settings/RecordingGeneralSettings.xaml/.cs`
- ✅ `Dialogs/Parts/Settings/RecordingSegmentationSettings.xaml/.cs` - Segmentation settings UI
- ✅ `Dialogs/Parts/Settings/RecordingMediaCleanupSettings.xaml/.cs` - Media cleanup UI
- ✅ `Dialogs/Parts/Settings/RecordingPlaybackOverlaySettings.xaml/.cs` - Playback overlay UI
- ✅ `Dialogs/Parts/Settings/AdvancedLoggingSettings.xaml/.cs`
- ✅ `Dialogs/Parts/Settings/AdvancedMaintenanceSettings.xaml/.cs`

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
- ✅ `Models/Settings/ApplicationSettings.cs` - Added all 7 sections (including MotionDetection)
- ✅ `Models/Settings/CameraDisplayAppSettings.cs` - Renamed from DisplaySettings, added OverlayPosition
- ✅ `Services/IApplicationSettingsService.cs` - Added new properties/methods including MotionDetection
- ✅ `Services/ApplicationSettingsService.cs` - Implemented new structure
- ✅ `Services/CameraWallManager.cs` - Updated to use CameraDisplay
- ✅ `Services/DialogService.cs` - Apply defaults to new cameras
- ✅ `Dialogs/SettingsDialogViewModel.cs` - Use new settings structure
- ✅ `Dialogs/SettingsDialog.xaml` - Added all settings UI including Motion Detection tab
- ✅ `Resources/Translations.resx` - Added new translation keys
- ✅ `Resources/Translations.da-DK.resx` - Added Danish translations
- ✅ `Resources/Translations.de-DE.resx` - Added German translations
- ✅ `Directory.Build.props` - Added NBGV package reference
- ✅ `Linksoft.CameraWall.slnx` - Added comment about WiX installer
- ✅ `src/Linksoft.Wpf.CameraWall/Linksoft.Wpf.CameraWall.csproj` - Added NuGet package metadata
- ✅ `src/Linksoft.Wpf.CameraWall.App/Linksoft.Wpf.CameraWall.App.csproj` - Added icon content for installer
- ✅ `Windows/FullScreenCameraWindow.xaml` - Updated overlay bindings to respect per-camera settings
- ✅ `Windows/FullScreenCameraWindow.xaml.cs` - Added time display timer and overlay background opacity
- ✅ `Windows/FullScreenCameraWindowViewModel.cs` - Added overlay setting properties
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
| | EnableRecordingOnConnect | bool? | Auto-record when camera connects |

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
| **Recording Overrides** | ❌ No | Applied on next recording start |

---

## Notes

1. **Backward Compatibility:** The application handles legacy settings.json files gracefully. Missing sections use default values.

2. **Default Values:** All settings have sensible defaults defined in the model classes.

3. **Validation:** Numeric inputs use LabelIntegerBox with min/max constraints.

4. **JSON Serialization:** Uses camelCase naming policy and JsonStringEnumConverter for enums.

5. **WiX Installer:** Excluded from Visual Studio solution due to SDK compatibility issues. Build via command line or GitHub Actions.
