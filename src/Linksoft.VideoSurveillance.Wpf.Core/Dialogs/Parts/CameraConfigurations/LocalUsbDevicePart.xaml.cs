// ReSharper disable InvertIf
namespace Linksoft.VideoSurveillance.Wpf.Core.Dialogs.Parts.CameraConfigurations;

/// <summary>
/// Standalone-mode (local hardware) USB picker shown when
/// <c>UseLocalUsbPicker</c> is true. Hosts <c>atc:LabelUsbCameraPicker</c>
/// from <c>Atc.Wpf.Hardware</c>; the Atc picker owns enumeration via
/// WinRT <c>DeviceInformation</c> and exposes the selection as a
/// <c>UsbCameraInfo</c> through TwoWay binding to
/// <c>SelectedUsbCamera</c> on the dialog ViewModel.
///
/// In edit mode we have a saved <c>DeviceId</c> on the camera but the
/// Atc picker creates its own <c>UsbCameraInfo</c> instances inside its
/// internal <c>UsbCameraService</c>. Setting <c>Value</c> to a stub
/// instance won't render as selected — the inner ComboBox compares by
/// reference. So we walk the visual tree to grab the inner
/// <c>UsbCameraPicker</c>, listen on its <c>Cameras</c> collection, and
/// match by <c>DeviceId</c> as the async enumeration delivers entries.
/// </summary>
public partial class LocalUsbDevicePart
{
    private Atc.Wpf.Hardware.Pickers.UsbCameraPicker? innerPicker;

    public LocalUsbDevicePart()
    {
        InitializeComponent();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(
        object sender,
        RoutedEventArgs e)
    {
        innerPicker ??= FindInnerPicker(PartUsbCameraPicker);
        if (innerPicker is null)
        {
            return;
        }

        innerPicker.Cameras.CollectionChanged += OnPickerCamerasCollectionChanged;
        TryRebindFromCamera();
    }

    private void OnUnloaded(
        object sender,
        RoutedEventArgs e)
    {
        if (innerPicker is not null)
        {
            innerPicker.Cameras.CollectionChanged -= OnPickerCamerasCollectionChanged;
        }
    }

    private void OnPickerCamerasCollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        // Re-attempt the saved-DeviceId match each time the picker's
        // service publishes another batch — the initial enumeration is
        // async, so the saved camera may not appear in the very first
        // CollectionChanged event.
        TryRebindFromCamera();
    }

    private void TryRebindFromCamera()
    {
        if (innerPicker is null ||
            DataContext is not CameraConfigurationDialogViewModel vm)
        {
            return;
        }

        var savedDeviceId = vm.Camera.Connection.Usb?.DeviceId;
        if (string.IsNullOrEmpty(savedDeviceId))
        {
            return;
        }

        // Already bound (either by us or by the user) — leave it alone.
        if (vm.SelectedUsbCamera is not null)
        {
            return;
        }

        var match = innerPicker.Cameras.FirstOrDefault(c =>
            string.Equals(c.DeviceId, savedDeviceId, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
        {
            // Setting Value flows through the picker's binding to
            // vm.SelectedUsbCamera, which writes DeviceId/FriendlyName
            // back to Camera.Connection.Usb (idempotent here).
            innerPicker.Value = match;
        }
    }

    // Try the logical tree first — for templated UserControl hosts
    // (LabelControl → LabelContent → picker) it's the cheapest walk.
    // Some Atc templates attach content via a ContentPresenter which
    // doesn't show in the logical tree on first probe, so fall back
    // to a visual-tree walk if logical comes up empty.
    private static Atc.Wpf.Hardware.Pickers.UsbCameraPicker? FindInnerPicker(
        DependencyObject root)
        => FindInLogicalTree(root) ?? FindInVisualTree(root);

    private static Atc.Wpf.Hardware.Pickers.UsbCameraPicker? FindInLogicalTree(
        DependencyObject root)
    {
        if (root is Atc.Wpf.Hardware.Pickers.UsbCameraPicker picker)
        {
            return picker;
        }

        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is DependencyObject depChild)
            {
                var found = FindInLogicalTree(depChild);
                if (found is not null)
                {
                    return found;
                }
            }
        }

        return null;
    }

    private static Atc.Wpf.Hardware.Pickers.UsbCameraPicker? FindInVisualTree(
        DependencyObject root)
    {
        if (root is Atc.Wpf.Hardware.Pickers.UsbCameraPicker picker)
        {
            return picker;
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            var found = FindInVisualTree(child);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }
}