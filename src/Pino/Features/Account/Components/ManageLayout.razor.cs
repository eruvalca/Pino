using Microsoft.AspNetCore.Components;

namespace Pino.Features.Account.Components;

public sealed partial class ManageLayout
{
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private string RelativePath => AccountNavigation.Path(Navigation.ToBaseRelativePath(Navigation.Uri));
    private string Section => AccountNavigation.Section(RelativePath);
    private bool IsChildPage => !string.Equals(RelativePath, AccountNavigation.Href(Section), StringComparison.OrdinalIgnoreCase);
}
