using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Clubs;

namespace Pino.UI.Features.Clubs.Components;

public sealed partial class ProfileEditor
{
    private readonly ProfileInput _model = new();
    private string? _error;
    private bool _initialized;
    private bool _photoPending;

    [Parameter, EditorRequired] public ProfileSummary Profile { get; set; } = default!;
    [Parameter, EditorRequired] public EventCallback<ProfileInput> Save { get; set; }
    [Parameter] public EventCallback Cancel { get; set; }
    [Parameter] public bool Busy { get; set; }

    protected override void OnParametersSet()
    {
        if (!_initialized)
        {
            _model.FirstName = Profile.FirstName;
            _model.LastName = Profile.LastName;
            _initialized = true;
        }
    }

    private async Task SaveAsync()
    {
        if (Busy || !RendererInfo.IsInteractive) { return; }
        _error = null;
        if (_photoPending) { _error = "Use this crop to review your photo before saving the profile."; return; }
        if (!Profile.IsComplete && _model.CroppedPhoto is null)
        {
            _error = "Choose and crop a photo before saving your profile.";
            return;
        }
        await Save.InvokeAsync(_model);
    }
}
