using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Clubs;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Services;

namespace Pino.UI.Features.Sporting.Pages;

public sealed partial class PlayerErase : SportPageBase
{
    [Parameter] public Guid PlayerId { get; set; }
    [Parameter] public Guid OperationId { get; set; }
    private PlayerSummary? _player;
    private ErasureReport? _result;
    private Guid _operationId = Guid.NewGuid();
    private string _confirmation = "";
    private bool _acknowledge;

    protected override async Task LoadAsync()
    {
        if (Membership?.Role != ClubRole.Administrator) { _player = null; return; }
        if (OperationId != Guid.Empty) { _operationId = OperationId; _result = await Gateway.GetErasureAsync(ClubId, OperationId, Token); return; }
        if (_result is { Kind: SportReplyKind.Saved }) { _result = await Gateway.GetErasureAsync(ClubId, _operationId, Token); return; }
        _player = (await Gateway.GetPlayerAsync(ClubId, PlayerId, Token)).Player;
        _operationId = Guid.NewGuid();
        _confirmation = "";
        _acknowledge = false;
    }

    private Task EraseAsync() => ExecuteAsync(async () =>
    {
        if (_player is null) { return; }
        _result = await Gateway.ErasePlayerAsync(ClubId, new(_operationId, PlayerId, _player.Revision, _confirmation, _acknowledge), Token);
        if (_result.Kind == SportReplyKind.Saved) { _player = null; _confirmation = ""; _acknowledge = false; Navigation.NavigateTo($"/clubs/{ClubId}/erasures/{_operationId}"); }
        else { Message = _result.Message; MessageKind = "error"; }
    });
    private Task CheckAsync() => ExecuteAsync(async () => _result = await Gateway.GetErasureAsync(ClubId, _operationId, Token));
}
