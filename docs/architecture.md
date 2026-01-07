# Linksoft.CameraWall Architecture

## Overview

Linksoft.CameraWall is a WPF-based camera wall application for displaying multiple RTSP/HTTP camera streams in a dynamic grid layout. The solution consists of two projects:

- **Linksoft.Wpf.CameraWall** - Reusable library (NuGet package) containing all core functionality
- **Linksoft.Wpf.CameraWall.App** - Thin shell WPF application with Ribbon UI

## Architecture Philosophy

The library is designed to be a complete, self-contained package that other applications can consume. The App project serves as a thin shell that:
- Provides the Ribbon UI (Fluent.Ribbon)
- Hosts the CameraWall control
- Displays the status bar
- Delegates all business logic to the library's `ICameraWallManager`

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
| DI Container | Microsoft.Extensions.DependencyInjection |
| Network | Atc.Network |

## Project Structure

### Linksoft.Wpf.CameraWall (Library)

```
src/Linksoft.Wpf.CameraWall/
├── CameraWallEngine.cs              # FlyleafLib initialization
├── Dialogs/
│   ├── CameraConfigurationDialog.xaml/.cs  # Add/Edit camera dialog
│   └── InputBox.xaml/.cs            # Simple text input dialog
├── Enums/
│   ├── CameraProtocol.cs            # Rtsp, Http, Https
│   ├── ConnectionState.cs           # Connected, Connecting, Error
│   ├── OverlayPosition.cs           # TopLeft, TopRight, BottomLeft, BottomRight
│   └── SwapDirection.cs             # Left, Right
├── Events/
│   ├── CameraConnectionChangedEventArgs.cs
│   ├── CameraPositionChangedEventArgs.cs
│   └── DialogClosedEventArgs.cs
├── Extensions/
│   └── CameraProtocolExtensions.cs  # Protocol-related extensions
├── Helpers/
│   ├── CameraUriHelper.cs           # Build RTSP/HTTP URIs
│   └── GridLayoutHelper.cs          # Auto-calculate rows/columns
├── Messages/
│   ├── CameraAddMessage.cs          # Add camera to wall
│   ├── CameraRemoveMessage.cs       # Remove camera from wall
│   └── CameraSwapMessage.cs         # Swap camera positions
├── Models/
│   ├── CameraConfiguration.cs       # Camera settings
│   ├── CameraLayout.cs              # Named layout
│   ├── CameraLayoutItem.cs          # Camera position in layout
│   └── CameraStorageData.cs         # Root persistence model
├── Options/
│   └── CameraWallOptions.cs         # Library configuration options
├── Services/
│   ├── ICameraStorageService.cs     # Storage abstraction
│   ├── CameraStorageService.cs      # JSON file implementation
│   ├── IDialogService.cs            # Dialog abstraction
│   ├── DialogService.cs             # Default dialog implementation
│   ├── ICameraWallManager.cs        # Main facade interface
│   └── CameraWallManager.cs         # Core business logic
├── UserControls/
│   ├── CameraTile.xaml/.cs          # Single camera with overlay
│   ├── CameraOverlay.xaml/.cs       # Title/status overlay
│   └── CameraWall.xaml/.cs          # Multi-camera grid control
└── ViewModels/
    └── CameraConfigurationDialogViewModel.cs  # Dialog ViewModel
```

### Linksoft.Wpf.CameraWall.App (Application)

```
src/Linksoft.Wpf.CameraWall.App/
├── App.xaml/.cs                     # DI host, startup, theme init
├── appsettings.json                 # Configuration
├── Configuration/
│   └── AppOptions.cs                # App-specific settings
├── MainWindow.xaml/.cs              # Fluent.Ribbon shell
└── MainWindowViewModel.cs           # Thin binding layer
```

## Key Services

### ICameraWallManager

The main facade that encapsulates all camera wall operations. Apps inject this service and delegate all business logic to it.

