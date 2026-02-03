# 🎯 Motion Detection Implementation Plan

> **Goal:** Detect motion in camera streams, display bounding boxes around moving areas, and optionally trigger recording.

---

## 📋 Implementation Checklist

### Phase 1: Core Infrastructure 🔧 (Priority: HIGH)
- [x] **P1** Extend `MotionDetectionSettings` with bounding box settings
- [x] **P1** Extend `MotionDetectedEventArgs` to include bounding box data
- [x] **P1** Update `IMotionDetectionService` interface with bounding box support
- [x] **P1** Implement bounding box calculation in `MotionDetectionService`

### Phase 2: Settings UI 🎛️ (Priority: HIGH)
- [x] **P1** Add `ShowBoundingBoxInGrid` setting to `BoundingBoxSettings`
- [x] **P1** Add `ShowBoundingBoxInFullScreen` setting to `BoundingBoxSettings`
- [x] **P1** Add `EnableMotionTriggeredRecording` setting (already exists as `EnableRecordingOnMotion`)
- [x] **P2** Update `MotionDetectionAnalysisSettings.xaml` with new controls
- [x] **P2** Create `MotionDetectionBoundingBoxSettings.xaml` for bounding box display settings
- [x] **P2** Separate Motion Detection settings into own tab in Settings dialog
- [x] **P2** Add translations for new settings

### Phase 3: UI Overlay 🖼️ (Priority: MEDIUM)
- [x] **P2** Create `MotionBoundingBoxOverlay` UserControl
- [x] **P2** Integrate overlay into `CameraTile.xaml`
- [x] **P2** Integrate overlay into `FullScreenCameraWindow.xaml`
- [x] **P3** Implement coordinate mapping (analysis res → UI res)
- [x] **P3** Add smoothing to reduce jitter

### Phase 4: Recording Integration 📹 (Priority: MEDIUM)
- [x] **P2** Wire `MotionDetected` event to `IRecordingService`
- [x] **P2** Implement motion-triggered recording start/stop logic
- [x] **P3** Add hysteresis (cooldown, post-motion duration)

### Phase 5: Performance & Testing 🚀 (Priority: LOW)
- [x] **P3** Add scheduler for staggered analysis across streams
- [x] **P3** Optimize grayscale conversion (LockBits instead of GetPixel)
- [ ] **P3** Optimize frame capture (avoid temp files if possible) - *Requires FlyleafLib API investigation*
- [ ] **P4** Unit tests for `MotionDetectionService`
- [ ] **P4** Integration tests (8+ streams)

---

## 🏗️ Architecture Overview

### Current Implementation Status

| Component | Status | Location |
|-----------|--------|----------|
| `IMotionDetectionService` | ✅ Updated | `Services/IMotionDetectionService.cs` |
| `MotionDetectionService` | ✅ Updated (with bounding box) | `Services/MotionDetectionService.cs` |
| `MotionDetectionSettings` | ✅ Top-level in `ApplicationSettings` | `Models/Settings/MotionDetectionSettings.cs` |
| `BoundingBoxSettings` | ✅ Nested in `MotionDetectionSettings` | `Models/Settings/BoundingBoxSettings.cs` |
| `MotionDetectedEventArgs` | ✅ Updated (with bounding box) | `Events/MotionDetectedEventArgs.cs` |
| `RecordingSettings.EnableRecordingOnMotion` | ✅ Exists | `Models/Settings/RecordingSettings.cs` |
| Settings UI - Motion Detection | ✅ Own tab | `Dialogs/Parts/Settings/MotionDetectionAnalysisSettings.xaml` |
| Settings UI - Bounding Box | ✅ Own tab | `Dialogs/Parts/Settings/MotionDetectionBoundingBoxSettings.xaml` |
| Bounding Box Overlay | ✅ Complete | `UserControls/MotionBoundingBoxOverlay.xaml` |
| CameraTile Integration | ✅ Complete | `UserControls/CameraTile.xaml.cs` |
| FullScreen Integration | ✅ Complete | `Windows/FullScreenCameraWindow.xaml` |
| Recording Integration | ✅ Complete | `CameraTile.xaml.cs`, `RecordingService.cs` |
| Staggered Scheduler | ✅ Complete | `MotionDetectionService.cs` (round-robin analysis) |
| Optimized Grayscale | ✅ Complete | `MotionDetectionService.cs` (LockBits + unsafe) |

### Settings Architecture

Motion detection settings are **top-level** in `ApplicationSettings`, independent of recording:

```
ApplicationSettings
├── MotionDetection                        ← top-level (sensitivity, bounding box display)
│   ├── Sensitivity, MinimumChangePercent, AnalysisFrameRate
│   ├── PostMotionDurationSeconds, CooldownSeconds
│   └── BoundingBox (ShowInGrid, ShowInFullScreen, Color, Thickness, ...)
├── Recording                              ← recording only
│   ├── RecordingPath, RecordingFormat
│   ├── EnableRecordingOnMotion, EnableRecordingOnConnect
│   ├── Cleanup, PlaybackOverlay
│   └── EnableHourlySegmentation, MaxRecordingDurationMinutes
```

