using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Sporting;

namespace Pino.UI.Features.Sporting.Pages;

public sealed partial class TryoutWork
{
    private string _positionFilter = "";
    private string _observationFilter = "";
    private Guid _previousSeasonId;
    private string _previousTeamFilter = "";
    private IReadOnlyList<SeasonSummary> _otherSeasons = [];
    private IReadOnlyList<SeasonTeamRoster> _previousTeams = [];
    private Dictionary<Guid, Guid> _previousTeamByPlayer = [];
    private IEnumerable<string> Positions => (_data?.Roster ?? []).SelectMany(value => new[] { value.Player.Position.Trim(), value.Player.SecondaryPosition.Trim() })
        .Where(value => value.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase);

    private bool MatchesPlanning(RosterEntry entry) =>
        (_positionFilter.Length == 0 || string.Equals(entry.Player.Position.Trim(), _positionFilter, StringComparison.OrdinalIgnoreCase) || string.Equals(entry.Player.SecondaryPosition.Trim(), _positionFilter, StringComparison.OrdinalIgnoreCase)) &&
        (_observationFilter.Length == 0 || !entry.HasObservations) &&
        (_previousTeamFilter.Length == 0 || (string.Equals(_previousTeamFilter, "none", StringComparison.Ordinal) ? !_previousTeamByPlayer.ContainsKey(entry.Player.Id) :
            _previousTeamByPlayer.TryGetValue(entry.Player.Id, out var teamId) && string.Equals(teamId.ToString(), _previousTeamFilter, StringComparison.Ordinal)));

    private Task ChangePreviousSeasonAsync(ChangeEventArgs args) => ExecuteAsync(async () =>
    {
        _previousSeasonId = Guid.TryParse(args.Value?.ToString(), out var id) ? id : Guid.Empty;
        _previousTeamFilter = "";
        _previousTeams = [];
        _previousTeamByPlayer.Clear();
        ResetRosterPage();
        await LoadPreviousTeamsAsync();
    });

    private async Task LoadPreviousTeamsAsync()
    {
        if (_previousSeasonId == Guid.Empty) { return; }
        // Clear before loading so a failed request cannot leave another season's filter active.
        _previousTeams = [];
        _previousTeamByPlayer.Clear();
        var selectedTeam = _previousTeamFilter;
        _previousTeamFilter = "";
        var review = await ReadInitialAsync("GetSeasonReviewAsync", () => Gateway.GetSeasonReviewAsync(ClubId, _previousSeasonId, Token));
        _previousTeams = review.Teams;
        _previousTeamByPlayer = review.Teams.SelectMany(team => team.Players.Select(player => (player.Id, TeamId: team.Team.Id))).ToDictionary(value => value.Id, value => value.TeamId);
        _previousTeamFilter = selectedTeam;
    }

    private void ClearRosterFilters()
    {
        _query = ""; _filter = ""; _year = ""; _teamFilter = "";
        _positionFilter = ""; _observationFilter = ""; _previousTeamFilter = "";
        ResetRosterPage();
    }
}
