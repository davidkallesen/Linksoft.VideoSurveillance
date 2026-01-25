# 📹 Linksoft.CameraWall

A professional WPF camera wall application for live monitoring of multiple RTSP/HTTP camera streams with an intuitive ribbon interface and dynamic grid layout.

## ✨ Features

### 🖥️ Camera Display
- **Dynamic Grid Layout** - Auto-calculates optimal grid based on camera count (optimized for 4:3 aspect ratio)
- **Camera Tile Overlay** - Configurable overlay showing title, description, timestamp, and connection status
- **Overlay Configuration** - Choose corner position (TopLeft, TopRight, BottomLeft, BottomRight) and opacity
- **Full Screen Mode** - View any camera in full screen with Escape to exit
- **Connection State Indicators** - Visual indicators for Connected, Connecting, and Error states

### 📷 Camera Management
- **Context Menu** - Edit, Delete, Full Screen, Swap Left/Right, Snapshot, Reconnect, Start/Stop Recording
- **Drag-and-Drop** - Reorder cameras by dragging tiles within the grid
- **Network Scanner** - Auto-discover cameras on local network (integrated in Add Camera dialog)
- **Test Connection** - Validate camera connectivity before saving (TCP for RTSP, HTTP GET for HTTP/HTTPS)
- **Multiple Protocols** - RTSP, HTTP, and HTTPS support with configurable ports and paths

### ⏺️ Recording
- **Manual Recording** - Start/stop recording per camera via context menu
- **Auto-Record on Connect** - Configurable global setting with per-camera overrides
- **Motion-Triggered Recording** - Automatically record when motion is detected
- **Recording Segmentation** - Clock-aligned automatic file segmentation (e.g., every 15 minutes at :00, :15, :30, :45)
- **Recording Format** - Configurable format (MP4, MKV, AVI)
- **Recording Indicator** - Visual indicator when recording is active
- **Thumbnail Generation** - Auto-generates 2x2 grid thumbnails from recording frames

### 🎯 Motion Detection
- **Frame-Based Detection** - Compares consecutive frames to detect motion
- **Configurable Sensitivity** - Adjust threshold for motion detection
- **Motion Events** - Triggers events for recording and notifications

### 📐 Layout Management
- **Named Layouts** - Create, save, and switch between named layouts
- **Startup Layout** - Designate a layout to load on application start
- **Auto-Save** - Automatic persistence of camera positions (configurable)
- **Camera Assignment** - Dual-list dialog for assigning cameras to layouts

### 🧹 Media Cleanup
- **Automatic Cleanup** - Remove old recordings and snapshots based on retention policy
- **Flexible Scheduling** - On startup, periodic, or manual cleanup
- **Configurable Retention** - Set maximum age for recordings and snapshots

### 🎨 User Interface
- **Fluent Ribbon UI** - Modern ribbon interface with Layouts, Cameras, View, and Help tabs
- **Theme Support** - Dark/Light theme switching with configurable accent colors
- **Multi-Language** - Localization support via LCID-based language selection
- **Settings Dialog** - Configure general and display settings
- **Recordings Browser** - Browse and playback recorded videos
- **About Dialog** - Application version and information
- **Check for Updates** - GitHub-based update checking

## 🔧 Services

The library provides a comprehensive set of services for camera management, recording, and system operations. All services are registered via dependency injection using `[Registration]` attributes.

### 🎬 ICameraWallManager

The main facade for all camera wall operations. Applications inject this service and delegate business logic to it.

**Responsibilities:**
- Manages camera layouts and current layout state
- Handles camera CRUD operations (Add, Edit, Delete)
- Coordinates connection state changes
- Provides status information (camera count, connected count)
- Delegates to other services for dialogs and settings

**Key Operations:**

| Operation | Description |
|-----------|-------------|
| `Initialize()` | Initializes manager with the camera grid control |
| `AddCamera()` | Opens dialog to add a new camera |
| `EditCamera()` | Opens dialog to edit existing camera |
| `DeleteCamera()` | Deletes camera after confirmation |
| `CreateNewLayout()` | Creates a new named layout |
| `SetCurrentAsStartup()` | Sets current layout as startup layout |
| `ReconnectAll()` | Reconnects all cameras in current layout |

---

### 💾 ICameraStorageService

Handles persistence of camera configurations and layouts to JSON files.

**Storage Location:** `%ProgramData%/Linksoft/CameraWall/cameras.json`

**How it works:**
1. On `Load()`, reads JSON file and deserializes cameras and layouts
2. Maintains in-memory collections for fast access
3. On `Save()`, serializes all data back to JSON

**Key Operations:**

| Operation | Description |
|-----------|-------------|
| `GetAllCameras()` | Returns all camera configurations |
| `GetCameraById()` | Retrieves specific camera by GUID |
| `AddOrUpdateCamera()` | Creates or updates camera config |
| `DeleteCamera()` | Removes camera from storage |
| `GetAllLayouts()` | Returns all layouts |
| `StartupLayoutId` | Gets/sets the startup layout identifier |

