# Linksoft.CameraWall Architecture

## Overview

Linksoft.CameraWall is a multi-assembly video surveillance platform. A shared Core library provides models, service interfaces, and business logic that powers both a WPF desktop application and a headless REST API + SignalR server. An Aspire AppHost orchestrates the server-side components.

### Projects

| Project | Framework | Role |
|---------|-----------|------|
| `Linksoft.VideoSurveillance.Core` | net10.0 | Shared models, enums, events, service interfaces, helpers |
| `Linksoft.Wpf.CameraWall` | net10.0-windows | Reusable WPF library (NuGet) with UI, dialogs, FlyleafLib |
| `Linksoft.Wpf.CameraWall.App` | net10.0-windows | Thin WPF shell with Fluent.Ribbon |
| `Linksoft.VideoSurveillance.Api.Contracts` | net10.0 | Generated API models, endpoints, handler interfaces |
| `Linksoft.VideoSurveillance.Api.Domain` | net10.0 | Handler implementations calling Core services |
| `Linksoft.VideoSurveillance.Api` | net10.0 | ASP.NET Core host with SignalR hub |
| `Linksoft.VideoSurveillance.Aspire` | net10.0 | Aspire AppHost for orchestrated startup |
| `Linksoft.VideoSurveillance.Core.Tests` | net10.0 | xUnit v3 tests for Core library |

### Dependency Graph

```
Linksoft.VideoSurveillance.Core                    (net10.0, no WPF)
    ^                ^               ^
    |                |               |
    |     Api.Contracts -----> Api.Domain
    |           ^                    ^
    |           |                    |
    |     Linksoft.VideoSurveillance.Api (host)
    |           ^
    |           |
    |     Linksoft.VideoSurveillance.Aspire (orchestration)
    |
Linksoft.Wpf.CameraWall
(net10.0-windows, WPF)
    ^
    |
Linksoft.Wpf.CameraWall.App
(net10.0-windows, WPF shell)
```

## Architecture Philosophy

- **Core** contains zero UI dependencies. All shared models, enums, events, service interfaces, and helpers live here. Both the WPF app and the API server reference Core.
- **WPF library** provides the complete desktop experience: camera grid, dialogs, settings, FlyleafLib video playback. Apps inject `ICameraWallManager` and delegate all business logic.
- **WPF App** is a thin shell providing the Ribbon UI, status bar, and theme initialization.
- **API** is OpenAPI-first: a YAML spec defines all endpoints, and `Atc.Rest.Api.SourceGenerator` generates the contracts. Handler implementations inject Core services.
- **Aspire** orchestrates the API (and future Blazor UI) with a developer dashboard providing logs, traces, and metrics.

## Technology Stack

| Component | Technology |
|-----------|------------|
| Framework | .NET 10.0 / .NET 10.0-windows |
| Desktop UI | WPF with Fluent.Ribbon, Atc.Wpf.Controls |
| Server | ASP.NET Core, SignalR |
| API Definition | OpenAPI 3.0 (atc-rest-api-source-generator) |
| Orchestration | .NET Aspire v13 |
| Video (Desktop) | FlyleafLib (FFmpeg-based) |
| Video (Server) | FFmpeg subprocess |
| MVVM | Atc.XamlToolkit |
| Source Generators | Atc.SourceGenerators (`[Registration]`, `[MapTo]`, `[OptionsBinding]`) |
| DI Container | Microsoft.Extensions.DependencyInjection |
| Theming | Atc.Wpf.Theming |
| Logging | Serilog (file sink) |
| Network | Atc.Network |

## Core Library (Linksoft.VideoSurveillance.Core)

The Core library contains everything that is shared between the WPF desktop app and the server API.

### Contents

