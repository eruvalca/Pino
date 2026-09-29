namespace Pino.UI.Features.Tryouts.Models;

internal sealed record DecisionRecord(string Season, string Tryout, string Outcome, string Author, DateTimeOffset RecordedAt);
