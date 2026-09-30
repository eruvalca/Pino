using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Services;

namespace Pino.UI.Features.Sporting.Pages;

public sealed partial class TeamRoster : SportPageBase
{
    [Parameter] public Guid TeamId { get; set; }
    private TeamDetail? _detail;
    protected override async Task LoadAsync() => _detail = await Gateway.GetTeamAsync(ClubId, TeamId, Token);
}
