using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Services;

namespace Pino.UI.Features.Sporting.Pages;

public sealed partial class Players : SportPageBase
{
    private PlayerPage? _players;
    private string _query = "";
    private bool _archived;
    private int _page;
    private string EmptyHeading => (_query.Length > 0, _archived) switch { (true, _) => "No matching players", (_, true) => "No archived players", _ => "Your roster starts here" };
    protected override async Task LoadAsync() => _players = await Gateway.GetPlayersAsync(ClubId, _query, _archived, _page, Token);
    private Task SearchAsync() { _page = 0; return ReloadAsync(); }
    private Task PreviousAsync() { _page = Math.Max(0, _page - 1); return ReloadAsync(); }
    private Task NextAsync() { _page++; return ReloadAsync(); }
}
