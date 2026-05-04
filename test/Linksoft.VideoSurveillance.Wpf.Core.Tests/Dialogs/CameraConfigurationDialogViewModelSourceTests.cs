namespace Linksoft.VideoSurveillance.Wpf.Core.Dialogs;

using CameraConfiguration = Linksoft.VideoSurveillance.Wpf.Core.Models.CameraConfiguration;
using IWpfCoreSettingsService = Linksoft.VideoSurveillance.Wpf.Core.Services.IApplicationSettingsService;

public class CameraConfigurationDialogViewModelSourceTests
{
    [Fact]
    public void IsNetworkSource_True_When_Connection_Source_Is_Network()
    {
        var vm = BuildVm(BuildNetworkCamera());

        vm.IsNetworkSource.Should().BeTrue();
        vm.IsUsbSource.Should().BeFalse();
    }

    [Fact]
    public void IsUsbSource_True_When_Connection_Source_Is_Usb()
    {
        var vm = BuildVm(BuildUsbCamera());

        vm.IsUsbSource.Should().BeTrue();
        vm.IsNetworkSource.Should().BeFalse();
    }

    [Fact]
    public void Switching_From_Network_To_Usb_Resets_IpAddress_Port_Auth()
    {
        var camera = BuildNetworkCamera();
        camera.Connection.IpAddress = "10.0.0.1";
        camera.Connection.Port = 554;
        camera.Connection.Path = "stream1";
        camera.Authentication.UserName = "admin";
        camera.Authentication.Password = "secret";

        var vm = BuildVm(camera);
        vm.IsUsbSource = true;

        // The VM owns its own clone — assert against vm.Camera, not the seed.
        vm.Camera.Connection.Source.Should().Be(CameraSource.Usb);
        vm.Camera.Connection.IpAddress.Should().BeEmpty();
        vm.Camera.Connection.Port.Should().Be(0);
        vm.Camera.Connection.Path.Should().BeNull();
        vm.Camera.Authentication.UserName.Should().BeEmpty();
        vm.Camera.Authentication.Password.Should().BeEmpty();
        vm.Camera.Connection.Usb.Should().NotBeNull();
    }

    [Fact]
    public void Switching_From_Usb_To_Network_Resets_UsbSettings()
    {
        var camera = BuildUsbCamera();
        camera.Connection.Usb = new UsbConnectionSettings
        {
            DeviceId = "abc",
            FriendlyName = "Cam",
            Format = new Linksoft.VideoSurveillance.Models.UsbStreamFormat { Width = 1920, Height = 1080, FrameRate = 30 },
        };

        var vm = BuildVm(camera);
        vm.IsNetworkSource = true;

        vm.Camera.Connection.Source.Should().Be(CameraSource.Network);
        vm.Camera.Connection.Usb.Should().BeNull();
    }

    [Fact]
    public void Switching_To_Same_Source_Is_NoOp()
    {
        var camera = BuildNetworkCamera();
        camera.Connection.IpAddress = "10.0.0.1";

        var vm = BuildVm(camera);
        vm.IsNetworkSource = true; // already Network — must not wipe fields

        vm.Camera.Connection.IpAddress.Should().Be("10.0.0.1");
    }

    [Fact]
    public void CanEditSource_Is_True_For_New_Camera_And_False_For_Existing()
    {
        var camera = BuildNetworkCamera();

        BuildVm(camera, isNew: true).CanEditSource.Should().BeTrue();
        BuildVm(camera, isNew: false).CanEditSource.Should().BeFalse();
    }

    [Fact]
    public void HasMinimumSourceIdentity_Network_Requires_IpAddress()
    {
        // Indirect check via CanSave — directly testing the private
        // helper would be brittle and the gating effect on Save is
        // the user-visible behaviour we care about.
        var camera = BuildNetworkCamera();
        camera.Display.DisplayName = "Cam";

        var vm = BuildVm(camera);
        vm.SaveCommand.CanExecute(null).Should().BeFalse();

        vm.Camera.Connection.IpAddress = "10.0.0.1";
        vm.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void HasMinimumSourceIdentity_Usb_Requires_Device_OrFriendlyName()
    {
        var camera = BuildUsbCamera();
        camera.Display.DisplayName = "Cam";
        camera.Connection.Usb = new UsbConnectionSettings();

        var vm = BuildVm(camera);
        vm.SaveCommand.CanExecute(null).Should().BeFalse();

        vm.Camera.Connection.Usb!.DeviceId = "abc";
        vm.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void Endpoint_Uniqueness_Skipped_For_Usb_Cameras()
    {
        // The endpoint-uniqueness check is bypassed for USB cameras
        // because two recording configs against the same physical
        // device are operator-meaningful, and the OS arbitrates the
        // single-open contention at runtime regardless.
        IReadOnlyCollection<(string IpAddress, string? Path)> existing = [("10.0.0.1", "stream1")];

        var usbCamera = BuildUsbCamera();
        usbCamera.Display.DisplayName = "USB Cam";
        usbCamera.Connection.Usb = new UsbConnectionSettings { DeviceId = "abc" };

        var vm = BuildVmWithEndpoints(usbCamera, existing);

        vm.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    private static CameraConfigurationDialogViewModel BuildVm(
        CameraConfiguration camera,
        bool isNew = true)
        => BuildVmWithEndpoints(camera, [], isNew);

    private static CameraConfigurationDialogViewModel BuildVmWithEndpoints(
        CameraConfiguration camera,
        IReadOnlyCollection<(string IpAddress, string? Path)> endpoints,
        bool isNew = true)
    {
        var settingsService = Substitute.For<IWpfCoreSettingsService>();
        settingsService.CameraDisplay.Returns(new CameraDisplayAppSettings());
        settingsService.Connection.Returns(new ConnectionAppSettings());
        settingsService.Recording.Returns(new RecordingSettings());
        settingsService.Performance.Returns(new PerformanceSettings());
        settingsService.MotionDetection.Returns(new MotionDetectionSettings());

        var playerFactory = Substitute.For<IVideoPlayerFactory>();

        return new CameraConfigurationDialogViewModel(
            camera,
            isNew,
            endpoints,
            settingsService,
            playerFactory,
            NullUsbCameraEnumerator.Instance);
    }

    private static CameraConfiguration BuildNetworkCamera()
    {
        var camera = new CameraConfiguration();
        camera.Connection.Source = CameraSource.Network;
        camera.Connection.Protocol = CameraProtocol.Rtsp;
        camera.Connection.Port = 554;
        return camera;
    }

    private static CameraConfiguration BuildUsbCamera()
    {
        var camera = new CameraConfiguration();
        camera.Connection.Source = CameraSource.Usb;
        camera.Connection.Usb = new UsbConnectionSettings();
        return camera;
    }
}