using System.Globalization;
using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Services;

namespace Pino.UI.Features.Sporting.Pages;

public sealed partial class TeamComparison : SportPageBase
{
    [Parameter] public Guid SeasonId { get; set; }
    private SeasonReview? _review;
    private Guid _firstTeam;
    private Guid _secondTeam;
    private Guid _selectedId;
    private Guid _previewTeam;
    private Guid _tryoutId;
    private string _query = "";
    private string _position = "";
    private bool _playerLoaded;
    private bool _focusPlayer;
    private bool _focusRosters;
    private bool _showNotebook;
    private ElementReference _heading;
    private ElementReference _rosterHeading;
    private PlayerNotebook? _notebook;
    private PlayerTryoutSummary[] PlayerTryouts { get; set; } = [];
    private Dictionary<Guid, IReadOnlyList<PlayerSummary>> _filtered = [];
    private IReadOnlyList<PlayerSummary> _unplaced = [];
    private IEnumerable<SeasonTeamRoster> ComparedTeams => (_review?.Teams ?? []).Where(value => value.Team.Id == _firstTeam || value.Team.Id == _secondTeam);
    private IEnumerable<PlayerSummary> AllPlayers => (_review?.Teams ?? []).SelectMany(value => value.Players).Concat(_review?.UnplacedPlayers ?? []);
    private PlayerSummary? SelectedPlayer => AllPlayers.FirstOrDefault(value => value.Id == _selectedId);
    private PlayerTryoutSummary? SelectedTryout => PlayerTryouts.FirstOrDefault(value => value.Id == _tryoutId);
    private IEnumerable<string> Positions => AllPlayers.SelectMany(value => new[] { value.Position, value.SecondaryPosition }).Where(value => value.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase);

    protected override async Task LoadAsync()
    {
        _review = null;
        ClearPlayer();
        var review = await ReadInitialAsync("GetSeasonReviewAsync", () => Gateway.GetSeasonReviewAsync(ClubId, SeasonId, Token));
        _review = review;
        if (!review.Teams.Any(value => value.Team.Id == _firstTeam)) { _firstTeam = review.Teams.Count > 0 ? review.Teams[0].Team.Id : Guid.Empty; }
        if (!review.Teams.Any(value => value.Team.Id == _secondTeam)) { _secondTeam = review.Teams.Skip(1).FirstOrDefault()?.Team.Id ?? Guid.Empty; }
        FilterPlayers();
    }

    private void FilterPlayers()
    {
        _filtered = (_review?.Teams ?? []).ToDictionary(value => value.Team.Id, value => (IReadOnlyList<PlayerSummary>)value.Players.Where(Matches).ToArray());
        _unplaced = (_review?.UnplacedPlayers ?? []).Where(Matches).ToArray();
    }

    private bool Matches(PlayerSummary player) => (player.FullName.Contains(_query.Trim(), StringComparison.OrdinalIgnoreCase) || player.GraduationYear.ToString(CultureInfo.InvariantCulture).Contains(_query.Trim(), StringComparison.Ordinal)) &&
        (_position.Length == 0 || string.Equals(player.Position, _position, StringComparison.OrdinalIgnoreCase) || string.Equals(player.SecondaryPosition, _position, StringComparison.OrdinalIgnoreCase));

    private void ClearPlayer()
    {
        _selectedId = Guid.Empty; _previewTeam = Guid.Empty; _tryoutId = Guid.Empty;
        _showNotebook = false;
        _notebook = null; PlayerTryouts = []; _playerLoaded = false;
    }

    private Task SelectPlayerAsync(Guid id) => ExecuteAsync(async () =>
    {
        ClearPlayer();
        _selectedId = id;
        _showNotebook = true;
        _focusPlayer = true;
        var detail = await ReadInitialAsync("GetPlayerAsync", () => Gateway.GetPlayerAsync(ClubId, id, Token));
        PlayerTryouts = (detail.Tryouts ?? []).Where(value => _review?.Tryouts.Any(tryout => tryout.Id == value.Id) == true).OrderByDescending(value => value.Date).ToArray();
        _playerLoaded = true;
        _tryoutId = PlayerTryouts.Length > 0 ? PlayerTryouts[0].Id : Guid.Empty;
        if (_tryoutId != Guid.Empty) { _notebook = await ReadInitialAsync("GetNotebookAsync", () => Gateway.GetNotebookAsync(ClubId, _tryoutId, id, Token)); }
    });

    private Task LoadNotebookAsync() => ExecuteAsync(async () =>
    {
        _notebook = null;
        if (SelectedTryout is not null) { _notebook = await ReadInitialAsync("GetNotebookAsync", () => Gateway.GetNotebookAsync(ClubId, _tryoutId, _selectedId, Token)); }
    });

    private void ResetPreview() => _previewTeam = Guid.Empty;
    private void Preview(Guid teamId) => _previewTeam = teamId;
    private void ShowRosters() { _showNotebook = false; _focusRosters = true; }
    private void ShowPlayer() { _showNotebook = true; _focusPlayer = true; }

    private IReadOnlyList<PlayerSummary> CoveragePlayers(SeasonTeamRoster roster)
    {
        if (_previewTeam == Guid.Empty || SelectedPlayer is not { } player) { return roster.Players; }
        var players = roster.Players.Where(value => value.Id != player.Id);
        return roster.Team.Id == _previewTeam ? players.Append(player).ToArray() : players.ToArray();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusPlayer) { _focusPlayer = false; await _heading.FocusAsync(); }
        if (_focusRosters) { _focusRosters = false; await _rosterHeading.FocusAsync(); }
    }
}
