using System.Diagnostics.CodeAnalysis;
namespace Pino.SharedKernel.Clubs;

// HTTP/UI transport for this feature. The server uses a named union for operation outcomes.
[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Club contracts are shared by the server, browser, and UI assemblies.")]
public sealed record ClubReply(ClubReplyKind Kind, string Message)
{
    public bool Succeeded => Kind == ClubReplyKind.Saved;
}
