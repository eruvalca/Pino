using Microsoft.AspNetCore.Components;

namespace Pino.Features.Account.Pages;

public sealed partial class RegisterConfirmation
{
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
