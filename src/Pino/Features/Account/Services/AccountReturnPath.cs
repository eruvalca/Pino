namespace Pino.Features.Account.Services;

internal static class AccountReturnPath
{
    internal static string Local(string? path) =>
        !string.IsNullOrWhiteSpace(path) && Uri.IsWellFormedUriString(path, UriKind.Relative) &&
        !path.StartsWith("//", StringComparison.Ordinal) && !path.Contains('\\', StringComparison.Ordinal) &&
        !path.Any(char.IsControl) ? path : "/club/access";

    internal static string Link(string page, string? returnUrl) => $"{page}?returnUrl={Uri.EscapeDataString(Local(returnUrl))}";
}
