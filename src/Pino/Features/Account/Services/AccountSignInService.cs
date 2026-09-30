using Microsoft.AspNetCore.Identity;
using Pino.Data;
using Pino.Features.Account.Extensions;
using Pino.Features.Account.Models;

namespace Pino.Features.Account.Services;

internal sealed class AccountSignInService(SignInManager<ApplicationUser> signInManager)
{
    public async Task<SignInOutcome> PasswordAsync(string email, string password, bool rememberMe) =>
        (await signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: true)).ToSignInOutcome();

    public async Task<SignInOutcome> PasskeyAsync(string credentialJson) =>
        (await signInManager.PasskeySignInAsync(credentialJson)).ToSignInOutcome();

    public async Task<SignInOutcome> AuthenticatorAsync(string code, bool rememberMe, bool rememberMachine)
    {
        var normalizedCode = code.NormalizeAuthenticatorCode();
        return (await signInManager.TwoFactorAuthenticatorSignInAsync(normalizedCode, rememberMe, rememberMachine)).ToSignInOutcome();
    }

    public async Task<SignInOutcome> RecoveryCodeAsync(string code) =>
        (await signInManager.TwoFactorRecoveryCodeSignInAsync(code.NormalizeRecoveryCode())).ToSignInOutcome();

    public async Task<SignInOutcome> ExternalAsync(string provider, string providerKey) =>
        (await signInManager.ExternalLoginSignInAsync(provider, providerKey, isPersistent: false, bypassTwoFactor: true)).ToSignInOutcome();
}
