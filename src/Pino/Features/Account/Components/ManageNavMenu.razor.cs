using Microsoft.AspNetCore.Identity;
using Pino.Data;

namespace Pino.Features.Account.Components;

public sealed partial class ManageNavMenu
{
    private bool _hasExternalLogins;

    protected override async Task OnInitializedAsync() => _hasExternalLogins = (await SignInManager.GetExternalAuthenticationSchemesAsync()).Any();
}
