# Linksoft.CameraWall - Claude Code Guidelines

## Project Overview
A WPF application for displaying multiple camera streams in a configurable grid layout. Uses RTSP/HTTP protocols with FlyleafLib for video playback.

## Solution Structure
- `Linksoft.Wpf.CameraWall` - Reusable WPF library (NuGet package) with all core functionality
- `Linksoft.Wpf.CameraWall.App` - Thin shell WPF application using the library

## Architecture
The library contains all business logic. The App is a thin shell that:
- Provides Ribbon UI (Fluent.Ribbon)
- Hosts the CameraGrid control
- Delegates all operations to `ICameraWallManager`

## Key Frameworks & Packages
- **Atc.XamlToolkit** - MVVM source generators (`[ObservableProperty]`, `[RelayCommand]`, `[DependencyProperty]`)
- **Atc.SourceGenerators** - Source generators for DI registration (`[Registration]`), options binding (`[OptionsBinding]`)
- **Atc.Wpf.Controls** - UI controls (`LabelTextBox`, `LabelComboBox`, `LabelPasswordBox`, `GridEx`)
- **Atc.Wpf.NetworkControls** - `NetworkScannerView` for discovering cameras
- **FlyleafLib** - Video player for RTSP/HTTP streams
- **Fluent.Ribbon** - Ribbon UI (App only)
- **Serilog** - Structured logging with file sink (App only)

## Library Services

### ICameraWallManager
Main facade for all camera wall operations. Apps inject this and delegate business logic to it.
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
    Linksoft.Wpf.CameraWall.UserControls.CameraGrid? CameraGrid { get; }

    // Initialization
    void Initialize(Linksoft.Wpf.CameraWall.UserControls.CameraGrid cameraGridControl);

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

    // Dialog operations
    void ShowAboutDialog();
    void ShowCheckForUpdatesDialog();
    void ShowSettingsDialog();

    // CanExecute properties
    bool CanCreateNewLayout { get; }
    bool CanDeleteCurrentLayout { get; }
    bool CanSetCurrentAsStartup { get; }
    bool CanRefreshAll { get; }
}
```

### IDialogService
Abstraction for showing dialogs:
```csharp
public interface IDialogService
{
    CameraConfiguration? ShowCameraConfigurationDialog(CameraConfiguration? camera, bool isNew);
    string? ShowInputBox(string title, string prompt, string defaultText = "");
    bool ShowConfirmation(string message, string title);
    void ShowError(string message, string title = "Error");
    void ShowInfo(string message, string title = "Information");
    void ShowAboutDialog();
    void ShowCheckForUpdatesDialog();
    void ShowFullScreenCamera(CameraConfiguration camera);
    void ShowSettingsDialog();
}
```

### ICameraStorageService
Abstraction for camera/layout persistence (JSON file implementation provided):
```csharp
public interface ICameraStorageService
{
    List<CameraConfiguration> GetAllCameras();
    CameraConfiguration? GetCameraById(Guid id);
    List<CameraLayout> GetAllLayouts();
    CameraLayout? GetLayoutById(Guid id);
    void AddOrUpdateCamera(CameraConfiguration camera);
    void DeleteCamera(Guid id);
    void AddOrUpdateLayout(CameraLayout layout);
    void DeleteLayout(Guid id);
    Guid? StartupLayoutId { get; set; }
    void Load();
    void Save();
}
```

### IApplicationSettingsService
Abstraction for application settings (theme, language, display, recording options):
```csharp
public interface IApplicationSettingsService
{
    GeneralSettings General { get; }
    CameraDisplayAppSettings CameraDisplay { get; }
    ConnectionAppSettings Connection { get; }
    PerformanceSettings Performance { get; }
    RecordingSettings Recording { get; }
    AdvancedSettings Advanced { get; }
    void Load();
    void Save();
}
```

### IRecordingService
Service for managing camera recording sessions:
```csharp
public interface IRecordingService
{
    event EventHandler<RecordingStateChangedEventArgs>? RecordingStateChanged;
    RecordingState GetRecordingState(Guid cameraId);
    bool StartRecording(CameraConfiguration camera, Player player);
    void StopRecording(Guid cameraId);
    void StopAllRecordings();
    bool IsRecording(Guid cameraId);
    bool TriggerMotionRecording(CameraConfiguration camera, Player player);
}
```

### IGitHubReleaseService
Service for checking GitHub releases for updates.

## MVVM Patterns

### Observable Properties
Use `[ObservableProperty]` attribute on private fields:
```csharp
[ObservableProperty]
private string displayName = string.Empty;
```

With callbacks:
```csharp
[ObservableProperty(AfterChangedCallback = nameof(OnSelectedCameraChanged))]
private CameraConfiguration? selectedCamera;

private static void OnSelectedCameraChanged()
    => CommandManager.InvalidateRequerySuggested();
```

With dependent properties:
```csharp
[ObservableProperty(DependentPropertyNames = [nameof(ScanButtonText)])]
private bool isScanning;
```

### Dependency Properties
Use `[DependencyProperty]` attribute on private fields for UserControls:
```csharp
[DependencyProperty(PropertyChangedCallback = nameof(OnCameraChanged))]
private CameraConfiguration? camera;

