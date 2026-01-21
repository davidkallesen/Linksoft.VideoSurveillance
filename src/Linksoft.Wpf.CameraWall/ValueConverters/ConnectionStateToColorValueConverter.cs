namespace Linksoft.Wpf.CameraWall.ValueConverters;

/// <summary>
/// Converts ConnectionState to a color brush.
/// </summary>
public sealed class ConnectionStateToColorValueConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
        => value switch
        {
            ConnectionState.Connected => Brushes.LimeGreen,
            ConnectionState.Connecting => Brushes.Yellow,
            ConnectionState.Reconnecting => Brushes.Orange,
            ConnectionState.ConnectionFailed => Brushes.Red,
            ConnectionState.Disconnected => Brushes.Gray,
            _ => Brushes.Gray,
        };

    /// <inheritdoc />
    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
        => throw new NotSupportedException();
}