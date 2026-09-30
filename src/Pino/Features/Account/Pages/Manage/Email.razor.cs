using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Pino.Data;
using Pino.Features.Account.Extensions;

namespace Pino.Features.Account.Pages.Manage;

public sealed partial class Email
{
    private string? _message;
    private ApplicationUser? _user;
    private string? _email;
    private bool _isEmailConfirmed;

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm(FormName = "change-email")]
    private InputModel Input { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        Input ??= new();

        _user = await UserManager.GetUserAsync(HttpContext.User);
        if (_user is null)
        {
            RedirectManager.RedirectToInvalidUser(UserManager, HttpContext);
            return;
        }

        _email = await UserManager.GetEmailAsync(_user);
        _isEmailConfirmed = await UserManager.IsEmailConfirmedAsync(_user);

        Input.NewEmail ??= _email;
    }

    private async Task OnValidSubmitAsync()
    {
        if (Input.NewEmail is null || string.Equals(Input.NewEmail, _email, StringComparison.Ordinal))
        {
            _message = "Your email is unchanged.";
            return;
        }

        if (_user is null)
        {
            RedirectManager.RedirectToInvalidUser(UserManager, HttpContext);
            return;
        }

        var userId = await UserManager.GetUserIdAsync(_user);
        var code = await UserManager.GenerateChangeEmailTokenAsync(_user, Input.NewEmail);
        code = code.EncodeIdentityToken();
        var callbackUrl = NavigationManager.GetUriWithQueryParameters(
            NavigationManager.ToAbsoluteUri("Account/ConfirmEmailChange").AbsoluteUri,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["userId"] = userId, ["email"] = Input.NewEmail, ["code"] = code });

        await EmailSender.SendConfirmationLinkAsync(_user, Input.NewEmail, HtmlEncoder.Default.Encode(callbackUrl));

        _message = "Email change confirmation requested. Check the new inbox and spam folder. If you requested a message recently, use the latest one or wait at least a minute before trying again. Requests are limited to six per hour.";
    }

    private async Task OnSendEmailVerificationAsync()
    {
        if (_email is null)
        {
            return;
        }

        if (_user is null)
        {
            RedirectManager.RedirectToInvalidUser(UserManager, HttpContext);
            return;
        }

        var userId = await UserManager.GetUserIdAsync(_user);
        var code = await UserManager.GenerateEmailConfirmationTokenAsync(_user);
        code = code.EncodeIdentityToken();
        var callbackUrl = NavigationManager.GetUriWithQueryParameters(
            NavigationManager.ToAbsoluteUri("Account/ConfirmEmail").AbsoluteUri,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["userId"] = userId, ["code"] = code });

        await EmailSender.SendConfirmationLinkAsync(_user, _email, HtmlEncoder.Default.Encode(callbackUrl));

        _message = "Verification email requested. Check your inbox and spam folder. If you requested a message recently, use the latest one or wait at least a minute before trying again. Requests are limited to six per hour.";
    }

    private sealed class InputModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "New email")]
        public string? NewEmail { get; set; }
    }
}
