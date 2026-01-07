# Linksoft.CameraWall

A professional WPF camera wall application for live monitoring of multiple RTSP/HTTP camera streams with an intuitive ribbon interface and dynamic grid layout.

## Features

- **Dynamic Grid Layout** - Auto-calculates optimal grid based on camera count
- **Camera Tile Overlay** - Configurable overlay showing camera name and connection status
- **Context Menu** - Edit, Delete, Full Screen, Swap Left/Right, Snapshot, Reconnect
- **Drag-and-Drop** - Reorder cameras by dragging tiles within the grid
- **Layout Management** - Create, save, and switch between named layouts
- **Startup Layout** - Designate a layout to load on application start
- **Network Scanner** - Auto-discover cameras on local network (integrated in Add Camera dialog)
- **Fluent Ribbon UI** - Modern ribbon interface with Layouts, Cameras, and View tabs
- **Theme Support** - Dark/Light theme switching

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
│   │   ├── Dialogs/                       # CameraConfigurationDialog, InputBox
│   │   ├── Events/                        # Connection, Position, Dialog events
│   │   ├── Helpers/                       # GridLayoutHelper, CameraUriHelper
│   │   ├── Models/                        # CameraConfiguration, CameraLayout
│   │   ├── Options/                       # CameraWallOptions
│   │   ├── Services/                      # ICameraWallManager, IDialogService, ICameraStorageService
│   │   ├── UserControls/                  # CameraTile, CameraWall, CameraOverlay
│   │   └── ViewModels/                    # CameraConfigurationDialogViewModel
│   │
│   └── Linksoft.Wpf.CameraWall.App/      # Thin shell WPF application
│       ├── Configuration/                 # App-specific options
│       ├── MainWindow.xaml                # Fluent.Ribbon shell
│       └── MainWindowViewModel.cs         # Thin binding layer to library
│
└── docs/
    ├── architecture.md                    # Technical architecture
    └── roadmap.md                         # Feature roadmap
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
4. **Context Menu** - Right-click any camera tile for actions (Edit, Delete, Full Screen, etc.)
5. **Drag-and-Drop** - Drag camera tiles to reorder them in the grid

## License

MIT
