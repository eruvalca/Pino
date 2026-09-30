using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Services;

namespace Pino.UI.Features.Sporting.Pages;

public sealed partial class Seasons : SportPageBase
{
    [Parameter] public Guid SeasonId { get; set; }
    private SportOverview? _overview;
    private IReadOnlyList<TeamSummary> _teams = [];
    private SeasonSummary? _selected;
    private SeasonInput? _seasonInput;
    private TeamInput? _teamInput;
    private TryoutInput? _tryoutInput;
    private static readonly string[] _tryoutSteps = ["Tryout details", "Review roster & teams"];
    private bool _reviewTryout;
    private string TryoutHeading => (_tryoutInput?.Revision > 0, _reviewTryout) switch
    {
        (true, _) => "Edit tryout",
        (_, true) => "Review your new tryout",
        _ => "New tryout details",
    };
    private ElementReference _tryoutHeading;
    private bool _focusTryout;
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusTryout && _tryoutInput is not null) { _focusTryout = false; await _tryoutHeading.FocusAsync(); }
    }
    private Task ReviewTryoutAsync(TryoutInput input) => ExecuteAsync(async () =>
    {
        _tryoutInput = input;
        _overview = await Gateway.GetOverviewAsync(ClubId, Token);
        _teams = (await Gateway.GetTeamAvailabilityAsync(ClubId, SeasonId, tryoutId: null, Token)).Select(value => value.Team with { Excluded = value.SeasonExcluded }).ToArray();
        _reviewTryout = true;
        _focusTryout = true;
    });
    private void BackToTryoutDetails() { _reviewTryout = false; _focusTryout = true; }

    protected override async Task LoadAsync()
    {
        _overview = await ReadInitialAsync("GetOverviewAsync", () => Gateway.GetOverviewAsync(ClubId, Token));
        _selected = _overview.Seasons.FirstOrDefault(season => season.Id == SeasonId);
        if (SeasonId != Guid.Empty && _selected is null) { throw new KeyNotFoundException(); }
        _teams = _selected is null ? _overview.Teams : (await ReadInitialAsync("GetTeamAvailabilityAsync", () => Gateway.GetTeamAvailabilityAsync(ClubId, SeasonId, tryoutId: null, Token))).Select(value => value.Team with { Excluded = value.SeasonExcluded }).ToArray();
        CloseForms();
    }
    private void CloseForms() { _seasonInput = null; _teamInput = null; _tryoutInput = null; _reviewTryout = false; }
    private void NewSeason() { CloseForms(); _seasonInput = new(); }
    private void NewTeam() { CloseForms(); _teamInput = new(); }
    private string TeamUrl(Guid teamId) => $"/clubs/{ClubId}/teams/{teamId}" + (_selected is null ? "" : $"?season={SeasonId}");
    private void NewTryout() { CloseForms(); _focusTryout = true; _tryoutInput = new() { SeasonId = SeasonId, Date = _selected?.StartsOn ?? DateOnly.FromDateTime(DateTime.UtcNow) }; }
    private void EditSeason()
    {
        if (_selected is null) { return; }
        CloseForms();
        _seasonInput = new() { Id = _selected.Id, Revision = _selected.Revision, Name = _selected.Name, StartsOn = _selected.StartsOn, EndsOn = _selected.EndsOn, Archived = _selected.Archived };
    }
    private void EditTeam(TeamSummary team)
    {
        CloseForms();
        _teamInput = new() { Id = team.Id, Revision = team.Revision, Name = team.Name, GraduationYear = team.GraduationYear, Archived = team.Archived };
    }
    private void EditTryout(TryoutSummary tryout)
    {
        CloseForms();
        _tryoutInput = new() { Id = tryout.Id, SeasonId = tryout.SeasonId, Revision = tryout.Revision, Name = tryout.Name, Date = tryout.Date, Location = tryout.Location };
    }
    private async Task SaveSeasonAsync(SeasonInput input)
    {
        if (await SaveAsync(() => Gateway.SaveSeasonAsync(ClubId, input, Token)))
        {
            if (SeasonId == Guid.Empty) { Navigation.NavigateTo($"/clubs/{ClubId}/seasons/{input.Id}"); }
            else { await ReloadAsync(); }
        }
    }
    private async Task SaveTeamAsync(TeamInput input)
    {
        if (await SaveAsync(() => Gateway.SaveTeamAsync(ClubId, input, Token))) { await ReloadAsync(); }
    }
    private async Task SaveTryoutAsync(TryoutInput input)
    {
        if (await SaveAsync(() => Gateway.SaveTryoutAsync(ClubId, input, Token)))
        {
            if (input.Revision == 0) { Navigation.NavigateTo($"/clubs/{ClubId}/tryouts/{input.Id}"); return; }
            await ReloadAsync();
        }
    }
}