- **Enums**: `CameraProtocol`, `ConnectionState`, `RecordingState`, `OverlayPosition`, `MediaCleanupSchedule`, `SwapDirection`
- **Models**: `CameraConfiguration`, `CameraLayout`, `CameraLayoutItem`, `RecordingEntry`, `RecordingSession`, `BoundingBox`, `SmoothedBox`, `MediaCleanupResult`
- **Settings Models**: `ApplicationSettings`, `GeneralSettings`, `CameraDisplayAppSettings`, `ConnectionAppSettings`, `PerformanceSettings`, `RecordingSettings`, `MotionDetectionSettings`, `AdvancedSettings`, etc.
- **Override Models**: `CameraOverrides`, `ConnectionOverrides`, `CameraDisplayOverrides`, `PerformanceOverrides`, `RecordingOverrides`, `MotionDetectionOverrides`, `BoundingBoxOverrides`
- **Events**: `CameraConnectionChangedEventArgs`, `CameraPositionChangedEventArgs`, `RecordingStateChangedEventArgs`, `MotionDetectedEventArgs`, `RecordingSegmentedEventArgs`, `ConnectionStateChangedEventArgs`, etc.
- **Service Interfaces**: `ICameraStorageService`, `IApplicationSettingsService`, `IRecordingService`, `IMotionDetectionService`, `ITimelapseService`, `IThumbnailGeneratorService`, `IMediaCleanupService`, `IGitHubReleaseService`, `IRecordingSegmentationService`
- **Media Abstraction**: `IMediaPipeline`, `IMediaPipelineFactory` -- WPF uses FlyleafLib implementation, server uses FFmpeg subprocess
- **Helpers**: `ApplicationPaths`, `CameraUriHelper`
- **Factories**: `DropDownItemsFactory`

## WPF Library (Linksoft.Wpf.CameraWall)

Most service interfaces and event types are defined in Core and aliased into the WPF namespace via `global using` directives in `GlobalUsings.cs`. This includes `IMotionDetectionService`, `IRecordingSegmentationService`, `IGitHubReleaseService`, `IMediaPipeline`, `MotionDetectedEventArgs`, `RecordingStateChangedEventArgs`, and others. Only UI-specific interfaces remain defined in the WPF library.

### Key Services

| Service | Description |
|---------|-------------|
| `ICameraWallManager` | Main facade for all camera wall operations |
| `IDialogService` | Dialog abstraction for camera config, settings, input, confirmations |
| `IToastNotificationService` | WPF toast popup notifications |

### UI Components

- **UserControls**: `CameraTile`, `CameraGrid`, `CameraOverlay`, `MotionBoundingBoxOverlay`
- **Dialogs**: `CameraConfigurationDialog`, `SettingsDialog`, `InputBox`, `RecordingsBrowserDialog`, `CheckForUpdatesDialog`, `AssignCameraDialog`
- **Windows**: `FullScreenCameraWindow`, `FullScreenRecordingWindow`
- **Dialog Parts**: 11 camera configuration parts, 19 settings parts

### Media Pipeline

The WPF library implements `IMediaPipeline` via `FlyleafLibMediaPipeline`, wrapping FlyleafLib's `Player` for video playback, recording, and frame capture. `FlyleafLibMediaPipelineFactory` creates configured pipeline instances.

## API Architecture (Linksoft.VideoSurveillance.Api)

### OpenAPI-First Design

The API is defined in `src/VideoSurveillance.yaml` (OpenAPI 3.0.3). The `Atc.Rest.Api.SourceGenerator` generates at build time:

- **Contracts project**: Model records, parameter classes, handler interfaces, typed result classes, `MapEndpoints()` extension, `AddApiHandlersFromDomain()` DI extension
- **Domain project**: Contains handler implementations (16 handlers) that inject Core services

### REST Endpoints

```
GET    /api/cameras                    # List all cameras
POST   /api/cameras                    # Add camera
GET    /api/cameras/{id}               # Get camera by ID
PUT    /api/cameras/{id}               # Update camera
DELETE /api/cameras/{id}               # Delete camera
POST   /api/cameras/{id}/snapshot      # Capture snapshot
POST   /api/cameras/{id}/recording/start  # Start recording
POST   /api/cameras/{id}/recording/stop   # Stop recording

GET    /api/layouts                    # List layouts
POST   /api/layouts                    # Create layout
PUT    /api/layouts/{id}               # Update layout
DELETE /api/layouts/{id}               # Delete layout
POST   /api/layouts/{id}/apply         # Apply layout

GET    /api/recordings                 # List recordings

GET    /api/settings                   # Get settings
PUT    /api/settings                   # Update settings
```

### SignalR Hub

`/hubs/surveillance` provides real-time events:

**Server -> Client**: `ConnectionStateChanged`, `MotionDetected`, `RecordingStateChanged`, `CameraAdded`, `CameraRemoved`, `LayoutApplied`

**Client -> Server**: `StartRecording`, `StopRecording`, `SwapCameras`, `StartStream`, `StopStream`

### Server Services

- `FFmpegMediaPipeline` -- implements `IMediaPipeline` using FFmpeg subprocess
- `FFmpegMediaPipelineFactory` -- creates configured FFmpeg pipelines
- `StreamingService` -- manages per-camera RTSP to HLS transcoding with viewer ref-counting
- `SurveillanceEventBroadcaster` -- `IHostedService` that subscribes to recording/motion events and broadcasts via SignalR

