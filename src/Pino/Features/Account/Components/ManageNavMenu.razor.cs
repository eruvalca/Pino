using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Pino.Data;

namespace Pino.Features.Account.Components;

public sealed partial class ManageNavMenu
{
    private bool _hasExternalLogins;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    private string Section => AccountNavigation.Section(Navigation.ToBaseRelativePath(Navigation.Uri));
    private string Group => AccountNavigation.Group(Section);
    private static readonly string[] _profileSections = ["profile", "email"];
    private static readonly string[] _securitySections = ["password", "twofactor", "passkeys", "external"];

    protected override async Task OnInitializedAsync() => _hasExternalLogins = (await SignInManager.GetExternalAuthenticationSchemesAsync()).Any();
}
