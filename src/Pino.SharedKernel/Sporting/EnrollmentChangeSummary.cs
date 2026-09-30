using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Sporting contracts are shared by server, browser and UI assemblies.")]
public sealed record EnrollmentChangeSummary(Guid Id, bool Removed, string Reason, string Author, DateTimeOffset CreatedAt, string Bib, Uri? AuthorPhotoUrl = null);
