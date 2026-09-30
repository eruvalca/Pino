namespace Pino.Features.Account.Components;

internal static class AccountNavigation
{
    private static readonly char[] _suffixSeparators = ['?', '#'];
    internal static string Path(string relativeUri) => relativeUri.Split(_suffixSeparators, 2)[0].TrimEnd('/');
    internal static string Section(string relativeUri)
    {
        var path = Path(relativeUri).Trim('/').Split('/');
        var page = path.Length > 2 ? path[2].ToUpperInvariant() : "";
        return page switch
        {
            "EMAIL" => "email",
            "CHANGEPASSWORD" or "SETPASSWORD" => "password",
            "EXTERNALLOGINS" => "external",
            "TWOFACTORAUTHENTICATION" or "ENABLEAUTHENTICATOR" or "RESETAUTHENTICATOR" or "DISABLE2FA" or "GENERATERECOVERYCODES" => "twofactor",
            "PASSKEYS" or "RENAMEPASSKEY" => "passkeys",
            "PERSONALDATA" or "DELETEPERSONALDATA" => "data",
            _ => "profile",
        };
    }
    internal static string Group(string section) => section switch { "profile" or "email" => "profile", "data" => "data", _ => "security" };
    internal static string Label(string section) => section switch
    {
        "email" => "Email",
        "password" => "Password",
        "external" => "Connected accounts",
        "twofactor" => "Two-factor authentication",
        "passkeys" => "Passkeys",
        "data" => "Personal data",
        _ => "Profile",
    };
    internal static string Href(string section) => "Account/Manage" + (section switch
    {
        "email" => "/Email",
        "password" => "/ChangePassword",
        "external" => "/ExternalLogins",
        "twofactor" => "/TwoFactorAuthentication",
        "passkeys" => "/Passkeys",
        "data" => "/PersonalData",
        _ => "",
    });
}
