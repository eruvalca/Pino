using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Sporting;

namespace Pino.UI.Features.Sporting.Components;

public sealed partial class TeamCoverage
{
    [Parameter, EditorRequired] public TeamSummary Team { get; set; } = default!;
    [Parameter, EditorRequired] public IReadOnlyList<PlayerSummary> Players { get; set; } = [];
    private IReadOnlyList<PositionCoverage> _coverage = [];
    private int _unspecified;

    protected override void OnParametersSet()
    {
        var targets = Team.PositionTargets ?? [];
        var names = targets.Select(value => value.Position.Trim()).Concat(Players.SelectMany(value => new[] { value.Position.Trim(), value.SecondaryPosition.Trim() }))
            .Where(value => value.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase);
        _coverage = names.Select(name => new PositionCoverage(name,
            Players.Count(value => Same(value.Position, name)),
            Players.Count(value => Same(value.SecondaryPosition, name) && !Same(value.Position, name)),
            targets.FirstOrDefault(value => Same(value.Position, name))?.Players)).ToArray();
        _unspecified = Players.Count(value => string.IsNullOrWhiteSpace(value.Position));
    }

    private static bool Same(string first, string second) => string.Equals(first.Trim(), second.Trim(), StringComparison.OrdinalIgnoreCase);
    private static string Difference(int actual, int target)
    {
        if (actual == target) { return "at target"; }
        return actual < target ? $"{target - actual} short" : $"{actual - target} over";
    }
    private sealed record PositionCoverage(string Position, int Primary, int Secondary, int? Target);
}