**Settings dialog tabs:** General | Camera Display | Connection | Performance | **Motion Detection** | Recording | Advanced

This separation allows motion detection with bounding box display to work independently of recording to disk.

### Data Flow

```
┌─────────────────┐     ┌────────────────────┐     ┌──────────────────┐
│  FlyleafPlayer  │────▶│MotionDetectionSvc  │────▶│MotionDetectedArgs│
│  (per camera)   │     │ (frame analysis)   │     │ + BoundingBox    │
└─────────────────┘     └────────────────────┘     └──────────────────┘
                                                            │
                        ┌───────────────────────────────────┼───────────────────────────┐
                        ▼                                   ▼                           ▼
              ┌─────────────────┐              ┌─────────────────────┐      ┌──────────────────┐
              │   CameraTile    │              │FullScreenCameraWnd  │      │ RecordingService │
              │ (BoundingBox    │              │ (BoundingBox        │      │ (start/stop rec) │
              │  if enabled)    │              │  if enabled)        │      │                  │
              └─────────────────┘              └─────────────────────┘      └──────────────────┘
```

---

## 📝 Detailed Implementation Tasks

### 1️⃣ Extend `MotionDetectionSettings` (P1) — ✅ DONE

**File:** `src/Linksoft.Wpf.CameraWall/Models/Settings/MotionDetectionSettings.cs`

Bounding box display settings are in a nested `BoundingBoxSettings` object. Colors use well-known color names (e.g., `"Red"`) compatible with `LabelWellKnownColorSelector` and `ColorConverter.ConvertFromString`.

```csharp
public class MotionDetectionSettings
{
    public int Sensitivity { get; set; } = 30;
    public double MinimumChangePercent { get; set; } = 2.0;
    public int AnalysisFrameRate { get; set; } = 2;
    public int PostMotionDurationSeconds { get; set; } = 10;
    public int CooldownSeconds { get; set; } = 5;
    public BoundingBoxSettings BoundingBox { get; set; } = new();
}

public class BoundingBoxSettings
{
    public bool ShowInGrid { get; set; }
    public bool ShowInFullScreen { get; set; }
    public string Color { get; set; } = "Red";           // well-known color name
    public int Thickness { get; set; } = 2;
    public int MinArea { get; set; } = 100;
    public int Padding { get; set; } = 4;
    public double Smoothing { get; set; } = 0.3;
}
```

---

### 2️⃣ Extend `MotionDetectedEventArgs` (P1)

**File:** `src/Linksoft.Wpf.CameraWall/Events/MotionDetectedEventArgs.cs`

```csharp
// Add bounding box properties:

/// <summary>
/// Gets the bounding box of the detected motion in analysis coordinates.
/// Null if no distinct motion region was identified.
/// </summary>
public Rect? BoundingBox { get; }

/// <summary>
/// Gets the analysis resolution width used for the bounding box coordinates.
/// </summary>
public int AnalysisWidth { get; }

/// <summary>
/// Gets the analysis resolution height used for the bounding box coordinates.
/// </summary>
public int AnalysisHeight { get; }

/// <summary>
/// Gets a value indicating whether motion is currently active (above threshold).
/// </summary>
public bool IsMotionActive { get; }
```

---

### 3️⃣ Update `MotionDetectionService` (P1)

**File:** `src/Linksoft.Wpf.CameraWall/Services/MotionDetectionService.cs`

**Changes needed in `AnalyzeFrame` method:**

```csharp
// In the frame difference calculation loop, track bounding box:
int minX = int.MaxValue, minY = int.MaxValue;
int maxX = int.MinValue, maxY = int.MinValue;
int motionPixelCount = 0;

for (var y = 0; y < targetHeight; y++)
{
    for (var x = 0; x < targetWidth; x++)
    {
        var i = (y * targetWidth) + x;
        var diff = Math.Abs(current[i] - previous[i]);
        if (diff > threshold)
        {
            changedPixels++;
            motionPixelCount++;

            // Track bounding box
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }
    }
}

// Create bounding box if enough motion pixels
Rect? boundingBox = null;
if (motionPixelCount >= settings.MinMotionPixels)
{
    var width = maxX - minX + 1;
    var height = maxY - minY + 1;
    var area = width * height;

    if (area >= settings.MinBoundingBoxArea)
    {
        // Add padding
        var padding = settings.BoundingBoxPadding;
        boundingBox = new Rect(
            Math.Max(0, minX - padding),
            Math.Max(0, minY - padding),
            Math.Min(targetWidth - minX, width + (2 * padding)),
            Math.Min(targetHeight - minY, height + (2 * padding)));
    }
}

// Include bounding box in event args
MotionDetected?.Invoke(this, new MotionDetectedEventArgs(
    cameraId,
    changePercent,
    boundingBox,
    AnalysisWidth,
    AnalysisHeight,
    isMotionActive: changePercent >= minimumChange));
```

---

### 4️⃣ Create `MotionBoundingBoxOverlay` UserControl (P2) — ✅ DONE

