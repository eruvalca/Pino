using System.Diagnostics.CodeAnalysis;
using Cropper.Blazor.Components;
using Cropper.Blazor.Models;
using Cropper.Blazor.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace Pino.UI.Components;

public sealed partial class PhotoEditor(IUrlImageInterop images) : IAsyncDisposable
{
    private const long MaxUpload = 5 * 1024 * 1024;
    private readonly Options _options = new() { AspectRatio = 1, ViewMode = ViewMode.Vm1, DragMode = "move", AutoCropArea = 0.85m, CropBoxMovable = false, CropBoxResizable = false, ZoomOnWheel = true, ZoomOnTouch = true };
    private readonly Dictionary<string, object> _imageAttributes = new(StringComparer.Ordinal) { ["alt"] = "Photo crop positioning area" };
    private CropperComponent? _cropper;
    private string? _source;
    private string? _preview;
    private string? _error;
    private bool _ready;
    private bool _editing;

    [Parameter, EditorRequired] public string InputId { get; set; } = "";
    [Parameter, EditorRequired] public string Label { get; set; } = "";
    [Parameter] public Uri? SavedPhotoUrl { get; set; }
    [Parameter] public string HelpText { get; set; } = "Only the square crop is kept.";
    [Parameter] public bool Disabled { get; set; }
    [Parameter, EditorRequired] public EventCallback<string?> CroppedPhotoChanged { get; set; }
    [Parameter, EditorRequired] public EventCallback<bool> PendingChanged { get; set; }

    private bool Processing { get; set; }
    private bool Unavailable => Disabled || Processing || !RendererInfo.IsInteractive;
    // Cropper exposes Action callbacks rather than Blazor EventCallbacks. Marshal
    // their render onto the dispatcher and let Blazor handle unexpected failures.
    [SuppressMessage("Major Code Smell", "S3168:Async methods should not return void", Justification = "Cropper requires an Action callback; this bridge awaits the dispatcher and forwards failures to Blazor.")]
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Every callback failure is forwarded to Blazor through DispatchExceptionAsync, never suppressed.")]
    private async void PhotoStateChanged(bool ready)
    {
        try
        {
            await InvokeAsync(() =>
            {
                _ready = ready;
                if (!ready) { _error = "This photo could not be read. Cancel or choose a valid JPEG or PNG."; }
                StateHasChanged();
            });
        }
        catch (Exception exception) { await DispatchExceptionAsync(exception); }
    }

    private async Task ChooseAsync(InputFileChangeEventArgs args)
    {
        if (Unavailable) { return; }
        _error = null;
        if (args.File.Size > MaxUpload || args.File.ContentType is not ("image/jpeg" or "image/png"))
        {
            _error = "Choose a JPEG or PNG no larger than 5 MB.";
            return;
        }
        Processing = true;
        await PendingChanged.InvokeAsync(true);
        try
        {
            var source = await images.GetImageUsingStreamingAsync(args.File, MaxUpload);
            await ReleaseSourceAsync();
            _source = source;
            _options.SetDataOptions = null;
            _ready = false;
            _editing = true;
        }
        catch (Exception exception) when (exception is IOException or JSException)
        {
            _error = "We couldn't open this photo. Choose a valid JPEG or PNG and try again.";
            await PendingChanged.InvokeAsync(_editing);
        }
        finally { Processing = false; }
    }

    private async Task ApplyAsync()
    {
        if (Unavailable || !_ready || _cropper is null) { return; }
        Processing = true;
        _error = null;
        try
        {
            var position = await _cropper.GetDataAsync(rounded: false);
            var receiver = await _cropper.GetCroppedCanvasDataInBackgroundAsync(new() { Width = 512, Height = 512, FillColor = "#ffffff" }, "image/jpeg", 0.85f, maximumReceiveChunkSize: 16000);
            await using var image = await receiver.GetImageChunkStreamAsync();
            var value = Convert.ToBase64String(image.ToArray());
            _preview = "data:image/jpeg;base64," + value;
            await CroppedPhotoChanged.InvokeAsync(value);
            // Recreate the editor at its current size when adjustment resumes;
            // a hidden responsive Cropper can lose its geometry during resize.
            _options.SetDataOptions = new() { X = position.X, Y = position.Y, Width = position.Width, Height = position.Height, Rotate = position.Rotate, ScaleX = position.ScaleX, ScaleY = position.ScaleY };
            _editing = false;
            await PendingChanged.InvokeAsync(false);
        }
        catch (Exception exception) when (exception is IOException or JSException or InvalidOperationException)
        {
            _error = "We couldn't prepare this crop. Try again or choose another photo. Your saved photo has not changed.";
        }
        finally { Processing = false; }
    }

    private async Task AdjustAsync()
    {
        if (Unavailable) { return; }
        _ready = false;
        _editing = true;
        await PendingChanged.InvokeAsync(true);
    }

    private async Task CancelAsync()
    {
        if (Unavailable) { return; }
        await ReleaseSourceAsync();
        _preview = null;
        _editing = false;
        _ready = false;
        _error = null;
        await CroppedPhotoChanged.InvokeAsync(null);
        await PendingChanged.InvokeAsync(false);
    }

    private async Task ReleaseSourceAsync()
    {
        if (_source is not null) { await images.RevokeObjectUrlAsync(_source); _source = null; }
    }

    public async ValueTask DisposeAsync()
    {
        try { await ReleaseSourceAsync(); }
        catch (JSDisconnectedException) { /* The disconnected page owns the remaining browser resources. */ }
    }
}
