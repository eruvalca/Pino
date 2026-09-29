using OneOf;

namespace Pino.UI.Features.Tryouts.Models;

[GenerateOneOf]
internal sealed partial class Decision : OneOfBase<Decision.Awaiting, Decision.Placement, Decision.Withdrawal, Decision.NonSelection>
{
    internal readonly record struct Awaiting;
    internal sealed record Placement(Team Team);
    internal readonly record struct Withdrawal;
    internal readonly record struct NonSelection;

    internal string Label => Match(_ => "Awaiting decision", _ => "Placed", _ => "Withdrawn", _ => "Not selected");
    internal string Kind => Match(_ => "awaiting", _ => "placed", _ => "withdrawn", _ => "not-selected");
    internal Team? AssignedTeam => Match<Team?>(_ => null, placed => placed.Team, _ => null, _ => null);
    internal bool IsComplete => Match(_ => false, _ => true, _ => true, _ => true);
}
