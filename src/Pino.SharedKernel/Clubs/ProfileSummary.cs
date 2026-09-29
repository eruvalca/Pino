using System.Diagnostics.CodeAnalysis;
namespace Pino.SharedKernel.Clubs;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Club contracts are shared by the server, browser, and UI assemblies.")]
public sealed record ProfileSummary(string FirstName, string LastName, Uri? PhotoUrl)
{
    public bool IsComplete => !string.IsNullOrWhiteSpace(FirstName) && !string.IsNullOrWhiteSpace(LastName) && PhotoUrl is not null;
}
