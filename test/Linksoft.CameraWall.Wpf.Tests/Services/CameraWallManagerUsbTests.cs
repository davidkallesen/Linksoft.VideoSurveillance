namespace Linksoft.CameraWall.Wpf.Services;

using IUsbCameraWatcher = Linksoft.VideoSurveillance.Services.IUsbCameraWatcher;

/// <summary>
/// Pins the seed-camera contract for the AddCamera / AddUsbCamera flow.
/// The dialog itself has its own ViewModel-level coverage; these tests
/// catch the layer between the ribbon command and the dialog where a
/// future refactor could silently drop the Source = Usb pre-selection.
/// </summary>
public class CameraWallManagerUsbTests
{
    [Fact]
    public void AddUsbCamera_PassesSeedCamera_WithSourceUsb_ToDialog()
    {
        var (manager, deps) = BuildManager();
        CameraConfiguration? capturedSeed = null;

        deps.Dialog
            .ShowCameraConfigurationDialog(
                Arg.Do<CameraConfiguration?>(c => capturedSeed = c),
                Arg.Any<bool>(),
                Arg.Any<IReadOnlyCollection<(string, string?)>>())
            .Returns((CameraConfiguration?)null);

        manager.AddUsbCamera();

        capturedSeed.Should().NotBeNull();
        capturedSeed!.Connection.Source.Should().Be(CameraSource.Usb);
        capturedSeed.Connection.Usb.Should().NotBeNull();
    }

    [Fact]
    public void AddCamera_PassesSeedCamera_WithSourceNetwork_ToDialog()
    {
        // Negative case — pin the network-source path too so that a
        // shared-helper refactor can't accidentally seed both flows
        // with the same source kind.
        var (manager, deps) = BuildManager();
        CameraConfiguration? capturedSeed = null;

        deps.Dialog
            .ShowCameraConfigurationDialog(
                Arg.Do<CameraConfiguration?>(c => capturedSeed = c),
                Arg.Any<bool>(),
                Arg.Any<IReadOnlyCollection<(string, string?)>>())
            .Returns((CameraConfiguration?)null);

        manager.AddCamera();

        capturedSeed.Should().NotBeNull();
        capturedSeed!.Connection.Source.Should().Be(CameraSource.Network);
        capturedSeed.Connection.Usb.Should().BeNull();
    }

    [Fact]
    public void AddUsbCamera_DialogCancelled_DoesNotPersist()
    {
        // Cancel returns null from the dialog — make sure the manager
        // doesn't accidentally write a half-baked seed to storage.
        var (manager, deps) = BuildManager();

        deps.Dialog
            .ShowCameraConfigurationDialog(
                Arg.Any<CameraConfiguration?>(),
                Arg.Any<bool>(),
                Arg.Any<IReadOnlyCollection<(string, string?)>>())
            .Returns((CameraConfiguration?)null);

        manager.AddUsbCamera();

        deps.Storage.DidNotReceive().AddOrUpdateCamera(Arg.Any<CameraConfiguration>());
    }

    private static (CameraWallManager Manager, Deps Deps) BuildManager()
    {
        var deps = new Deps
        {
            Storage = Substitute.For<ICameraStorageService>(),
            Dialog = Substitute.For<IDialogService>(),
            Settings = Substitute.For<IApplicationSettingsService>(),
            Recording = Substitute.For<IRecordingService>(),
            MotionDetection = Substitute.For<Linksoft.VideoSurveillance.Services.IMotionDetectionService>(),
            Timelapse = Substitute.For<ITimelapseService>(),
            GitHub = Substitute.For<Linksoft.VideoSurveillance.Services.IGitHubReleaseService>(),
            Toast = Substitute.For<IToastNotificationService>(),
            PlayerFactory = Substitute.For<IVideoPlayerFactory>(),
            UsbWatcher = Substitute.For<IUsbCameraWatcher>(),
        };

        deps.Storage.GetAllLayouts().Returns([]);
        deps.Storage.GetAllCameras().Returns([]);

        var manager = new CameraWallManager(
            NullLogger<CameraWallManager>.Instance,
            deps.Storage,
            deps.Dialog,
            deps.Settings,
            deps.Recording,
            deps.MotionDetection,
            deps.Timelapse,
            deps.GitHub,
            deps.Toast,
            deps.PlayerFactory,
            deps.UsbWatcher);
        return (manager, deps);
    }

    private sealed class Deps
    {
        public ICameraStorageService Storage { get; set; } = null!;

        public IDialogService Dialog { get; set; } = null!;

        public IApplicationSettingsService Settings { get; set; } = null!;

        public IRecordingService Recording { get; set; } = null!;

        public Linksoft.VideoSurveillance.Services.IMotionDetectionService MotionDetection { get; set; } = null!;

        public ITimelapseService Timelapse { get; set; } = null!;

        public Linksoft.VideoSurveillance.Services.IGitHubReleaseService GitHub { get; set; } = null!;

        public IToastNotificationService Toast { get; set; } = null!;

        public IVideoPlayerFactory PlayerFactory { get; set; } = null!;

        public IUsbCameraWatcher UsbWatcher { get; set; } = null!;
    }
}