**File:** `src/Linksoft.Wpf.CameraWall/UserControls/MotionBoundingBoxOverlay.xaml(.cs)`

Dependency properties: `IsOverlayEnabled`, `BoxColor` (color name string, default `"Red"`), `BoxThickness`.
Uses `ColorConverter.ConvertFromString` which accepts both color names and hex values, providing backward compatibility with any persisted hex values.

---

### 5️⃣ Settings UI (P2) — ✅ DONE

Motion detection settings now have their own **Motion Detection** tab in the Settings dialog (between Performance and Recording).

**Motion Detection tab** (`Dialogs/Parts/Settings/`):
- `MotionDetectionAnalysisSettings.xaml` — sensitivity, post-motion duration, frame rate, cooldown
- `MotionDetectionBoundingBoxSettings.xaml` — show in grid/fullscreen toggles, `LabelWellKnownColorSelector` for color, thickness

**Recording tab** keeps `EnableRecordingOnMotion` and `EnableRecordingOnConnect` toggles, segmentation, cleanup, and playback overlay.

The bounding box color picker uses `atc:LabelWellKnownColorSelector` (from Atc.Wpf.Forms) which provides a visual color picker with well-known color names.

---

### 6️⃣ Add Translations (P2)

**File:** `src/Linksoft.Wpf.CameraWall/Resources/Translations.resx`

| Key | Value |
|-----|-------|
| `EnableMotionDetection` | Enable Motion Detection |
| `ShowBoundingBoxInGrid` | Show bounding boxes in grid view |
| `ShowBoundingBoxInFullScreen` | Show bounding boxes in full screen |
| `BoundingBoxColor` | Bounding box color |
| `BoundingBoxDisplay` | Bounding Box Display |
| `MotionTriggeredRecording` | Motion-Triggered Recording |

---

### 7️⃣ Integrate Overlay into `CameraTile.xaml` (P2)

**File:** `src/Linksoft.Wpf.CameraWall/UserControls/CameraTile.xaml`

Add after the CameraOverlay control:

```xml
<!-- Motion Bounding Box Overlay (conditionally shown based on settings) -->
<local:MotionBoundingBoxOverlay
    x:Name="MotionOverlay"
    IsEnabled="{Binding HostDataContext.ShowBoundingBoxInGrid}"
    BoxColor="{Binding HostDataContext.BoundingBoxColor}"
    BoxThickness="{Binding HostDataContext.BoundingBoxThickness}"
    IsHitTestVisible="False" />
```

**File:** `src/Linksoft.Wpf.CameraWall/UserControls/CameraTile.xaml.cs`

Subscribe to motion events and update the overlay:

```csharp
private void OnMotionDetected(object? sender, MotionDetectedEventArgs e)
{
    if (e.CameraId != Camera?.Id)
        return;

    Dispatcher.InvokeAsync(() =>
    {
        var videoSize = new Size(VideoPlayer.ActualWidth, VideoPlayer.ActualHeight);
        MotionOverlay.UpdateBoundingBox(e.BoundingBox, videoSize);
    });
}
```

---

### 8️⃣ Integrate Overlay into `FullScreenCameraWindow.xaml` (P2)

**File:** `src/Linksoft.Wpf.CameraWall/Windows/FullScreenCameraWindow.xaml`

Add inside the FlyleafHost's Grid:

```xml
<!-- Motion Bounding Box Overlay -->
<local:MotionBoundingBoxOverlay
    x:Name="MotionOverlay"
    IsEnabled="{Binding HostDataContext.ShowBoundingBoxInFullScreen}"
    BoxColor="{Binding HostDataContext.BoundingBoxColor}"
    BoxThickness="{Binding HostDataContext.BoundingBoxThickness}"
    IsHitTestVisible="False" />
```

---

### 9️⃣ Wire Motion Detection to Recording (P2)

**File:** `src/Linksoft.Wpf.CameraWall/Services/CameraWallManager.cs`

```csharp
// In Initialize or constructor, subscribe to motion events:
private void SubscribeToMotionEvents()
{
    _motionDetectionService.MotionDetected += OnMotionDetected;
}

private void OnMotionDetected(object? sender, MotionDetectedEventArgs e)
{
    if (!_settings.Recording.EnableRecordingOnMotion)
        return;

    var camera = _cameraStorageService.GetCameraById(e.CameraId);
    if (camera is null)
        return;

    // Find the player for this camera
    var player = GetPlayerForCamera(e.CameraId);
    if (player is null)
        return;

    if (e.IsMotionActive)
    {
        // Start recording if not already recording
        _recordingService.TriggerMotionRecording(camera, player);
    }
    // Note: Recording stops automatically after PostMotionDuration via RecordingService
}
```

---

## 🎨 Bounding Box Color — ✅ DONE

Bounding box color uses `LabelWellKnownColorSelector` from Atc.Wpf.Forms, which provides the full set of WPF well-known colors with visual swatches. No custom color dictionary needed.

Default color: `"Red"` (set in `DropDownItemsFactory.DefaultBoundingBoxColor` and `BoundingBoxSettings.Color`).

