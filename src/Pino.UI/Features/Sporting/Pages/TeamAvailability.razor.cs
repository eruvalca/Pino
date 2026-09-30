using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Clubs;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Services;

namespace Pino.UI.Features.Sporting.Pages;

public sealed partial class TeamAvailability : SportPageBase
{
    [Parameter] public Guid SeasonId { get; set; }
    [SupplyParameterFromQuery(Name = "tryout")] public Guid? TryoutId { get; set; }
    private SeasonSummary? _season;
    private TryoutSummary? _tryout;
    private IReadOnlyList<TeamAvailabilitySummary> _teams = [];
    private string _query = "";
    private int _page;
    private bool CanEdit => _season is { Archived: false } && _tryout is not { Closed: true };
    private string BackUrl => _tryout is null ? $"/clubs/{ClubId}/seasons/{SeasonId}" : $"/clubs/{ClubId}/tryouts/{_tryout.Id}";
    private IEnumerable<TeamAvailabilitySummary> Filtered => _teams.Where(value => value.Team.Name.Contains(_query.Trim(), StringComparison.OrdinalIgnoreCase));

    protected override async Task LoadAsync()
    {
        if (Membership?.Role != ClubRole.Administrator) { throw new UnauthorizedAccessException(); }
        var overview = await Gateway.GetOverviewAsync(ClubId, Token);
        _season = overview.Seasons.SingleOrDefault(value => value.Id == SeasonId) ?? throw new KeyNotFoundException();
        _tryout = TryoutId is { } id ? overview.Tryouts.SingleOrDefault(value => value.Id == id && value.SeasonId == SeasonId) ?? throw new KeyNotFoundException() : null;
        _teams = await Gateway.GetTeamAvailabilityAsync(ClubId, SeasonId, TryoutId, Token);
        _page = Math.Min(_page, Math.Max(0, (Filtered.Count() - 1) / 50));
    }

    private async Task ToggleAsync(TeamAvailabilitySummary row)
    {
        var excluded = _tryout is null ? row.SeasonExcluded : row.TryoutExcluded;
        var revision = _tryout is null ? row.SeasonRevision : row.TryoutRevision;
        if (await SaveAsync(() => Gateway.SaveTeamAvailabilityAsync(ClubId, SeasonId, TryoutId, new(row.Team.Id, !excluded, revision), Token))) { await ExecuteAsync(LoadAsync); }
    }

    private bool IsAvailable(TeamAvailabilitySummary row) => !row.Team.Archived && !row.SeasonExcluded && (_tryout is null || !row.TryoutExcluded);
    private string Action(TeamAvailabilitySummary row)
    {
        var excluded = _tryout is null ? row.SeasonExcluded : row.TryoutExcluded;
        return excluded ? "Include team" : "Exclude team";
    }
    private string State(TeamAvailabilitySummary row) => row switch
    {
        { Team.Archived: true } => "Archived club team",
        { SeasonExcluded: true } => "Excluded for the season",
        { TryoutExcluded: true } when _tryout is not null => "Excluded for this tryout",
        _ => "Included",
    };
    private void ResetPage() => _page = 0;
    private void ChangePage(int page) => _page = page;
}
