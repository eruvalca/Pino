using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Clubs;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Services;

namespace Pino.UI.Features.Sporting.Pages;

public sealed partial class ReturningPlayers : SportPageBase
{
    [Parameter] public Guid TryoutId { get; set; }
    private TryoutDetail? _tryout;
    private SportOverview? _overview;
    private Guid? _sourceSeasonId;
    private IReadOnlyList<ReturningPlayerReview> _players = [];
    private Dictionary<Guid, string> _names = [];
    private readonly HashSet<Guid> _selected = [];
    private ReturningPlacementInput? _input;
    private SportingBatchReport? _report;
    private string _query = "";
    private string _status = "";
    private int _page;
    private ElementReference _listHeading;
    private bool CanEdit => _tryout is { Tryout.Closed: false, Season.Archived: false };
    private IEnumerable<ReturningPlayerReview> Filtered => _players.Where(value => value.Player.FullName.Contains(_query.Trim(), StringComparison.OrdinalIgnoreCase) &&
        (string.IsNullOrEmpty(_status) || (string.Equals(_status, "ready", StringComparison.Ordinal) == (value.Selection is not null))));

    protected override async Task LoadAsync()
    {
        if (Membership?.Role != ClubRole.Administrator) { throw new UnauthorizedAccessException(); }
        _tryout = await Gateway.GetTryoutAsync(ClubId, TryoutId, Token);
        _overview = await Gateway.GetOverviewAsync(ClubId, Token);
        _sourceSeasonId ??= _overview.Seasons.Where(value => value.StartsOn < _tryout.Season.StartsOn).OrderByDescending(value => value.StartsOn).Select(value => (Guid?)value.Id).FirstOrDefault();
        await LoadPlayersAsync();
    }

    private async Task LoadPlayersAsync()
    {
        _players = [];
        _names.Clear();
        _selected.Clear();
        _input = null;
        _report = null;
        _page = 0;
        Message = null;
        if (_sourceSeasonId is not { } sourceId) { return; }
        _players = await Gateway.GetReturningPlayersAsync(ClubId, TryoutId, sourceId, Token);
        _names = _players.ToDictionary(value => value.Player.Id, value => value.Player.FullName);
    }

    private Task RefreshAsync() => ExecuteAsync(LoadPlayersAsync);
    private void Select(Guid id, ChangeEventArgs args)
    {
        if (args.Value is true)
        {
            if (_selected.Count >= 1000) { Message = "A batch can contain up to 1,000 players."; MessageKind = "warning"; return; }
            _selected.Add(id);
        }
        else { _selected.Remove(id); }
    }

    private void SelectReady()
    {
        var ids = Filtered.Where(value => value.Selection is not null).Select(value => value.Player.Id).ToHashSet();
        ids.UnionWith(_selected);
        if (ids.Count > 1000) { Message = "More than 1,000 players match. Search for a smaller selection."; MessageKind = "warning"; return; }
        _selected.UnionWith(ids);
    }

    private void Review()
    {
        if (Disabled || !CanEdit || _sourceSeasonId is not { } sourceId || _selected.Count == 0) { return; }
        _input = new(Guid.NewGuid(), sourceId, _players.Where(value => _selected.Contains(value.Player.Id) && value.Selection is not null).Select(value => value.Selection!).ToArray());
        _report = null;
        _page = 0;
    }

    private Task PlaceAsync() => ExecuteAsync(async () =>
    {
        if (_input is null) { return; }
        Message = null;
        _report = await Gateway.PlaceReturningPlayersAsync(ClubId, TryoutId, _input, Token);
        _page = 0;
    });

    private void ResetPage() => _page = 0;
    private async Task ChangePageAsync(int page) { _page = page; await _listHeading.FocusAsync(); }
    private void BackToSelection() { _input = null; _report = null; _page = 0; }
}