[DependencyProperty(DefaultValue = true)]
private bool autoSave = true;
```

### Relay Commands
Use `[RelayCommand]` attribute on methods:
```csharp
[RelayCommand(CanExecute = nameof(CanSave))]
private void Save() { ... }

private bool CanSave() => !string.IsNullOrWhiteSpace(Camera.DisplayName);
```

For async commands without "Async" suffix in command name:
```csharp
[RelayCommand("TestConnection", CanExecute = nameof(CanTestConnection))]
private async Task TestConnectionAsync() { ... }
```

### Command CanExecute Invalidation
When a property affects command CanExecute, use `AfterChangedCallback`:
```csharp
[ObservableProperty(AfterChangedCallback = nameof(OnIsTestingChanged))]
private bool isTesting;

private static void OnIsTestingChanged()
    => CommandManager.InvalidateRequerySuggested();
```

## UI Controls (Atc.Wpf)
Use Atc controls instead of standard WPF controls:
- `atc:GridEx` instead of `Grid`
- `atc:LabelTextBox` for labeled text inputs
- `atc:LabelComboBox` for labeled dropdowns (uses `IDictionary<string, string>` Items and `SelectedKey`)
- `atc:LabelPasswordBox` for password inputs
- `atc:LabelIntegerBox` for numeric inputs

## Models
Models that need UI binding should inherit from `ObservableObject`:
```csharp
public partial class CameraConfiguration : ObservableObject
{
    [ObservableProperty]
    private string ipAddress = string.Empty;
}
```

## Enums
Use extension methods for enum conversions:
```csharp
public static string ToScheme(this CameraProtocol protocol)
    => protocol switch
    {
        CameraProtocol.Rtsp => CameraProtocol.Rtsp.ToStringLowerCase(),
        // ...
    };
```

## ViewModels
- Inherit from `ViewModelDialogBase` for dialog ViewModels
- Use `CloseRequested` event with `DialogClosedEventArgs` for dialog closing
- Subscribe to model `PropertyChanged` to react to changes

## Dialogs (in Library)
- `CameraConfigurationDialog` - Add/Edit camera with integrated network scanner
- `InputBox` - Simple text input dialog (for layout names)
- `SettingsDialog` - Configure general and display settings
- `CheckForUpdatesDialog` - Check for and display available updates from GitHub
- `FullScreenCameraWindow` - Display single camera in full screen mode

## Naming Conventions
- ViewModels: `{Name}ViewModel` (e.g., `CameraConfigurationDialogViewModel`)
- Dialogs: `{Name}Dialog` (e.g., `CameraConfigurationDialog`)
- Extensions: `{Type}Extensions` (e.g., `CameraProtocolExtensions`)
- UserControls: PascalCase (e.g., `CameraTile`, `CameraGrid`, `CameraOverlay`)
- Services: `I{Name}Service` / `{Name}Service` (e.g., `ICameraWallManager`, `CameraWallManager`)

## Dependency Injection
Library services are auto-registered using the source-generated extension method:
```csharp
// Auto-registers all services marked with [Registration] attribute
services.AddDependencyRegistrationsFromCameraWall();
```

For transitive registration of referenced assemblies:
```csharp
services.AddDependencyRegistrationsFromCameraWall(includeReferencedAssemblies: true);
```

## Atc.Source.Generators

### Service Registration Attribute
Services annotated with `[Registration]` are auto-registered via source-generated extension methods:
```csharp
[Registration(Lifetime.Singleton)]
public class CameraStorageService : ICameraStorageService { }

[Registration(Lifetime.Scoped)]
public class UserService : IUserService { }

[Registration(Lifetime.Transient)]
public class LoggerService { }
```

The generator creates `AddDependencyRegistrationsFrom{AssemblySuffix}()` extension methods. For `Linksoft.Wpf.CameraWall`, this generates `AddDependencyRegistrationsFromCameraWall()`.

Advanced options:
```csharp
// Register as specific interface
[Registration(As = typeof(INotificationService))]
public class EmailService : INotificationService, IEmailService { }

// Register as both interface and self
[Registration(AsSelf = true)]
public class CacheService : ICacheService { }

// TryAdd pattern (only register if not already present)
[Registration(TryAdd = true)]
public class DefaultLogger : ILogger { }
```

### Options Binding
Use `[OptionsBinding]` for configuration classes with validation:
```csharp
[OptionsBinding("CameraWall", ValidateDataAnnotations = true, ValidateOnStart = true)]
public partial class CameraWallOptions
{
    [Required]
    public string ThemeBase { get; set; } = "Dark";
}
```

### DataAnnotations on Models
Use DataAnnotations for validation metadata:
```csharp
public partial class CameraConfiguration : ObservableObject
{
    [ObservableProperty]
    [Required(ErrorMessage = "IP Address is required")]
    private string ipAddress = string.Empty;