```csharp
public interface ICameraWallManager : INotifyPropertyChanged
{
    // Properties
    ObservableCollection<CameraLayout> Layouts { get; }
    CameraLayout? CurrentLayout { get; set; }
    CameraLayout? SelectedStartupLayout { get; }
    int CameraCount { get; }
    int ConnectedCount { get; }
    string StatusText { get; }
    UserControls.CameraWall? CameraWall { get; }

    // Initialization
    void Initialize(UserControls.CameraWall cameraWallControl);

    // Camera operations
    void AddCamera();
    void EditCamera(CameraConfiguration camera);
    void DeleteCamera(CameraConfiguration camera);
    void ShowFullScreen(CameraConfiguration camera);
    void RefreshAll();

    // Layout operations
    void CreateNewLayout();
    void DeleteCurrentLayout();
    void SetCurrentAsStartup();
    void SaveCurrentLayout();

    // Event handlers
    void OnConnectionStateChanged(CameraConnectionChangedEventArgs e);
    void OnPositionChanged(CameraPositionChangedEventArgs e);

    // CanExecute properties for commands
    bool CanCreateNewLayout { get; }
    bool CanDeleteCurrentLayout { get; }
    bool CanSetCurrentAsStartup { get; }
    bool CanRefreshAll { get; }
}
```

### IDialogService

Abstraction for showing dialogs, allowing apps to customize dialog behavior.

```csharp
public interface IDialogService
{
    CameraConfiguration? ShowCameraConfigurationDialog(CameraConfiguration? camera, bool isNew);
    string? ShowInputBox(string title, string prompt, string defaultText = "");
    bool ShowConfirmation(string message, string title);
    void ShowError(string message, string title = "Error");
    void ShowInfo(string message, string title = "Information");
}
```

### ICameraStorageService

Abstraction for camera and layout persistence.

```csharp
public interface ICameraStorageService
{
    List<CameraConfiguration> GetAllCameras();
    List<CameraLayout> GetAllLayouts();
    void AddOrUpdateCamera(CameraConfiguration camera);
    void DeleteCamera(Guid id);
    void AddOrUpdateLayout(CameraLayout layout);
    void DeleteLayout(Guid id);
    Guid? StartupLayoutId { get; set; }
}
```

## Data Flow

```
┌─────────────────────────────────────────────────────────────┐
│            MainWindowViewModel (Thin Shell)                  │
│  - Exposes manager properties for binding                    │
│  - Delegates commands to ICameraWallManager                  │
│  - Only handles window-specific operations (theme, fullscreen)│
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│               ICameraWallManager (Library)                   │
│  - Manages layouts and cameras                               │
│  - Uses IDialogService for dialogs                           │
│  - Uses ICameraStorageService for persistence                │
│  - Sends messages via Atc.XamlToolkit Messenger              │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                   CameraWall Control                         │
│  - Receives messages via Atc.XamlToolkit Messenger           │
│  - Creates CameraTile for each camera                        │
│  - Manages grid layout                                       │
│  - Handles drag-and-drop reordering                          │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                   CameraTile Control                         │
│  - Constructs camera URI                                     │
│  - Creates FlyleafLib Player                                 │
│  - Auto-starts streaming when Camera property is set         │
│  - Handles connection/reconnection                           │
│  - Displays overlay                                          │
│  - Provides context menu actions                             │
└─────────────────────────────────────────────────────────────┘
```

## Dependency Injection

### Library Services Registration

Library services are auto-registered using the source-generated extension method:

```csharp
// Auto-registers all services marked with [Registration] attribute
services.AddDependencyRegistrationsFromCameraWall();
```

For transitive registration of referenced assemblies:
```csharp
services.AddDependencyRegistrationsFromCameraWall(includeReferencedAssemblies: true);
```

### App Services Registration

```csharp
services.AddSingleton<MainWindowViewModel>();
services.AddSingleton<MainWindow>();
```

## UI Architecture

### Ribbon Menu (Fluent.Ribbon)

```
┌─────────────────────────────────────────────────────────────┐
│  Layouts Tab                                                 │
│    [Layout ComboBox] [New Layout] [Delete Layout]           │
│    [Set as Startup]                                          │
├─────────────────────────────────────────────────────────────┤
│  Cameras Tab                                                 │
│    [Add Camera] [Refresh All]                               │
├─────────────────────────────────────────────────────────────┤
│  View Tab                                                    │
│    [Theme Dropdown] [Full Screen]                           │
└─────────────────────────────────────────────────────────────┘
```

### Context Menu (CameraTile)

- Edit Camera... → Opens CameraConfigurationDialog
- Delete Camera → Confirmation then removes
- Show in Full Screen
- Swap Left / Swap Right
- Take Snapshot
- Reconnect

## Messaging Pattern

Uses Atc.XamlToolkit Messenger for loose coupling:

| Message | Purpose |
|---------|---------|
| CameraAddMessage | Add camera to wall |
| CameraRemoveMessage | Remove camera from wall |
| CameraSwapMessage | Swap two camera positions |