Colors are stored as well-known color name strings (e.g., `"Red"`, `"Blue"`). The rendering code uses `ColorConverter.ConvertFromString` which handles both color names and hex values for backward compatibility.

---

## ⚡ Performance Considerations

### Current Issues in `MotionDetectionService`
1. ❌ Uses temp files for frame capture (slow I/O)
2. ❌ Uses `GetPixel()` for grayscale conversion (very slow)
3. ❌ No buffer reuse (GC pressure)

### Recommended Optimizations (P3)

```csharp
// Use LockBits for fast pixel access:
private static byte[] ConvertToGrayscaleFast(Bitmap bitmap, int targetWidth, int targetHeight)
{
    using var resized = new Bitmap(bitmap, new Size(targetWidth, targetHeight));
    var rect = new Rectangle(0, 0, targetWidth, targetHeight);
    var data = resized.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

    try
    {
        var grayscale = new byte[targetWidth * targetHeight];
        var stride = data.Stride;

        unsafe
        {
            var ptr = (byte*)data.Scan0;
            for (var y = 0; y < targetHeight; y++)
            {
                var row = ptr + (y * stride);
                for (var x = 0; x < targetWidth; x++)
                {
                    var b = row[x * 3];
                    var g = row[x * 3 + 1];
                    var r = row[x * 3 + 2];
                    grayscale[(y * targetWidth) + x] = (byte)((0.299 * r) + (0.587 * g) + (0.114 * b));
                }
            }
        }

        return grayscale;
    }
    finally
    {
        resized.UnlockBits(data);
    }
}
```

### Scheduler for Multiple Streams (P3)

```csharp
// Stagger analysis to avoid CPU spikes
public class MotionAnalysisScheduler
{
    private readonly List<Guid> _cameraIds = new();
    private int _currentIndex;
    private readonly DispatcherTimer _timer;

    public void AddCamera(Guid cameraId)
    {
        _cameraIds.Add(cameraId);
        RecalculateIntervals();
    }

    private void RecalculateIntervals()
    {
        // If 10 cameras at 2 FPS each = 20 analyses/sec
        // Spread evenly: analyze 1 camera every 50ms
        var intervalMs = 1000.0 / (_cameraIds.Count * TargetFps);
        _timer.Interval = TimeSpan.FromMilliseconds(intervalMs);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_cameraIds.Count == 0) return;

        var cameraId = _cameraIds[_currentIndex];
        _currentIndex = (_currentIndex + 1) % _cameraIds.Count;

        // Trigger analysis for this camera
        AnalyzeCamera(cameraId);
    }
}
```

---

## 🧪 Testing Plan

### Unit Tests (P4)

| Test | Description |
|------|-------------|
| `MotionAnalyzer_NoMotion_ReturnsZeroScore` | Two identical frames → score ≈ 0 |
| `MotionAnalyzer_FullMotion_ReturnsHighScore` | Completely different frames → score high |
| `MotionAnalyzer_MovingBlock_ReturnsBoundingBox` | Synthetic moving rectangle → correct bbox |
| `MotionAnalyzer_Noise_FilteredByThreshold` | Random noise → no bbox (below threshold) |
| `BoundingBoxMapper_MapsCorrectly` | Analysis coords → UI coords accurately |
| `BoundingBoxSmoothing_ReducesJitter` | Rapidly changing boxes → smooth output |

### Integration Tests (P4)

| Test | Description |
|------|-------------|
| `SingleStream_OverlayDisplays` | Motion detected → bbox visible in UI |
| `MultiStream_8Cameras_NoStutter` | 8 streams analyzing → UI remains responsive |
| `MotionTrigger_StartsRecording` | Motion → recording starts automatically |
| `MotionStops_RecordingStopsAfterCooldown` | Motion ends → recording stops after delay |

---

## 📅 Delivery Milestones

| Milestone | Description | Priority Items |
|-----------|-------------|----------------|
| **M1** 🎯 | Settings + basic bounding box calculation | P1 tasks |
| **M2** 🖼️ | Overlay in grid view working | P2 overlay tasks |
| **M3** 📺 | Overlay in full screen working | P2 fullscreen tasks |
| **M4** 📹 | Motion-triggered recording | P2 recording tasks |
| **M5** 🚀 | Performance optimizations | P3 tasks |
| **M6** ✅ | Tests + polish | P4 tasks |

---

## 📁 Files Created/Modified

### Created Files
- [x] `UserControls/MotionBoundingBoxOverlay.xaml(.cs)`
- [x] `Models/Settings/BoundingBoxSettings.cs`

