namespace Linksoft.VideoSurveillance.Wpf.ValueConverters;

/// <summary>
/// Converts a connection state string to a color brush.
/// </summary>
public sealed class ConnectionStateToColorConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
        => value?.ToString()?.ToLowerInvariant() switch
        {
            "connected" => Brushes.LimeGreen,
            "connecting" => Brushes.Yellow,
            "reconnecting" => Brushes.Orange,
            "error" => Brushes.Red,
            "disconnected" => Brushes.Gray,
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