## Configuration

### appsettings.json

```json
{
  "App": {
    "ApplicationUi": {
      "ThemeBase": "Dark",
      "ThemeAccent": "Blue",
      "Language": "en-US"
    }
  }
}
```

### CameraWallOptions (Library)

Uses `[OptionsBinding]` for configuration binding with validation:

```csharp
[OptionsBinding("CameraWall", ValidateDataAnnotations = true, ValidateOnStart = true)]
public partial class CameraWallOptions
{
    [Required]
    public string ThemeBase { get; set; } = "Dark";

    [Required]
    public string ThemeAccent { get; set; } = "Blue";

    [Required]
    public string Language { get; set; } = "en-US";
}
```

### User Data Storage

- Location: `%AppData%/Linksoft/CameraWall/`
- Files:
  - `cameras.json` - Camera configurations and layouts

## Dependencies

### Library (Linksoft.Wpf.CameraWall)

- FlyleafLib - Video playback engine
- FlyleafLib.Controls.WPF - WPF video controls
- Atc - Core utilities
- Atc.Network - Network operations
- Atc.SourceGenerators - Source generators for DI registration, options binding
- Atc.Wpf - WPF utilities
- Atc.Wpf.Controls - Custom controls
- Atc.Wpf.NetworkControls - Network scanner control
- Atc.Wpf.Theming - Theme management
- Atc.XamlToolkit - MVVM infrastructure
- Atc.XamlToolkit.Wpf - WPF MVVM utilities
- Microsoft.Extensions.Options.DataAnnotations - Options validation

### Application (Linksoft.Wpf.CameraWall.App)

- Fluent.Ribbon - Ribbon UI
- Microsoft.Extensions.Hosting - DI and hosting
- Microsoft.Extensions.Configuration.Json - Configuration

## Source Generators (Atc.SourceGenerators)

### Service Registration

Services annotated with `[Registration]` are auto-registered via source-generated extension methods:

```csharp
[Registration(Lifetime.Singleton)]
public class CameraStorageService : ICameraStorageService { }

[Registration(Lifetime.Singleton)]
public class DialogService : IDialogService { }

[Registration(Lifetime.Singleton)]
public class CameraWallManager : ObservableObject, ICameraWallManager { }
```

The generator creates `AddDependencyRegistrationsFrom{AssemblySuffix}()` extension methods based on the assembly name. For `Linksoft.Wpf.CameraWall`, this generates `AddDependencyRegistrationsFromCameraWall()`.

**Lifetime Options:**
- `Lifetime.Singleton` - Single instance for entire application
- `Lifetime.Scoped` - New instance per scope
- `Lifetime.Transient` - New instance every time

**Advanced Options:**
- `As` - Register against a specific interface
- `AsSelf` - Also register the concrete type
- `TryAdd` - Only register if not already present (for library authors)
- `Factory` - Use a static factory method
- `Condition` - Conditional registration based on configuration

### Options Binding

Configuration classes use `[OptionsBinding]` for automatic binding with validation:

```csharp
[OptionsBinding("CameraWall", ValidateDataAnnotations = true, ValidateOnStart = true)]
public partial class CameraWallOptions { }
```

### Model Validation

Models use DataAnnotations for validation metadata:

```csharp
public partial class CameraConfiguration : ObservableObject
{
    [ObservableProperty]
    [Required(ErrorMessage = "IP Address is required")]
    private string ipAddress = string.Empty;

    [ObservableProperty]
    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
    private int port = 554;
}
```

## Extension Points

### Custom Storage

Implement `ICameraStorageService` for custom persistence (database, cloud, etc.).

### Custom Dialogs

Implement `IDialogService` to customize how dialogs are displayed.

### Custom Overlays

Style the `CameraOverlay` control via WPF resource dictionaries.

### Theming

Use Atc.Wpf.Theming for Dark/Light theme switching with custom accent colors.

## Creating a New App

To create a new application using the library:

1. Reference the `Linksoft.Wpf.CameraWall` library
2. Register the library services using the source-generated extension method:
   ```csharp
   services.AddDependencyRegistrationsFromCameraWall();
   ```
3. Create a ViewModel that injects `ICameraWallManager`
4. Create a View that hosts the `CameraWall` control
5. Call `manager.Initialize(cameraWallControl)` when the view loads
6. Bind UI commands to manager methods
