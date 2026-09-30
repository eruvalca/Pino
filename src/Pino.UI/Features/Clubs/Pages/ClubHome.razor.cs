using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Services;

namespace Pino.UI.Features.Clubs.Pages;

public sealed partial class ClubHome : SportPageBase
{
    private SportOverview? _overview;
    protected override async Task LoadAsync() => _overview = await Gateway.GetOverviewAsync(ClubId, Token);
}
