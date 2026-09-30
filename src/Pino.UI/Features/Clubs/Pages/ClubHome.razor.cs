using Pino.SharedKernel.Clubs;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Services;

namespace Pino.UI.Features.Clubs.Pages;

public sealed partial class ClubHome : SportPageBase
{
    private SportOverview? _overview;
    private bool _hasPlayers;
    private bool _hasOtherStaff;
    private bool HasSeasonTeams => _overview?.Seasons.Any(season => !season.Archived) == true && _overview.Teams.Any(team => !team.Archived);
    private bool HasTryout => ActiveTryouts.Any();
    private bool HasRoster => ActiveTryouts.Any(value => value.Players > 0);
    private IEnumerable<TryoutSummary> ActiveTryouts => (_overview?.Tryouts ?? []).Where(value => _overview?.Seasons.Any(season => season.Id == value.SeasonId && !season.Archived) == true);

    protected override async Task LoadAsync()
    {
        _overview = await ReadInitialAsync("GetOverviewAsync", () => Gateway.GetOverviewAsync(ClubId, Token));
        if (Membership?.Role == ClubRole.Administrator)
        {
            _hasPlayers = _overview.ActivePlayers > 0;
            var people = await ReadInitialAsync("GetPeopleAsync", () => Clubs.GetPeopleAsync(ClubId, false, 0, Token));
            _hasOtherStaff = people.People.Count > 1 || people.HasMore;
        }
    }
}
