using Microsoft.AspNetCore.Components;
using Pino.Features.Account.Services;

namespace Pino.Features.Account.Pages;

public sealed partial class RegisterConfirmation
{
    [SupplyParameterFromQuery] private string? ReturnUrl { get; set; }
    private string SignInUrl => AccountReturnPath.Link("/Account/Login", ReturnUrl);
    private string ResendUrl => AccountReturnPath.Link("/Account/ResendEmailConfirmation", ReturnUrl);
    [SupplyParameterFromQuery]
    private string? Email { get; set; }

    protected override void OnInitialized()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            RedirectManager.RedirectTo("");
        }
    }
}
