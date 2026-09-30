using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Services;

namespace Pino.UI.Features.Sporting.Pages;

public sealed partial class PlayerRecord : SportPageBase
{
    [Parameter] public Guid PlayerId { get; set; }
    private PlayerDetail? _detail;
    private PlayerInput _model = new();
    private bool _editing;
    private string? _photoName;

    protected override async Task LoadAsync()
    {
        _detail = PlayerId == Guid.Empty ? null : await Gateway.GetPlayerAsync(ClubId, PlayerId, Token);
        ResetModel();
        _editing = false;
    }

    private void ResetModel()
    {
        _photoName = null;
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
        if (string.IsNullOrWhiteSpace(_model.ContactEmail)) { _model.ContactEmail = null; }
        if (await SaveAsync(() => Gateway.SavePlayerAsync(ClubId, _model, Token)))
        {
            if (PlayerId == Guid.Empty) { Navigation.NavigateTo($"/clubs/{ClubId}/players/{_model.Id}"); }
            else { await ReloadAsync(); }
        }
    }

    private Task ChoosePhotoAsync(InputFileChangeEventArgs args) => ExecuteAsync(async () =>
    {
        if (args.File.Size > 5 * 1024 * 1024 || args.File.ContentType is not ("image/jpeg" or "image/png"))
        {
            Message = "Choose a JPEG or PNG no larger than 5 MB."; MessageKind = "error"; return;
        }
        await using var stream = args.File.OpenReadStream(5 * 1024 * 1024, Token);
        await using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, Token);
        _model.Photo = Convert.ToBase64String(buffer.ToArray());
        _model.RemovePhoto = false;
        _photoName = args.File.Name;
    });
}
