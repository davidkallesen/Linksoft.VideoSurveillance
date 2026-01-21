# Linksoft.CameraWall

A professional WPF camera wall application for live monitoring of multiple RTSP/HTTP camera streams with an intuitive ribbon interface and dynamic grid layout.

## Features

### Camera Display
- **Dynamic Grid Layout** - Auto-calculates optimal grid based on camera count (optimized for 4:3 aspect ratio)
- **Camera Tile Overlay** - Configurable overlay showing title, description, timestamp, and connection status
- **Overlay Configuration** - Choose corner position (TopLeft, TopRight, BottomLeft, BottomRight) and opacity
- **Full Screen Mode** - View any camera in full screen with Escape to exit
- **Connection State Indicators** - Visual indicators for Connected, Connecting, and Error states

### Camera Management
- **Context Menu** - Edit, Delete, Full Screen, Swap Left/Right, Snapshot, Reconnect
- **Drag-and-Drop** - Reorder cameras by dragging tiles within the grid
- **Network Scanner** - Auto-discover cameras on local network (integrated in Add Camera dialog)
- **Test Connection** - Validate camera connectivity before saving (TCP for RTSP, HTTP GET for HTTP/HTTPS)
- **Multiple Protocols** - RTSP, HTTP, and HTTPS support with configurable ports and paths

### Layout Management
- **Named Layouts** - Create, save, and switch between named layouts
- **Startup Layout** - Designate a layout to load on application start
- **Auto-Save** - Automatic persistence of camera positions (configurable)

### User Interface
- **Fluent Ribbon UI** - Modern ribbon interface with Layouts, Cameras, View, and Help tabs
- **Theme Support** - Dark/Light theme switching with configurable accent colors
- **Multi-Language** - Localization support via LCID-based language selection
- **Settings Dialog** - Configure general and display settings
- **About Dialog** - Application version and information
- **Check for Updates** - GitHub-based update checking

## Technology Stack

| Component | Technology |
|-----------|------------|
| Framework | .NET 10.0-windows |
| UI Framework | WPF |
| Video Streaming | FlyleafLib (FFmpeg-based) |
| MVVM | Atc.XamlToolkit |
| Source Generators | Atc.SourceGenerators |
| UI Controls | Fluent.Ribbon, Atc.Wpf.Controls |
| Theming | Atc.Wpf.Theming |

## Solution Structure

```
Linksoft.CameraWall/
├── src/
│   ├── Linksoft.Wpf.CameraWall/          # Reusable WPF library (NuGet package)
│   │   ├── Converters/                    # BoolToOpacity, ConnectionStateToColor/Text
│   │   ├── Dialogs/                       # CameraConfigurationDialog, InputBox, SettingsDialog, etc.
│   │   ├── Enums/                         # CameraProtocol, ConnectionState, OverlayPosition, SwapDirection
│   │   ├── Events/                        # Connection, Position, Dialog events
│   │   ├── Extensions/                    # CameraProtocolExtensions
│   │   ├── Factories/                     # DropDownItemsFactory for centralized dropdown items
│   │   ├── Helpers/                       # GridLayoutHelper, CameraUriHelper, AppHelper
│   │   ├── Messages/                      # CameraAdd/Remove/Swap messages
│   │   ├── Models/                        # CameraConfiguration, CameraLayout, GeneralSettings, DisplaySettings
│   │   ├── Options/                       # CameraWallOptions
│   │   ├── Services/                      # ICameraWallManager, IDialogService, ICameraStorageService, IApplicationSettingsService
│   │   ├── UserControls/                  # CameraTile, CameraWall, CameraOverlay
│   │   └── ViewModels/                    # Dialog ViewModels
│   │
│   └── Linksoft.Wpf.CameraWall.App/      # Thin shell WPF application
│       ├── Configuration/                 # App-specific options
│       ├── MainWindow.xaml                # Fluent.Ribbon shell
│       └── MainWindowViewModel.cs         # Thin binding layer to library
│
├── docs/
│   ├── architecture.md                    # Technical architecture
│   └── roadmap.md                         # Feature roadmap
│
└── test/                                  # Test projects
```

## Getting Started

### Prerequisites

- .NET 10 SDK
- Windows 10/11

### Build and Run

```bash
dotnet build
dotnet run --project src/Linksoft.Wpf.CameraWall.App
```

### User Data

Camera configurations and layouts are stored in:
```
%AppData%/Linksoft/CameraWall/cameras.json
```

## Usage

1. **Add Camera** - Click "Add Camera" in the Cameras tab to open the configuration dialog
2. **Network Scanner** - Use the built-in scanner in the Add Camera dialog to discover cameras
3. **Layouts** - Use the Layouts tab to create, delete, or set startup layouts
4. **Context Menu** - Right-click any camera tile for actions (Edit, Delete, Full Screen, Swap, Snapshot, Reconnect)
5. **Drag-and-Drop** - Drag camera tiles to reorder them in the grid
6. **Full Screen** - Double-click a camera tile or use context menu for full screen view
7. **Settings** - Click the Settings button in the View tab to configure appearance and behavior
8. **Check for Updates** - Use Help tab to check for new versions on GitHub

## License

MIT