## Aspire Orchestration

The `Linksoft.VideoSurveillance.Aspire` project uses the Aspire AppHost SDK v13.1.0 to orchestrate the API server. Running the Aspire project starts all referenced services and provides a developer dashboard with:

- Resource monitoring (CPU, memory)
- Structured logs from all services
- Distributed traces
- Environment and endpoint information

```bash
dotnet run --project src/Linksoft.VideoSurveillance.Aspire
```

## WPF Application (Linksoft.Wpf.CameraWall.App)

### Data Flow

```
┌─────────────────────────────────────────────────────────────┐
│            MainWindowViewModel (Thin Shell)                  │
│  - Exposes manager properties for binding                   │
│  - Delegates commands to ICameraWallManager                 │
│  - Only handles window-specific operations (theme, fullscreen)│
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│               ICameraWallManager (Library)                   │
│  - Manages layouts and cameras                              │
│  - Uses IDialogService for dialogs                          │
│  - Uses ICameraStorageService for persistence               │
│  - Sends messages via Atc.XamlToolkit Messenger             │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                   CameraGrid Control                        │
│  - Receives messages via Atc.XamlToolkit Messenger          │
│  - Creates CameraTile for each camera                       │
│  - Manages grid layout                                      │
│  - Handles drag-and-drop reordering                         │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                   CameraTile Control                        │
│  - Creates FlyleafLibMediaPipeline per camera               │
│  - Auto-starts streaming when Camera property is set        │
│  - Handles connection/reconnection                          │
│  - Displays overlay and bounding boxes                      │
│  - Provides context menu actions                            │
└─────────────────────────────────────────────────────────────┘
```

### Ribbon Menu (Fluent.Ribbon)

```
┌─────────────────────────────────────────────────────────────┐
│  Layouts Tab                                                │
│    [Layout ComboBox] [New Layout] [Delete Layout]           │
│    [Set as Startup]                                         │
├─────────────────────────────────────────────────────────────┤
│  Cameras Tab                                                │
│    [Add Camera] [Refresh All]                               │
├─────────────────────────────────────────────────────────────┤
│  View Tab                                                   │
│    [Settings] [Recordings]                                  │
├─────────────────────────────────────────────────────────────┤
│  Help Tab                                                   │
│    [About] [Check for Updates]                              │
└─────────────────────────────────────────────────────────────┘
```

## Dependency Injection

### Library Services Registration

```csharp
// Auto-registers all services marked with [Registration] attribute
services.AddDependencyRegistrationsFromCameraWall(includeReferencedAssemblies: true);
```

### API Services Registration

```csharp
builder.Services.AddApiHandlersFromDomain();  // Generated handler DI
builder.Services.AddSignalR();
```

## Messaging Pattern (WPF)

Uses Atc.XamlToolkit Messenger for loose coupling:

| Message | Purpose |
|---------|---------|
| CameraAddMessage | Add camera to wall |
| CameraRemoveMessage | Remove camera from wall |
| CameraSwapMessage | Swap two camera positions |

## Configuration

### User Data Storage

- Location: `%ProgramData%\Linksoft\CameraWall\` (via `ApplicationPaths` helper)
- Files:
  - `cameras.json` - Camera configurations and layouts
  - `settings.json` - Application settings (all sections)
- Directories:
  - `logs/` - Debug log files (when enabled)
  - `snapshots/` - Camera snapshots and timelapse frames
  - `recordings/` - Camera recordings with thumbnails

## Source Generators

### Atc.SourceGenerators

- `[Registration]` -- auto-registers services via generated DI extension methods
- `[OptionsBinding]` -- binds configuration sections with validation
- `[MapTo]` -- generates compile-time model mapping between Core POCOs and API models

### Atc.Rest.Api.SourceGenerator

Generates from `VideoSurveillance.yaml` at build time:
- Minimal API endpoints with `MapEndpoints()`
- Handler interfaces with typed parameters and results
- Model records with validation attributes
- DI registration with `AddApiHandlersFromDomain()`

## Extension Points

### Custom Storage
Implement `ICameraStorageService` for custom persistence (database, cloud, etc.).

### Custom Dialogs
Implement `IDialogService` to customize how WPF dialogs are displayed.

### Custom Media Pipeline
Implement `IMediaPipeline` and `IMediaPipelineFactory` for alternative video backends.