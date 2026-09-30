using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Services;

namespace Pino.UI.Features.Sporting.Pages;

public sealed partial class PlayerRecord : SportPageBase
{
    [Parameter] public Guid PlayerId { get; set; }
    private PlayerDetail? _detail;
    private PlayerInput _model = new();
    private bool _editing;
    private bool _photoPending;

    protected override async Task LoadAsync()
    {
        _detail = PlayerId == Guid.Empty ? null : await Gateway.GetPlayerAsync(ClubId, PlayerId, Token);
        ResetModel();
        _editing = false;
    }

    private void ResetModel()
    {
        _photoPending = false;
        if (_detail is null) { _model = new(); return; }
        _model = new()
        {
            Id = _detail.Player.Id,
            Revision = _detail.Player.Revision,
            PlayerReference = _detail.Player.PlayerReference,
            FirstName = _detail.Player.FirstName,
            LastName = _detail.Player.LastName,
            GraduationYear = _detail.Player.GraduationYear,
            Position = _detail.Player.Position,
            ContactEmail = string.IsNullOrEmpty(_detail.Player.ContactEmail) ? null : _detail.Player.ContactEmail,
            Archived = _detail.Player.Archived,
        };
    }
    private void Edit() { ResetModel(); _editing = true; }
    private void CancelEdit() { ResetModel(); _editing = false; }
    private async Task SavePlayerAsync()
    {
        if (_photoPending) { Message = "Use this crop to review the photo before saving the player."; MessageKind = "warning"; return; }
        if (string.IsNullOrWhiteSpace(_model.ContactEmail)) { _model.ContactEmail = null; }
        if (await SaveAsync(() => Gateway.SavePlayerAsync(ClubId, _model, Token)))
        {
            if (PlayerId == Guid.Empty) { Navigation.NavigateTo($"/clubs/{ClubId}/players/{_model.Id}"); }
            else { await ReloadAsync(); }
        }
    }

    private void PhotoChanged(string? photo)
    {
        _model.Photo = photo;
        if (photo is not null) { _model.RemovePhoto = false; }
    }
}
