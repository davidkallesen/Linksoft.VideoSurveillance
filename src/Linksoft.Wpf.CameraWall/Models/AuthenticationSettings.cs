namespace Linksoft.Wpf.CameraWall.Models;

/// <summary>
/// Represents authentication settings for a network camera.
/// </summary>
public partial class AuthenticationSettings : ObservableObject
{
    [ObservableProperty]
    private string? userName;

    [ObservableProperty]
    private string? password;

    /// <summary>
    /// Creates a deep copy of this instance.
    /// </summary>
    public AuthenticationSettings Clone()
        => new()
        {
            UserName = UserName,
            Password = Password,
        };

    /// <summary>
    /// Copies values from another instance.
    /// </summary>
    public void CopyFrom(AuthenticationSettings source)
    {
        ArgumentNullException.ThrowIfNull(source);

        UserName = source.UserName;
        Password = source.Password;
    }

    /// <summary>
    /// Determines whether the specified instance has the same values.
    /// </summary>
    public bool ValueEquals(AuthenticationSettings? other)
    {
        if (other is null)
        {
            return false;
        }

        return string.Equals(UserName, other.UserName, StringComparison.Ordinal) &&
               string.Equals(Password, other.Password, StringComparison.Ordinal);
    }
}