using Microsoft.AspNetCore.Components.Routing;

namespace Pino.UI.Layout;

public sealed partial class NavMenu
{
    private string? _currentUrl;

    private bool IsWorkspace
    {
        get
        {
            var path = _currentUrl?.Split('?')[0].Split('#')[0].TrimEnd('/') ?? "";
            return path.Length == 0 || path.Equals("club/access", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("clubs/", StringComparison.OrdinalIgnoreCase);
        }
    }

    protected override void OnInitialized()
    {
        _currentUrl = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        NavigationManager.LocationChanged += OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        _currentUrl = NavigationManager.ToBaseRelativePath(e.Location);
        StateHasChanged();
    }

    public void Dispose() => NavigationManager.LocationChanged -= OnLocationChanged;
}