### Modified Files
- [x] `Models/Settings/ApplicationSettings.cs` - Added top-level `MotionDetection` property
- [x] `Models/Settings/MotionDetectionSettings.cs` - Added `BoundingBox` sub-object
- [x] `Models/Settings/RecordingSettings.cs` - Removed `MotionDetection` (moved to top-level)
- [x] `Services/IApplicationSettingsService.cs` - Added `MotionDetection` property + `SaveMotionDetection()`
- [x] `Services/ApplicationSettingsService.cs` - Implemented above
- [x] `Events/MotionDetectedEventArgs.cs` - Added bounding box data
- [x] `Services/IMotionDetectionService.cs` - Interface changes
- [x] `Services/MotionDetectionService.cs` - Bounding box calculation
- [x] `Services/RecordingService.cs` - Updated to use `settingsService.MotionDetection`
- [x] `UserControls/CameraTile.xaml(.cs)` - Added overlay, updated color default
- [x] `UserControls/CameraGrid.xaml(.cs)` - Updated color default
- [x] `Windows/FullScreenCameraWindow.xaml` - Added overlay
- [x] `Windows/FullScreenCameraWindowViewModel.cs` - Updated color default
- [x] `Dialogs/Parts/Settings/MotionDetectionAnalysisSettings.xaml(.cs)` - Renamed from `RecordingMotionDetectionSettings`
- [x] `Dialogs/Parts/Settings/MotionDetectionBoundingBoxSettings.xaml(.cs)` - Renamed from `RecordingBoundingBoxSettings`, uses `LabelWellKnownColorSelector`
- [x] `Dialogs/SettingsDialog.xaml` - Added Motion Detection tab
- [x] `Dialogs/SettingsDialogViewModel.cs` - Separated motion detection region, updated Load/Save
- [x] `Dialogs/CameraConfigurationDialogViewModel.cs` - Updated to use `settingsService.MotionDetection`
- [x] `Factories/DropDownItemsFactory.cs` - Removed `BoundingBoxColorItems`, changed `DefaultBoundingBoxColor` to `"Red"`
- [x] `Resources/Translations.resx` - Added `MotionDetection` key
- [x] `Resources/Translations.da-DK.resx` - Added Danish translation
- [x] `Resources/Translations.de-DE.resx` - Added German translation

---

## 🔗 Related Code

| File | Relevance |
|------|-----------|
| `Services/RecordingService.cs` | Has `TriggerMotionRecording()`, uses `settingsService.MotionDetection` |
| `Services/IApplicationSettingsService.cs` | `MotionDetection` property + `SaveMotionDetection()` |
| `RecordingSettings.EnableRecordingOnMotion` | Master switch for motion recording (in Recording tab) |
| `CameraTile.xaml` | Hosts `MotionBoundingBoxOverlay` |
| `FullScreenCameraWindow.xaml` | Hosts `MotionBoundingBoxOverlay` |

---

## ✅ Decisions Made

| Question | Decision | Priority |
|----------|----------|----------|
| 1. Frame Source | ✅ **Yes** - Investigate FlyleafLib's frame callback API | P2 |
| 2. Multiple Regions | ✅ **Yes** - Support multiple bounding boxes per camera | P3 |
| 3. Zone Masking | ✅ **Yes** - Allow users to define "ignore zones" | P3 |
| 4. Sensitivity Presets | ✅ **Yes** - Add Low/Medium/High presets | P2 |

---

## 🆕 Additional Features (Based on Decisions)

### 🔟 FlyleafLib Frame Callback API (P2)

**Goal:** Replace temp file snapshots with direct frame access for better performance.

**Investigation Tasks:**
- [ ] **P2** Research FlyleafLib's `Player.VideoDecoder` frame access
- [ ] **P2** Check if `Player.renderer` exposes frame data
- [ ] **P2** Test `VideoFrame` callback subscription
- [ ] **P2** Implement direct frame capture if API available

**Potential Approaches:**

```csharp
// Option A: VideoDecoder frame callback (if available)
player.VideoDecoder.VideoFrameDecoded += (sender, frame) =>
{
    // frame.Data contains raw pixel data
    ProcessFrame(frame.Data, frame.Width, frame.Height);
};

// Option B: Use Player.TakeSnapshot() to memory (check if available)
// Some versions support BitmapSource output instead of file

// Option C: Hook into D3D11 renderer (advanced)
// Access the rendered texture and copy to CPU memory
```

**File:** `src/Linksoft.Wpf.CameraWall/Services/FrameProviders/IFrameProvider.cs`

```csharp
/// <summary>
/// Provides frames from a video player for analysis.
/// </summary>
public interface IFrameProvider
{
    /// <summary>
    /// Gets a grayscale frame for motion analysis.
    /// </summary>
    /// <param name="player">The FlyleafLib player.</param>
    /// <param name="targetWidth">Target width for downscaling.</param>
    /// <param name="targetHeight">Target height for downscaling.</param>
    /// <returns>Grayscale byte array, or null if frame unavailable.</returns>
    byte[]? GetGrayscaleFrame(Player player, int targetWidth, int targetHeight);
}

// Implementations:
// - TempFileFrameProvider (current, fallback)
// - DirectFrameProvider (new, preferred if FlyleafLib supports it)
```

---

### 1️⃣1️⃣ Multiple Bounding Boxes Support (P3)

**Goal:** Detect and display multiple motion regions when motion occurs in separate areas.

**New Settings in `MotionDetectionSettings`:**

```csharp
/// <summary>
/// Gets or sets the maximum number of bounding boxes to display per camera.
/// </summary>
public int MaxBoundingBoxes { get; set; } = 5;

/// <summary>
/// Gets or sets the minimum distance (in pixels) between bounding boxes to be considered separate regions.
/// </summary>
public int MinRegionSeparation { get; set; } = 20;
```