---

### ⚙️ IApplicationSettingsService

Manages application settings with support for per-camera overrides.

**Storage Location:** `%ProgramData%/Linksoft/CameraWall/settings.json`

**Settings Categories:**

| Category | Description |
|----------|-------------|
| `General` | Theme, language, startup behavior |
| `CameraDisplay` | Overlay settings, drag-and-drop, auto-save |
| `Connection` | Connection timeout, retry settings |
| `Performance` | Video quality, hardware acceleration |
| `Recording` | Recording path, format, motion settings |
| `Advanced` | Debug logging, log path |

**Override System:**
Cameras can override app-level defaults. Use `GetEffectiveValue<T>()` to get the resolved value:
```csharp
var autoRecord = settingsService.GetEffectiveValue(
    camera,
    settings.Recording.EnableRecordingOnConnect,
    o => o?.AutoRecordOnConnect);
```

---

### ⏺️ IRecordingService

Manages camera recording sessions with support for manual and motion-triggered recordings.

**Recording Types:**
- **Manual** - User-initiated via context menu
- **Motion** - Triggered by motion detection with auto-stop after cooldown

**How it works:**
1. `StartRecording()` creates a `RecordingSession` and calls FlyleafLib's `StartRecording()`
2. Session tracks: camera ID, start time, file path, recording type
3. `StopRecording()` finalizes the file and cleans up session
4. Motion recordings auto-extend when `UpdateMotionTimestamp()` is called

**File Naming:** `{CameraName}_{yyyy-MM-dd_HH-mm-ss}.{format}`

**Key Operations:**

| Operation | Description |
|-----------|-------------|
| `StartRecording()` | Starts manual recording |
| `StopRecording()` | Stops and finalizes recording |
| `TriggerMotionRecording()` | Starts/extends motion recording |
| `SegmentRecording()` | Segments recording at boundaries |
| `GetActiveSessions()` | Returns all active recording sessions |

---

### ⏱️ IRecordingSegmentationService

Automatically segments long recordings at **clock-aligned interval boundaries**.

**How it works:**

Instead of segmenting based on recording start time, it uses time slots:
```
slot = (hour × 60 + minute) / intervalMinutes
```

**Example with 15-minute interval:**

| Start Time | Segment At |
|------------|------------|
| 15:07 | 15:15, 15:30, 15:45, 16:00 ✅ |
| ~~15:07~~ | ~~15:22, 15:37, 15:52~~ ❌ (old behavior) |

**Configuration:**
- `EnableHourlySegmentation` - Enable/disable segmentation
- `MaxRecordingDurationMinutes` - Interval between segments (e.g., 15, 30, 60)

**Events:**
- `RecordingSegmented` - Fired when a recording is segmented, includes previous/new file paths

---

### 🎯 IMotionDetectionService

Detects motion in camera video streams by comparing consecutive frames.

**How it works:**
1. Captures frames from FlyleafLib player at configurable intervals
2. Converts frames to grayscale and compares pixel differences
3. If difference exceeds threshold, fires `MotionDetected` event
4. Cooldown period prevents rapid-fire events

**Configuration (MotionDetectionSettings):**

| Setting | Default | Description |
|---------|---------|-------------|
| `Sensitivity` | 0.05 | Percentage of changed pixels to trigger (0.0-1.0) |
| `AnalysisIntervalMs` | 500 | Time between frame analyses |
| `CooldownMs` | 5000 | Minimum time between motion events |

**Key Operations:**

| Operation | Description |
|-----------|-------------|
| `StartDetection()` | Begins monitoring camera for motion |
| `StopDetection()` | Stops motion monitoring |
| `IsMotionDetected()` | Returns current motion state |

---

### 🖼️ IThumbnailGeneratorService

Generates 2x2 grid thumbnails from recording frames.

**How it works:**
1. When recording starts, `StartCapture()` begins frame capture
2. Captures 4 frames at 1-second intervals (0s, 1s, 2s, 3s)
3. On `StopCapture()`, combines frames into 2x2 grid PNG
4. Missing frames are filled with black

**Output:** `{VideoFileName}.png` (same directory as video)

---

### 🧹 IMediaCleanupService

Automatically removes old recordings and snapshots based on retention policy.

**Cleanup Schedules:**

| Schedule | Behavior |
|----------|----------|
| `Disabled` | No automatic cleanup |
| `OnStartup` | Cleans up when application starts |
| `Periodically` | Runs at configured interval (e.g., daily) |
| `OnStartupAndPeriodically` | Both startup and periodic |

**How it works:**
1. Scans recording and snapshot directories
2. Deletes files older than `MaxRecordingAgeDays` / `MaxSnapshotAgeDays`
3. Fires `CleanupCompleted` event with results

**Key Operations:**

| Operation | Description |
|-----------|-------------|
| `Initialize()` | Starts cleanup based on schedule |
| `RunCleanupAsync()` | Runs cleanup immediately |
| `StopService()` | Stops periodic timer |

