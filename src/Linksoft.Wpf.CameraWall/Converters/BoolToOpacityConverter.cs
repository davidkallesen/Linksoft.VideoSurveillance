namespace Linksoft.Wpf.CameraWall.Converters;

/// <summary>
/// Converts a boolean to opacity (true = 1.0, false = 0.0).
/// </summary>
public sealed class BoolToOpacityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
        => value is true
            ? 1.0
            : 0.0;

    /// <inheritdoc />
    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
        => throw new NotSupportedException();
}