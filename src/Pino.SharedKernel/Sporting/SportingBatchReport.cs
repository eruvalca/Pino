using System.Diagnostics.CodeAnalysis;

namespace Pino.SharedKernel.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Sporting contracts are shared by server, browser and UI assemblies.")]
public sealed record SportingBatchReport(SportReplyKind Kind, string Message, int Applied, int Skipped, IReadOnlyList<SportingBatchRow> Rows, bool Replayed = false);
