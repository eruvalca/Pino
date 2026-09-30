using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Clubs;

namespace Pino.UI.Features.Clubs.Components;

public sealed partial class ClubPageShell
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Parameter] public ClubSummary? Club { get; set; }
    [Parameter] public bool CanManagePeople { get; set; }
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    private bool IsSeasonSection
    {
        get
        {
            var path = Navigation.ToBaseRelativePath(Navigation.Uri).Split('?')[0].Split('#')[0].Split('/');
            return Club is not null && path.Length >= 3 && path[0].Equals("clubs", StringComparison.OrdinalIgnoreCase) &&
                Guid.TryParse(path[1], out var clubId) && clubId == Club.Id &&
                path[2].ToUpperInvariant() is "SEASONS" or "TEAMS" or "TRYOUTS";
        }
    }
}