**Updated `MotionDetectedEventArgs`:**

```csharp
/// <summary>
/// Gets the list of bounding boxes for detected motion regions.
/// Empty if no motion detected.
/// </summary>
public IReadOnlyList<Rect> BoundingBoxes { get; }

// Keep single BoundingBox for backwards compatibility
public Rect? BoundingBox => BoundingBoxes.FirstOrDefault();
```

**Algorithm: Connected Component Labeling (simplified)**

```csharp
private List<Rect> FindMotionRegions(byte[] motionMask, int width, int height, MotionDetectionSettings settings)
{
    var regions = new List<Rect>();
    var visited = new bool[width * height];

    for (var y = 0; y < height; y++)
    {
        for (var x = 0; x < width; x++)
        {
            var i = (y * width) + x;
            if (motionMask[i] == 0 || visited[i])
                continue;

            // Flood fill to find connected region
            var region = FloodFill(motionMask, visited, x, y, width, height);

            if (region.Width * region.Height >= settings.MinBoundingBoxArea)
            {
                // Add padding
                region = InflateRect(region, settings.BoundingBoxPadding, width, height);
                regions.Add(region);

                if (regions.Count >= settings.MaxBoundingBoxes)
                    break;
            }
        }

        if (regions.Count >= settings.MaxBoundingBoxes)
            break;
    }

    // Merge overlapping regions
    return MergeOverlappingRegions(regions, settings.MinRegionSeparation);
}
```

**Updated `MotionBoundingBoxOverlay`:**

```xml
<UserControl x:Class="Linksoft.Wpf.CameraWall.UserControls.MotionBoundingBoxOverlay"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             IsHitTestVisible="False">
    <Canvas x:Name="OverlayCanvas">
        <!-- ItemsControl for multiple rectangles -->
        <ItemsControl ItemsSource="{Binding BoundingBoxes}">
            <ItemsControl.ItemsPanel>
                <ItemsPanelTemplate>
                    <Canvas />
                </ItemsPanelTemplate>
            </ItemsControl.ItemsPanel>
            <ItemsControl.ItemTemplate>
                <DataTemplate>
                    <Rectangle
                        Stroke="{Binding DataContext.BoundingBoxBrush, RelativeSource={RelativeSource AncestorType=UserControl}}"
                        StrokeThickness="{Binding DataContext.BoundingBoxThickness, RelativeSource={RelativeSource AncestorType=UserControl}}"
                        Fill="Transparent"
                        Canvas.Left="{Binding Left}"
                        Canvas.Top="{Binding Top}"
                        Width="{Binding Width}"
                        Height="{Binding Height}" />
                </DataTemplate>
            </ItemsControl.ItemTemplate>
        </ItemsControl>
    </Canvas>
</UserControl>
```

---

### 1️⃣2️⃣ Zone Masking (Ignore Zones) (P3)

**Goal:** Allow users to define rectangular zones where motion should be ignored (e.g., trees swaying, busy roads).

**New Model: `MotionIgnoreZone`**

**File:** `src/Linksoft.Wpf.CameraWall/Models/MotionIgnoreZone.cs`

```csharp
/// <summary>
/// Represents a rectangular zone where motion should be ignored.
/// Coordinates are normalized (0.0-1.0) relative to video dimensions.
/// </summary>
public class MotionIgnoreZone
{
    /// <summary>
    /// Gets or sets the unique identifier for this zone.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the display name for this zone.
    /// </summary>
    public string Name { get; set; } = "Ignore Zone";

    /// <summary>
    /// Gets or sets the normalized X coordinate (0.0-1.0).
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Gets or sets the normalized Y coordinate (0.0-1.0).
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Gets or sets the normalized width (0.0-1.0).
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Gets or sets the normalized height (0.0-1.0).
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this zone is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}
```

**Add to `CameraConfiguration`:**

```csharp
/// <summary>
/// Gets or sets the list of motion ignore zones for this camera.
/// </summary>
public List<MotionIgnoreZone> MotionIgnoreZones { get; set; } = new();
```

**Apply Mask in `MotionDetectionService`:**

```csharp
private byte[] ApplyIgnoreZoneMask(byte[] motionMask, int width, int height, List<MotionIgnoreZone> zones)
{
    foreach (var zone in zones.Where(z => z.IsEnabled))
    {
        // Convert normalized coordinates to pixel coordinates
        var zoneX = (int)(zone.X * width);
        var zoneY = (int)(zone.Y * height);
        var zoneW = (int)(zone.Width * width);
        var zoneH = (int)(zone.Height * height);

        // Zero out motion pixels within the zone
        for (var y = zoneY; y < Math.Min(zoneY + zoneH, height); y++)
        {
            for (var x = zoneX; x < Math.Min(zoneX + zoneW, width); x++)
            {
                motionMask[(y * width) + x] = 0;
            }
        }
    }

    return motionMask;
}
```

**UI for Zone Management:**

