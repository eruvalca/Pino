using Microsoft.AspNetCore.Identity;
using Pino.Features.Account.Models;

namespace Pino.Features.Account.Extensions;

internal static class SignInResultExtensions
{
    extension(SignInResult result)
    {
        internal SignInOutcome ToSignInOutcome()
        {
            if (result.Succeeded)
            {
                return new SignInOutcome.Succeeded();
            }
            if (result.RequiresTwoFactor)
            {
                return new SignInOutcome.RequiresTwoFactor();
            }
            if (result.IsLockedOut)
            {
                return new SignInOutcome.LockedOut();
            }
            if (result.IsNotAllowed)
            {
                return new SignInOutcome.NotAllowed();
            }
            return new SignInOutcome.Failed();
        }
    }
}
