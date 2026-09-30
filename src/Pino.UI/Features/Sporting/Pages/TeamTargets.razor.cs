using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Clubs;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Services;

namespace Pino.UI.Features.Sporting.Pages;

public sealed partial class TeamTargets : SportPageBase
{
    [Parameter] public Guid TeamId { get; set; }
    private TeamDetail? _detail;
    private int? _total;
    private readonly List<PositionDraft> _positions = [];

    protected override async Task LoadAsync()
    {
        if (Membership?.Role != ClubRole.Administrator) { throw new UnauthorizedAccessException(); }
        _detail = await Gateway.GetTeamAsync(ClubId, TeamId, seasonId: null, Token);
        _total = _detail.Team.RosterTarget;
        _positions.Clear();
        _positions.AddRange((_detail.Team.PositionTargets ?? []).Select(value => new PositionDraft { Position = value.Position, Players = value.Players }));
    }

    private void AddPosition() { if (_positions.Count < 100) { _positions.Add(new()); } }

    private async Task SaveTargetsAsync()
    {
        if (_detail is null) { return; }
        var input = new TeamTargetsInput(_detail.Team.Revision, _total, _positions.Select(value => new PositionTarget(value.Position, value.Players)).ToArray());
        if (await SaveAsync(() => Gateway.SaveTeamTargetsAsync(ClubId, TeamId, input, Token))) { await ExecuteAsync(LoadAsync); }
    }

    private sealed class PositionDraft
    {
        internal Guid Id { get; } = Guid.NewGuid();
        internal string Position { get; set; } = "";
        internal int Players { get; set; }
    }
}
