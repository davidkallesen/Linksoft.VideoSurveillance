namespace Linksoft.Wpf.CameraWall.Converters;

/// <summary>
/// JSON converter for <see cref="CameraConfiguration"/> that supports migration from flat to nested structure.
/// Reads both legacy flat format and new nested format, always writes new nested format.
/// </summary>
public sealed class CameraConfigurationJsonConverter : JsonConverter<CameraConfiguration>
{
    /// <inheritdoc />
    public override CameraConfiguration? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        var config = new CameraConfiguration();

        // Read Id
        if (root.TryGetProperty("id", out var idElement) ||
            root.TryGetProperty("Id", out idElement))
        {
            config.Id = idElement.GetGuid();
        }

        // Determine if this is the new nested format or legacy flat format
        var hasConnectionObject = root.TryGetProperty("connection", out var connectionElement) ||
                                  root.TryGetProperty("Connection", out connectionElement);

        if (hasConnectionObject && connectionElement.ValueKind == JsonValueKind.Object)
        {
            // New nested format
            ReadNestedFormat(root, config, connectionElement);
        }
        else
        {
            // Legacy flat format
            ReadLegacyFormat(root, config);
        }

        return config;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, CameraConfiguration value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        // Id
        writer.WriteString("id", value.Id);

        // Connection
        writer.WritePropertyName("connection");
        WriteConnectionSettings(writer, value.Connection);

        // Authentication
        writer.WritePropertyName("authentication");
        WriteAuthenticationSettings(writer, value.Authentication);

        // Display
        writer.WritePropertyName("display");
        WriteDisplaySettings(writer, value.Display);

        // Stream
        writer.WritePropertyName("stream");
        WriteStreamSettings(writer, value.Stream);