---

### 🌐 IGitHubReleaseService

Checks GitHub releases for application updates.

**How it works:**
1. Queries GitHub API for latest release
2. Parses version from release tag
3. Compares with current application version

**Key Operations:**

| Operation | Description |
|-----------|-------------|
| `GetLatestVersionAsync()` | Returns latest version from GitHub |
| `GetLatestReleaseUrlAsync()` | Returns download URL for latest release |

---

### 💬 IDialogService

Abstraction for displaying dialogs, enabling testability and consistent UI.

**Dialogs:**

| Dialog | Description |
|--------|-------------|
| `ShowCameraConfigurationDialog()` | Add/Edit camera with network scanner |
| `ShowInputBox()` | Simple text input (e.g., layout names) |
| `ShowConfirmation()` | Yes/No confirmation |
| `ShowError()` / `ShowInfo()` | Message boxes |
| `ShowSettingsDialog()` | Application settings |
| `ShowFullScreenCamera()` | Full-screen camera view |
| `ShowRecordingsBrowserDialog()` | Browse recorded videos |
| `ShowAssignCameraDialog()` | Dual-list camera assignment |

---

## 🛠️ Technology Stack

| Component | Technology |
|-----------|------------|
| Framework | .NET 10.0-windows |
| UI Framework | WPF |
| Video Streaming | FlyleafLib (FFmpeg-based) |
| MVVM | Atc.XamlToolkit |
| Source Generators | Atc.SourceGenerators |
| UI Controls | Fluent.Ribbon, Atc.Wpf.Controls |
| Theming | Atc.Wpf.Theming |
| Logging | Serilog (file sink) |

## 📁 Solution Structure

```
Linksoft.CameraWall/
├── src/
│   ├── Linksoft.Wpf.CameraWall/          # 📦 Reusable WPF library (NuGet package)
│   │   ├── Converters/                    # BoolToOpacity, ConnectionStateToColor/Text
│   │   ├── Dialogs/                       # CameraConfigurationDialog, InputBox, SettingsDialog, etc.
│   │   ├── Enums/                         # CameraProtocol, ConnectionState, OverlayPosition, SwapDirection
│   │   ├── Events/                        # Connection, Position, Dialog, Recording events
│   │   ├── Extensions/                    # CameraProtocolExtensions
│   │   ├── Factories/                     # DropDownItemsFactory for centralized dropdown items
│   │   ├── Helpers/                       # GridLayoutHelper, CameraUriHelper, ApplicationPaths
│   │   ├── Messages/                      # CameraAdd/Remove/Swap messages
│   │   ├── Models/                        # CameraConfiguration, CameraLayout, Settings, RecordingSession
│   │   ├── Options/                       # CameraWallOptions
│   │   ├── Services/                      # All service interfaces and implementations
│   │   ├── UserControls/                  # CameraTile, CameraGrid, CameraOverlay
│   │   └── ViewModels/                    # Dialog ViewModels
│   │
│   └── Linksoft.Wpf.CameraWall.App/      # 🚀 Thin shell WPF application
│       ├── Configuration/                 # App-specific options
│       ├── MainWindow.xaml                # Fluent.Ribbon shell
│       └── MainWindowViewModel.cs         # Thin binding layer to library
│
├── docs/
│   ├── architecture.md                    # 📐 Technical architecture
│   └── roadmap.md                         # 🗺️ Feature roadmap
│
└── test/                                  # 🧪 Test projects
```

## 🚀 Getting Started

### Prerequisites

- .NET 10 SDK
- Windows 10/11

### Build and Run

```bash
dotnet build
dotnet run --project src/Linksoft.Wpf.CameraWall.App
```

### 📂 User Data

Application data is stored in:
```
%ProgramData%/Linksoft/CameraWall/
├── cameras.json      # 📷 Camera configurations and layouts
├── settings.json     # ⚙️ Application settings
├── logs/             # 📝 Debug log files (when enabled)
├── snapshots/        # 🖼️ Camera snapshots
└── recordings/       # 🎬 Camera recordings with thumbnails
```

## 📖 Usage

1. **➕ Add Camera** - Click "Add Camera" in the Cameras tab to open the configuration dialog
2. **🔍 Network Scanner** - Use the built-in scanner in the Add Camera dialog to discover cameras
3. **📐 Layouts** - Use the Layouts tab to create, delete, or set startup layouts
4. **📋 Context Menu** - Right-click any camera tile for actions (Edit, Delete, Full Screen, Swap, Snapshot, Recording, Reconnect)
5. **🖱️ Drag-and-Drop** - Drag camera tiles to reorder them in the grid
6. **🖥️ Full Screen** - Double-click a camera tile or use context menu for full screen view
7. **⚙️ Settings** - Click the Settings button in the View tab to configure appearance, recording, and motion detection
8. **🎬 Recordings** - Use the View tab to browse and playback recorded videos
9. **🔄 Check for Updates** - Use Help tab to check for new versions on GitHub

## 📄 License

MIT
