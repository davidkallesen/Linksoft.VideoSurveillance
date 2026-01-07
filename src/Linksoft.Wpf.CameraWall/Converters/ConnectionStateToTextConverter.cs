namespace Linksoft.Wpf.CameraWall.Converters;

/// <summary>
/// Converts ConnectionState to localized text.
/// </summary>
public sealed class ConnectionStateToTextConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is ConnectionState connectionState)
        {
            return connectionState.GetDescription();
        }

        return Translations.Unknown;
    }

    /// <inheritdoc />
    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
        => throw new NotSupportedException();
}