        writer.WriteEndObject();
    }

    private static void ReadNestedFormat(JsonElement root, CameraConfiguration config, JsonElement connectionElement)
    {
        // Connection
        ReadConnectionSettings(connectionElement, config.Connection);

        // Authentication
        if ((root.TryGetProperty("authentication", out var authElement) ||
             root.TryGetProperty("Authentication", out authElement)) &&
            authElement.ValueKind == JsonValueKind.Object)
        {
            ReadAuthenticationSettings(authElement, config.Authentication);
        }

        // Display
        if ((root.TryGetProperty("display", out var displayElement) ||
             root.TryGetProperty("Display", out displayElement)) &&
            displayElement.ValueKind == JsonValueKind.Object)
        {
            ReadDisplaySettings(displayElement, config.Display);
        }

        // Stream
        if ((root.TryGetProperty("stream", out var streamElement) ||
             root.TryGetProperty("Stream", out streamElement)) &&
            streamElement.ValueKind == JsonValueKind.Object)
        {
            ReadStreamSettings(streamElement, config.Stream);
        }
    }

    private static void ReadLegacyFormat(JsonElement root, CameraConfiguration config)
    {
        // Connection properties (flat)
        if (TryGetStringProperty(root, "ipAddress", "IpAddress", out var ipAddress))
        {
            config.Connection.IpAddress = ipAddress;
        }

        if (TryGetStringProperty(root, "protocol", "Protocol", out var protocol))
        {
            if (Enum.TryParse<CameraProtocol>(protocol, ignoreCase: true, out var protocolEnum))
            {
                config.Connection.Protocol = protocolEnum;
            }
        }

        if (TryGetInt32Property(root, "port", "Port", out var port))
        {
            config.Connection.Port = port;
        }

        if (TryGetStringProperty(root, "path", "Path", out var path))
        {
            config.Connection.Path = path;
        }

        // Authentication properties (flat)
        if (TryGetStringProperty(root, "userName", "UserName", out var userName))
        {
            config.Authentication.UserName = userName;
        }

        if (TryGetStringProperty(root, "password", "Password", out var password))
        {
            config.Authentication.Password = password;
        }

        // Display properties (flat)
        if (TryGetStringProperty(root, "displayName", "DisplayName", out var displayName))
        {
            config.Display.DisplayName = displayName;
        }

        if (TryGetStringProperty(root, "description", "Description", out var description))
        {
            config.Display.Description = description;
        }

        if (TryGetStringProperty(root, "overlayPosition", "OverlayPosition", out var overlayPosition))
        {
            if (Enum.TryParse<OverlayPosition>(overlayPosition, ignoreCase: true, out var positionEnum))
            {
                config.Display.OverlayPosition = positionEnum;
            }
        }

        // Stream properties (flat)
        if (TryGetBoolProperty(root, "useLowLatencyMode", "UseLowLatencyMode", out var useLowLatencyMode))
        {
            config.Stream.UseLowLatencyMode = useLowLatencyMode;
        }

        if (TryGetInt32Property(root, "maxLatencyMs", "MaxLatencyMs", out var maxLatencyMs))
        {
            config.Stream.MaxLatencyMs = maxLatencyMs;
        }

        if (TryGetStringProperty(root, "rtspTransport", "RtspTransport", out var rtspTransport))
        {
            config.Stream.RtspTransport = rtspTransport;
        }

        if (TryGetInt32Property(root, "bufferDurationMs", "BufferDurationMs", out var bufferDurationMs))
        {
            config.Stream.BufferDurationMs = bufferDurationMs;
        }
    }

    private static void ReadConnectionSettings(JsonElement element, ConnectionSettings settings)
    {
        if (TryGetStringProperty(element, "ipAddress", "IpAddress", out var ipAddress))
        {
            settings.IpAddress = ipAddress;
        }

        if (TryGetStringProperty(element, "protocol", "Protocol", out var protocol))
        {
            if (Enum.TryParse<CameraProtocol>(protocol, ignoreCase: true, out var protocolEnum))
            {
                settings.Protocol = protocolEnum;
            }
        }

        if (TryGetInt32Property(element, "port", "Port", out var port))
        {
            settings.Port = port;
        }

        if (TryGetStringProperty(element, "path", "Path", out var path))
        {
            settings.Path = path;
        }
    }

    private static void ReadAuthenticationSettings(JsonElement element, AuthenticationSettings settings)
    {
        if (TryGetStringProperty(element, "userName", "UserName", out var userName))
        {
            settings.UserName = userName;
        }

        if (TryGetStringProperty(element, "password", "Password", out var password))
        {
            settings.Password = password;
        }
    }

    private static void ReadDisplaySettings(JsonElement element, CameraDisplaySettings settings)
    {
        if (TryGetStringProperty(element, "displayName", "DisplayName", out var displayName))
        {
            settings.DisplayName = displayName;
        }

        if (TryGetStringProperty(element, "description", "Description", out var description))
        {
            settings.Description = description;
        }

        if (TryGetStringProperty(element, "overlayPosition", "OverlayPosition", out var overlayPosition))
        {
            if (Enum.TryParse<OverlayPosition>(overlayPosition, ignoreCase: true, out var positionEnum))
            {
                settings.OverlayPosition = positionEnum;
            }
        }
    }

    private static void ReadStreamSettings(JsonElement element, StreamSettings settings)
    {
        if (TryGetBoolProperty(element, "useLowLatencyMode", "UseLowLatencyMode", out var useLowLatencyMode))
        {
            settings.UseLowLatencyMode = useLowLatencyMode;
        }

        if (TryGetInt32Property(element, "maxLatencyMs", "MaxLatencyMs", out var maxLatencyMs))
        {
            settings.MaxLatencyMs = maxLatencyMs;
        }

        if (TryGetStringProperty(element, "rtspTransport", "RtspTransport", out var rtspTransport))
        {
            settings.RtspTransport = rtspTransport;
        }

        if (TryGetInt32Property(element, "bufferDurationMs", "BufferDurationMs", out var bufferDurationMs))
        {
            settings.BufferDurationMs = bufferDurationMs;
        }
    }

    private static void WriteConnectionSettings(Utf8JsonWriter writer, ConnectionSettings settings)
    {
        writer.WriteStartObject();
        writer.WriteString("ipAddress", settings.IpAddress);
        writer.WriteString("protocol", settings.Protocol.ToString());
        writer.WriteNumber("port", settings.Port);
        if (settings.Path is not null)
        {
            writer.WriteString("path", settings.Path);
        }
        else
        {
            writer.WriteNull("path");
        }

        writer.WriteEndObject();
    }

    private static void WriteAuthenticationSettings(Utf8JsonWriter writer, AuthenticationSettings settings)
    {
        writer.WriteStartObject();
        if (settings.UserName is not null)
        {
            writer.WriteString("userName", settings.UserName);
        }
        else
        {
            writer.WriteNull("userName");
        }

        if (settings.Password is not null)
        {
            writer.WriteString("password", settings.Password);
        }
        else
        {
            writer.WriteNull("password");
        }

        writer.WriteEndObject();
    }

    private static void WriteDisplaySettings(Utf8JsonWriter writer, CameraDisplaySettings settings)
    {
        writer.WriteStartObject();
        writer.WriteString("displayName", settings.DisplayName);
        if (settings.Description is not null)
        {
            writer.WriteString("description", settings.Description);
        }
        else
        {
            writer.WriteNull("description");
        }

        writer.WriteString("overlayPosition", settings.OverlayPosition.ToString());
        writer.WriteEndObject();
    }

    private static void WriteStreamSettings(Utf8JsonWriter writer, StreamSettings settings)
    {
        writer.WriteStartObject();
        writer.WriteBoolean("useLowLatencyMode", settings.UseLowLatencyMode);
        writer.WriteNumber("maxLatencyMs", settings.MaxLatencyMs);
        writer.WriteString("rtspTransport", settings.RtspTransport);
        writer.WriteNumber("bufferDurationMs", settings.BufferDurationMs);
        writer.WriteEndObject();
    }

    private static bool TryGetStringProperty(JsonElement element, string camelName, string pascalName, [NotNullWhen(true)] out string? value)
    {
        if (element.TryGetProperty(camelName, out var prop) || element.TryGetProperty(pascalName, out prop))
        {
            if (prop.ValueKind == JsonValueKind.String)
            {
                value = prop.GetString();
                return value is not null;
            }
        }

        value = null;
        return false;
    }

    private static bool TryGetInt32Property(JsonElement element, string camelName, string pascalName, out int value)
    {
        if (element.TryGetProperty(camelName, out var prop) || element.TryGetProperty(pascalName, out prop))
        {
            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out value))
            {
                return true;
            }
        }

        value = 0;
        return false;
    }

    private static bool TryGetBoolProperty(JsonElement element, string camelName, string pascalName, out bool value)
    {
        if (element.TryGetProperty(camelName, out var prop) || element.TryGetProperty(pascalName, out prop))
        {
            if (prop.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                value = prop.GetBoolean();
                return true;
            }
        }

        value = false;
        return false;
    }
}
