using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Pino.Data;
using Pino.Features.Account.Models;
using Pino.Features.Account.Services;

namespace Pino.Features.Account.Pages;

public sealed partial class ConfirmEmail
{
    private string? _statusMessage;
    private bool _confirmed;
    [SupplyParameterFromQuery] private string? ReturnUrl { get; set; }
    private string ContinueUrl => AccountReturnPath.Link("/Account/Login", ReturnUrl);

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromQuery]
    private string? UserId { get; set; }

    [SupplyParameterFromQuery]
    private string? Code { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (UserId is null || Code is null)
        {
            RedirectManager.RedirectTo("");
            return;
        }

        var user = await UserManager.FindByIdAsync(UserId);
        if (user is null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            _statusMessage = $"Error loading user with ID {UserId}";
        }
        else
        {
            await TokenDecodeOutcome.Decode(Code).Match(
                async token =>
                {
                    var result = await UserManager.ConfirmEmailAsync(user, token.Value);
                    _confirmed = result.Succeeded;
                    _statusMessage = result.Succeeded ? "Thank you for confirming your email." : "Error confirming your email.";
                },
                _ =>
                {
                    _statusMessage = "Error confirming your email.";
                    return Task.CompletedTask;
                });
        }
    }
}
