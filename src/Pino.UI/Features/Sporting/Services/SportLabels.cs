using System.Globalization;
using Pino.SharedKernel.Sporting;

namespace Pino.UI.Features.Sporting.Services;

internal static class SportLabels
{
    internal static string Progress(TryoutSummary tryout) => tryout switch
    {
        { Players: 0 } => "Needs players",
        { Complete: true } => "All decisions recorded",
        _ => string.Create(CultureInfo.CurrentCulture, $"{tryout.Players - tryout.Decided} awaiting decision"),
    };
    internal static string Decision(DecisionKind kind) => kind switch
    {
        DecisionKind.Placed => "Placed",
        DecisionKind.Withdrawn => "Withdrawn",
        DecisionKind.NotSelected => "Not selected",
        _ => "Awaiting decision",
    };
    internal static string Kind(DecisionKind kind) => kind switch
    {
        DecisionKind.Placed => "placed",
        DecisionKind.Withdrawn => "withdrawn",
        DecisionKind.NotSelected => "not-selected",
        _ => "awaiting",
    };
}
