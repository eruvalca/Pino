using System.Diagnostics.CodeAnalysis;
namespace Pino.SharedKernel.Clubs;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Club contracts are shared by the server, browser, and UI assemblies.")]
public sealed record PersonSummary(string UserId, string FirstName, string LastName, Uri PhotoUrl, ClubRole Role, Guid? RequestId, DateTimeOffset Since);
