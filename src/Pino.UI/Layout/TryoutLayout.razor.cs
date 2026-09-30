using Microsoft.AspNetCore.Components;

namespace Pino.UI.Layout;

public sealed partial class TryoutLayout
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
}
