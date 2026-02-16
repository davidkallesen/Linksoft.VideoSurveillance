namespace Linksoft.VideoSurveillance.Blazor.App;

#pragma warning disable MA0049, CA1724 // Type name should not match containing namespace
public partial class App
#pragma warning restore MA0049, CA1724
{
    private MudThemeProvider mudThemeProvider = null!;
    private bool isDarkMode = true;

    public bool IsDarkMode
    {
        get => isDarkMode;
        set
        {
            if (isDarkMode != value)
            {
                isDarkMode = value;
                StateHasChanged();
            }
        }
    }

    public void ToggleDarkMode()
        => IsDarkMode = !IsDarkMode;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            isDarkMode = await mudThemeProvider.GetSystemDarkModeAsync();
            await InvokeAsync(StateHasChanged);
        }
    }
}