using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Pino.Data;
using Pino.Features.Account.Extensions;

namespace Pino.Features.Account.Pages;

public sealed partial class Register
{
    private IEnumerable<IdentityError>? _identityErrors;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = default!;

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    private string? Message => _identityErrors is null ? null : $"Error: {_identityErrors.FormatDescriptions(", ")}";

    protected override void OnInitialized() => Input ??= new();

    public async Task RegisterUserAsync(EditContext _)
    {
        var result = await AccountRegistration.PasswordAsync(Input.Email, Input.Password);
        await result.Match(
            created => CompleteRegistrationAsync(created.User),
            rejected =>
            {
                _identityErrors = rejected.Errors;
                return Task.CompletedTask;
            });
    }

    private async Task CompleteRegistrationAsync(ApplicationUser user)
    {
        LogUserCreatedWithPassword(Logger);

        var userId = await UserManager.GetUserIdAsync(user);
        var code = await UserManager.GenerateEmailConfirmationTokenAsync(user);
        code = code.EncodeIdentityToken();
        var callbackUrl = NavigationManager.GetUriWithQueryParameters(
            NavigationManager.ToAbsoluteUri("Account/ConfirmEmail").AbsoluteUri,
            new Dictionary<string, object?>(StringComparer.Ordinal) { ["userId"] = userId, ["code"] = code, ["returnUrl"] = ReturnUrl });

        await EmailSender.SendConfirmationLinkAsync(user, Input.Email, HtmlEncoder.Default.Encode(callbackUrl));

        if (UserManager.Options.SignIn.RequireConfirmedAccount)
        {
            RedirectManager.RedirectTo(
                "Account/RegisterConfirmation",
                new(StringComparer.Ordinal) { ["email"] = Input.Email, ["returnUrl"] = ReturnUrl });
        }
        else
        {
            await SignInManager.SignInAsync(user, isPersistent: false);
            RedirectManager.RedirectTo(Pino.Features.Account.Services.AccountReturnPath.Local(ReturnUrl));
        }
    }

    [LoggerMessage(EventId = 1011, Level = LogLevel.Information, Message = "User created a new account with password.")]
    private static partial void LogUserCreatedWithPassword(ILogger logger);

    private sealed class InputModel
    {
        [Range(typeof(bool), "true", "true", ErrorMessage = "Confirm that you are an adult acting as club staff.")]
        public bool AdultStaff { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required]
        [StringLength(100, ErrorMessage = "{0} must be {2} to {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = "";

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }
}
