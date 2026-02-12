using CoreAdvancedSettings = Linksoft.VideoSurveillance.Models.Settings.AdvancedSettings;
using CoreGeneralSettings = Linksoft.VideoSurveillance.Models.Settings.GeneralSettings;
using CoreRecordingSettings = Linksoft.VideoSurveillance.Models.Settings.RecordingSettings;

namespace Linksoft.VideoSurveillance.Api.Domain.Mapping;

internal static class SettingsMappingExtensions
{
    public static AppSettings ToApiModel(
        CoreGeneralSettings general,
        CoreRecordingSettings recording,
        CoreAdvancedSettings advanced,
        string snapshotPath)
        => new(
            ThemeBase: ParseThemeBase(general.ThemeBase),
            Language: general.Language,
            ConnectOnStartup: general.ConnectCamerasOnStartup,
            RecordingPath: recording.RecordingPath,
            RecordingFormat: ParseRecordingFormat(recording.RecordingFormat),
            SnapshotPath: snapshotPath,
            EnableDebugLogging: advanced.EnableDebugLogging,
            LogPath: advanced.LogPath);

    public static void ApplyToCore(
        this AppSettings api,
        CoreGeneralSettings general,
        CoreRecordingSettings recording,
        CoreAdvancedSettings advanced)
    {
        if (api.ThemeBase is not null)
        {
            general.ThemeBase = api.ThemeBase.Value.ToString();
        }

        if (!string.IsNullOrEmpty(api.Language))
        {
            general.Language = api.Language;
        }

        general.ConnectCamerasOnStartup = api.ConnectOnStartup;

        if (!string.IsNullOrEmpty(api.RecordingPath))
        {
            recording.RecordingPath = api.RecordingPath;
        }

        if (api.RecordingFormat is not null)
        {
            recording.RecordingFormat = api.RecordingFormat.Value.ToString().ToLowerInvariant();
        }

        advanced.EnableDebugLogging = api.EnableDebugLogging;

        if (!string.IsNullOrEmpty(api.LogPath))
        {
            advanced.LogPath = api.LogPath;
        }
    }

    private static AppSettingsThemeBase? ParseThemeBase(string? themeBase)
    {
        if (string.IsNullOrEmpty(themeBase))
        {
            return null;
        }

        return string.Equals(themeBase, "Light", StringComparison.OrdinalIgnoreCase)
            ? AppSettingsThemeBase.Light
            : AppSettingsThemeBase.Dark;
    }

    private static AppSettingsRecordingFormat? ParseRecordingFormat(
        string? format)
    {
        if (string.IsNullOrEmpty(format))
        {
            return null;
        }

        return format.ToUpperInvariant() switch
        {
            "MKV" => AppSettingsRecordingFormat.Mkv,
            "AVI" => AppSettingsRecordingFormat.Avi,
            _ => AppSettingsRecordingFormat.Mp4,
        };
    }
}