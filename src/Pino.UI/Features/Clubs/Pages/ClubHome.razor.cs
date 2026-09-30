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
        _overview = await Gateway.GetOverviewAsync(ClubId, Token);
        if (Membership?.Role == ClubRole.Administrator)
        {
            var players = Gateway.GetPlayersAsync(ClubId, "", false, 0, Token);
            var people = Clubs.GetPeopleAsync(ClubId, false, 0, Token);
            await Task.WhenAll(players, people);
            _hasPlayers = (await players).Players.Count > 0;
            _hasOtherStaff = (await people).People.Count > 1 || (await people).HasMore;
        }
    }
}