**File:** `src/Linksoft.Wpf.CameraWall/Dialogs/MotionZoneEditorDialog.xaml`

```xml
<Window x:Class="Linksoft.Wpf.CameraWall.Dialogs.MotionZoneEditorDialog"
        Title="🚫 Motion Ignore Zones" Height="600" Width="800">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*" />
            <ColumnDefinition Width="200" />
        </Grid.ColumnDefinitions>

        <!-- Video preview with draggable zones overlay -->
        <Grid Grid.Column="0">
            <fl:FlyleafHost Player="{Binding PreviewPlayer}" />
            <Canvas x:Name="ZoneCanvas" Background="Transparent">
                <!-- Draggable/resizable rectangles for each zone -->
                <ItemsControl ItemsSource="{Binding IgnoreZones}">
                    <!-- Zone rectangles with resize handles -->
                </ItemsControl>
            </Canvas>
        </Grid>

        <!-- Zone list -->
        <StackPanel Grid.Column="1" Margin="8">
            <TextBlock Text="📋 Ignore Zones" FontWeight="Bold" Margin="0,0,0,8" />
            <ListBox ItemsSource="{Binding IgnoreZones}" SelectedItem="{Binding SelectedZone}">
                <ListBox.ItemTemplate>
                    <DataTemplate>
                        <StackPanel Orientation="Horizontal">
                            <CheckBox IsChecked="{Binding IsEnabled}" Margin="0,0,4,0" />
                            <TextBlock Text="{Binding Name}" />
                        </StackPanel>
                    </DataTemplate>
                </ListBox.ItemTemplate>
            </ListBox>

            <StackPanel Orientation="Horizontal" Margin="0,8,0,0">
                <Button Content="➕ Add" Command="{Binding AddZoneCommand}" Margin="0,0,4,0" />
                <Button Content="🗑️ Delete" Command="{Binding DeleteZoneCommand}" />
            </StackPanel>
        </StackPanel>
    </Grid>
</Window>
```

**Add button to camera configuration dialog:**

```xml
<Button Content="🚫 Edit Ignore Zones..."
        Command="{Binding EditIgnoreZonesCommand}"
        ToolTip="Define areas where motion should be ignored" />
```

---

### 1️⃣3️⃣ Sensitivity Presets (P2)

**Goal:** Provide user-friendly presets instead of numeric values.

**New Enum:**

**File:** `src/Linksoft.Wpf.CameraWall/Models/Enums/MotionSensitivityPreset.cs`

```csharp
/// <summary>
/// Predefined sensitivity presets for motion detection.
/// </summary>
public enum MotionSensitivityPreset
{
    /// <summary>
    /// Custom settings (use numeric values).
    /// </summary>
    Custom = 0,

    /// <summary>
    /// Low sensitivity - only detect large/obvious motion.
    /// Good for busy environments.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium sensitivity - balanced detection.
    /// Good for most scenarios.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High sensitivity - detect subtle motion.
    /// Good for secure/quiet areas.
    /// </summary>
    High = 3,

    /// <summary>
    /// Very high sensitivity - detect very small changes.
    /// May produce false positives.
    /// </summary>
    VeryHigh = 4
}
```

**Preset Values:**

**File:** `src/Linksoft.Wpf.CameraWall/Factories/MotionSensitivityPresets.cs`

```csharp
public static class MotionSensitivityPresets
{
    public static readonly IDictionary<string, string> PresetItems = new Dictionary<string, string>
    {
        [nameof(MotionSensitivityPreset.Low)] = "🔵 Low (busy areas)",
        [nameof(MotionSensitivityPreset.Medium)] = "🟢 Medium (recommended)",
        [nameof(MotionSensitivityPreset.High)] = "🟠 High (quiet areas)",
        [nameof(MotionSensitivityPreset.VeryHigh)] = "🔴 Very High (sensitive)",
        [nameof(MotionSensitivityPreset.Custom)] = "⚙️ Custom",
    };

    public static (int Sensitivity, double MinChangePercent, int AnalysisFps) GetPresetValues(MotionSensitivityPreset preset)
        => preset switch
        {
            MotionSensitivityPreset.Low => (20, 5.0, 2),      // Low sensitivity, high threshold
            MotionSensitivityPreset.Medium => (40, 2.5, 3),   // Balanced
            MotionSensitivityPreset.High => (60, 1.5, 4),     // High sensitivity
            MotionSensitivityPreset.VeryHigh => (80, 0.8, 5), // Very sensitive
            _ => (30, 2.0, 2),                                 // Default/Custom
        };

    public static MotionSensitivityPreset DetectPreset(int sensitivity, double minChangePercent)
    {
        // Check if current values match any preset
        foreach (MotionSensitivityPreset preset in Enum.GetValues<MotionSensitivityPreset>())
        {
            if (preset == MotionSensitivityPreset.Custom)
                continue;

            var (s, m, _) = GetPresetValues(preset);
            if (Math.Abs(sensitivity - s) <= 5 && Math.Abs(minChangePercent - m) <= 0.5)
                return preset;
        }

        return MotionSensitivityPreset.Custom;
    }
}
```

