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
}
