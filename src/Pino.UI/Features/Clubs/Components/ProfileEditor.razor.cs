using Cropper.Blazor.Components;
using Cropper.Blazor.Models;
using Cropper.Blazor.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

using Pino.SharedKernel.Clubs;

namespace Pino.UI.Features.Clubs.Components;

public sealed partial class ProfileEditor(IUrlImageInterop images) : IAsyncDisposable
{
    private const long MaxUpload = 5 * 1024 * 1024;
    private readonly ProfileInput _model = new();
    private readonly Options _options = new() { AspectRatio = 1, ViewMode = ViewMode.Vm1, DragMode = "move", AutoCropArea = 0.85m, CropBoxMovable = false, CropBoxResizable = false, ZoomOnWheel = false };
    private readonly Dictionary<string, object> _imageAttributes = new(StringComparer.Ordinal) { ["alt"] = "Profile photo crop preview" };
    private CropperComponent? _cropper;
    private string? _source;
    private string? _error;
    private bool _initialized;
    private bool _processing;
    private bool _ready;

    [Parameter, EditorRequired] public ProfileSummary Profile { get; set; } = default!;
    [Parameter, EditorRequired] public EventCallback<ProfileInput> Save { get; set; }
    [Parameter] public EventCallback Cancel { get; set; }
    [Parameter] public bool Busy { get; set; }

    private void PhotoReady() => _ready = true;
    private void PhotoFailed() { _ready = false; _error = "This photo could not be read. Choose a valid JPEG or PNG."; }

    protected override void OnParametersSet()
    {
        if (!_initialized)
        {
            _model.FirstName = Profile.FirstName;
            _model.LastName = Profile.LastName;
            _initialized = true;
        }
    }

    private async Task ChooseAsync(InputFileChangeEventArgs args)
    {
        if (_processing || Busy) { return; }
        _error = null;
        if (args.File.Size > MaxUpload || args.File.ContentType is not ("image/jpeg" or "image/png"))
        {
            _error = "Choose a JPEG or PNG no larger than 5 MB.";
            return;
        }
        _processing = true;
        try
        {
            await CancelCropAsync();
            _source = await images.GetImageUsingStreamingAsync(args.File, MaxUpload);
        }
        catch (Exception exception) when (exception is IOException or JSException)
        {
            _error = "We couldn't open this photo. Choose a valid JPEG or PNG and try again.";
        }
        finally { _processing = false; }
    }

    private async Task CancelCropAsync()
    {
        if (_source is not null)
        {
            await images.RevokeObjectUrlAsync(_source);
        }
        _source = null;
        _ready = false;
        _model.CroppedPhoto = null;
    }

    private async Task SaveAsync()
    {
        if (_processing || Busy) { return; }
        if (_source is not null && !_ready)
        {
            _error = "Wait for the photo to load before saving, or choose a different photo.";
            return;
        }
        _processing = true;
        try
        {
            _error = null;
            if (_source is not null && _cropper is not null)
            {
                var receiver = await _cropper.GetCroppedCanvasDataInBackgroundAsync(new() { Width = 512, Height = 512, FillColor = "#ffffff" }, "image/jpeg", 0.85f, maximumReceiveChunkSize: 16000);
                await using var image = await receiver.GetImageChunkStreamAsync();
                _model.CroppedPhoto = Convert.ToBase64String(image.ToArray());
            }
            if (!Profile.IsComplete && _model.CroppedPhoto is null)
            {
                _error = "Choose and crop a photo before saving your profile.";
                return;
            }
            await Save.InvokeAsync(_model);
        }
        catch (Exception exception) when (exception is IOException or JSException or InvalidOperationException)
        {
            _error = "We couldn't save the crop. Wait for the photo to load or choose it again. Your saved photo has not changed.";
        }
        finally { _processing = false; }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await CancelCropAsync();
        }
        catch (JSDisconnectedException)
        {
            // Browser resources are released with the disconnected page.
        }
    }
}