**Add to `MotionDetectionSettings`:**

```csharp
/// <summary>
/// Gets or sets the sensitivity preset.
/// When set to a preset other than Custom, updates Sensitivity and MinimumChangePercent automatically.
/// </summary>
public MotionSensitivityPreset SensitivityPreset { get; set; } = MotionSensitivityPreset.Medium;
```

**Updated Settings UI:**

```xml
<!-- Sensitivity Preset Selector -->
<GroupBox Header="🎚️ Sensitivity" Margin="0,0,0,16">
    <StackPanel Orientation="Vertical">
        <atc:LabelComboBox
            HideAreas="Validation"
            LabelText="Preset"
            Items="{x:Static factories:MotionSensitivityPresets.PresetItems}"
            SelectedKey="{Binding Path=SensitivityPreset, Mode=TwoWay}"
            Orientation="Vertical" />

        <!-- Show custom controls only when Custom is selected -->
        <StackPanel Visibility="{Binding IsCustomPreset, Converter={StaticResource BooleanToVisibilityConverter}}">
            <atc:LabelIntegerBox
                LabelText="Sensitivity (0-100)"
                Value="{Binding Path=MotionSensitivity, Mode=TwoWay}"
                Minimum="0" Maximum="100"
                Orientation="Vertical" />

            <atc:LabelDecimalBox
                LabelText="Min Change %"
                Value="{Binding Path=MinimumChangePercent, Mode=TwoWay}"
                Minimum="0.1" Maximum="20"
                Orientation="Vertical" />
        </StackPanel>
    </StackPanel>
</GroupBox>
```

---

## 📋 Updated Implementation Checklist

### Phase 6: Frame Provider Abstraction 🎬 (Priority: MEDIUM)
- [ ] **P2** Create `IFrameProvider` interface
- [ ] **P2** Implement `TempFileFrameProvider` (current behavior)
- [ ] **P2** Research FlyleafLib direct frame access
- [ ] **P2** Implement `DirectFrameProvider` if API available
- [ ] **P2** Add fallback logic in `MotionDetectionService`

### Phase 7: Multiple Bounding Boxes 📦 (Priority: LOW)
- [ ] **P3** Add `MaxBoundingBoxes` and `MinRegionSeparation` settings
- [ ] **P3** Update `MotionDetectedEventArgs` with `BoundingBoxes` list
- [ ] **P3** Implement connected component labeling algorithm
- [ ] **P3** Update `MotionBoundingBoxOverlay` to display multiple boxes

### Phase 8: Zone Masking 🚫 (Priority: LOW)
- [ ] **P3** Create `MotionIgnoreZone` model
- [ ] **P3** Add `MotionIgnoreZones` to `CameraConfiguration`
- [ ] **P3** Create `MotionZoneEditorDialog` with visual zone drawing
- [ ] **P3** Apply mask in `MotionDetectionService.AnalyzeFrame()`
- [ ] **P3** Add "Edit Ignore Zones" button to camera config dialog

### Phase 9: Sensitivity Presets 🎚️ (Priority: MEDIUM)
- [ ] **P2** Create `MotionSensitivityPreset` enum
- [ ] **P2** Create `MotionSensitivityPresets` factory class
- [ ] **P2** Add `SensitivityPreset` to `MotionDetectionSettings`
- [ ] **P2** Update settings UI with preset dropdown
- [ ] **P2** Auto-detect preset from current values

---

## 📁 Additional Files to Create

| File | Description |
|------|-------------|
| `Services/FrameProviders/IFrameProvider.cs` | Frame provider interface |
| `Services/FrameProviders/TempFileFrameProvider.cs` | Current temp file implementation |
| `Services/FrameProviders/DirectFrameProvider.cs` | Direct FlyleafLib access (if available) |
| `Models/MotionIgnoreZone.cs` | Ignore zone model |
| `Models/Enums/MotionSensitivityPreset.cs` | Sensitivity preset enum |
| `Factories/MotionSensitivityPresets.cs` | Preset values and detection |
| `Dialogs/MotionZoneEditorDialog.xaml` | Zone editor UI |
| `Dialogs/MotionZoneEditorDialogViewModel.cs` | Zone editor ViewModel |

---

## 📅 Updated Delivery Milestones

| Milestone | Description | Priority Items |
|-----------|-------------|----------------|
| **M1** 🎯 | Settings + basic bounding box calculation | P1 tasks |
| **M2** 🎛️ | Sensitivity presets + settings UI | P2 presets |
| **M3** 🖼️ | Overlay in grid view working | P2 overlay tasks |
| **M4** 📺 | Overlay in full screen working | P2 fullscreen tasks |
| **M5** 📹 | Motion-triggered recording | P2 recording tasks |
| **M6** 🎬 | Frame provider abstraction | P2 frame provider |
| **M7** 📦 | Multiple bounding boxes | P3 tasks |
| **M8** 🚫 | Zone masking | P3 tasks |
| **M9** 🚀 | Performance optimizations | P3 perf tasks |
| **M10** ✅ | Tests + polish | P4 tasks |
