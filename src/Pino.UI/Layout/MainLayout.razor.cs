using Microsoft.AspNetCore.Components;

namespace Pino.UI.Layout;

public sealed partial class MainLayout
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
}