    [ObservableProperty]
    [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
    private int port = 554;

    [ObservableProperty]
    [Required(ErrorMessage = "Display Name is required")]
    [StringLength(256, ErrorMessage = "Display Name cannot exceed 256 characters")]
    private string displayName = string.Empty;
}
```

## Settings Models

### GeneralSettings
```csharp
public class GeneralSettings
{
    public string ThemeBase { get; set; } = "Dark";           // "Dark" or "Light"
    public string ThemeAccent { get; set; } = "Blue";         // Accent color name
    public string Language { get; set; } = "1033";            // LCID as string (1033 = en-US)
    public bool ConnectCamerasOnStartup { get; set; } = true;
    public bool StartMaximized { get; set; } = false;
    public bool StartRibbonCollapsed { get; set; } = false;
}
```

### CameraDisplayAppSettings
```csharp
public class CameraDisplayAppSettings
{
    public bool ShowOverlayTitle { get; set; } = true;
    public bool ShowOverlayDescription { get; set; } = true;
    public bool ShowOverlayTime { get; set; } = false;
    public bool ShowOverlayConnectionStatus { get; set; } = true;
    public double OverlayOpacity { get; set; } = 0.7;
    public OverlayPosition OverlayPosition { get; set; } = OverlayPosition.TopLeft;
    public bool AllowDragAndDropReorder { get; set; } = true;
    public bool AutoSaveLayoutChanges { get; set; } = true;
    public string SnapshotPath { get; set; } = ApplicationPaths.DefaultSnapshotsPath;
}
```

### RecordingSettings
```csharp
public class RecordingSettings
{
    public string RecordingPath { get; set; } = ApplicationPaths.DefaultRecordingsPath;
    public string RecordingFormat { get; set; } = "mp4";
    public bool EnableRecordingOnMotion { get; set; }
    public bool EnableRecordingOnConnect { get; set; }
    public MotionDetectionSettings MotionDetection { get; set; } = new();
}
```

### AdvancedSettings
```csharp
public class AdvancedSettings
{
    public bool EnableDebugLogging { get; set; }
    public string LogPath { get; set; } = ApplicationPaths.DefaultLogsPath;
}
```

## Threading
Use `SemaphoreSlim` for thread-safe operations with proper acquisition tracking:
```csharp
var acquired = false;
try
{
    semaphore.Wait();
    acquired = true;
    // work...
}
finally
{
    if (acquired)
    {
        semaphore.Release();
    }
}
```

## Converters
- `BoolToOpacityConverter` - Converts bool to opacity value (0.0-1.0)
- `ConnectionStateToColorConverter` - Converts ConnectionState enum to brush color
- `ConnectionStateToTextConverter` - Converts ConnectionState enum to localized text
- `OverrideOrDefaultMultiValueConverter` - Returns override value if non-null, otherwise default value

## Factories
Use `DropDownItemsFactory` for centralized dropdown items and defaults:
```csharp
// Dropdown items for comboboxes
public static IDictionary<string, string> VideoQualityItems { get; }
public static IDictionary<string, string> RtspTransportItems { get; }
public static IDictionary<string, string> RecordingFormatItems { get; }
public static IDictionary<string, string> OverlayPositionItems { get; }
public static IDictionary<string, string> OverlayOpacityItems { get; }  // 0%-100% in 10% increments
public static IDictionary<string, string> ProtocolItems { get; }

// Default values
public const string DefaultVideoQuality = "Auto";
public const string DefaultRtspTransport = "tcp";
public const string DefaultRecordingFormat = "mp4";
public const string DefaultOverlayPosition = "TopLeft";
public const string DefaultOverlayOpacity = "0.7";
public const string DefaultProtocol = "Rtsp";

// Utility methods
public static int GetMaxResolutionFromQuality(string videoQuality);
```

## Helpers

### ApplicationPaths
Provides default application paths using `Environment.SpecialFolder.CommonApplicationData`:
```csharp
public static class ApplicationPaths
{
    public static string DefaultLogsPath { get; }       // {ProgramData}\Linksoft\CameraWall\logs
    public static string DefaultSnapshotsPath { get; }  // {ProgramData}\Linksoft\CameraWall\snapshots
    public static string DefaultRecordingsPath { get; } // {ProgramData}\Linksoft\CameraWall\recordings
    public static string DefaultSettingsPath { get; }   // {ProgramData}\Linksoft\CameraWall\settings.json
    public static string DefaultCameraDataPath { get; } // {ProgramData}\Linksoft\CameraWall\cameras.json
}
```

## Debug Logging
Debug logging is configured via Serilog in `App.xaml.cs`:
- Controlled by `AdvancedSettings.EnableDebugLogging` and `AdvancedSettings.LogPath`
- When enabled, logs to `{LogPath}\camera-wall-{date}.log`
- Daily rolling with 7-day retention
- Settings loaded early (before Host builder) to configure logging at startup
- Logs include: layout loading, camera connection state changes, recording state changes

## Build
```bash
dotnet build
dotnet run --project src/Linksoft.Wpf.CameraWall.App
```
