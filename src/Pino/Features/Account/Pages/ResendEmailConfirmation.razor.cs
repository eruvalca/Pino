using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Pino.Data;
using Pino.Features.Account.Extensions;

namespace Pino.Features.Account.Pages;

public sealed partial class ResendEmailConfirmation
{
    private string? _message;
    [SupplyParameterFromQuery] private string? ReturnUrl { get; set; }

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = default!;

    protected override void OnInitialized() => Input ??= new();

    private async Task OnValidSubmitAsync()
    {
        var user = await UserManager.FindByEmailAsync(Input.Email);
        if (user is null)
        {
            _message = "If this email belongs to an account, check your inbox and spam folder. Use the latest confirmation message, or wait at least a minute before requesting another. Requests are limited to six per hour.";
            return;
        }

        var userId = await UserManager.GetUserIdAsync(user);
        var code = await UserManager.GenerateEmailConfirmationTokenAsync(user);
        code = code.EncodeIdentityToken();
        var callbackUrl = NavigationManager.GetUriWithQueryParameters(
            NavigationManager.ToAbsoluteUri("Account/ConfirmEmail").AbsoluteUri,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["userId"] = userId, ["code"] = code, ["returnUrl"] = ReturnUrl });
        await EmailSender.SendConfirmationLinkAsync(user, Input.Email, HtmlEncoder.Default.Encode(callbackUrl));

        _message = "If this email belongs to an account, check your inbox and spam folder. Use the latest confirmation message, or wait at least a minute before requesting another. Requests are limited to six per hour.";
    }

    private sealed class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
    }
}
