using System.Text;
using Microsoft.AspNetCore.Components.Forms;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Services;

namespace Pino.UI.Features.Sporting.Pages;

public sealed partial class PlayerImport : SportPageBase
{
    private string? _csv;
    private string? _fileName;
    private ImportReport? _report;
    private string PreviewKind => _report switch { { Saved: true } => "success", { CanImport: true } => "information", _ => "error" };
    protected override Task LoadAsync() => Task.CompletedTask;
    private Task ChooseAsync(InputFileChangeEventArgs args) => ExecuteAsync(async () =>
    {
        _report = null; _csv = null; _fileName = null;
        if (args.File.Size > 1024 * 1024) { Message = "Choose a CSV no larger than 1 MB."; MessageKind = "error"; return; }
        await using var stream = args.File.OpenReadStream(1024 * 1024, Token);
        using var reader = new StreamReader(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true));
        _csv = await reader.ReadToEndAsync(Token);
        _fileName = args.File.Name;
        _report = await Gateway.ImportAsync(ClubId, new(_csv, Commit: false), Token);
    });
    private Task PreviewAsync() => ExecuteAsync(async () =>
    {
        if (_csv is not null) { _report = await Gateway.ImportAsync(ClubId, new(_csv, Commit: false), Token); }
    });
    private Task ImportAsync() => ExecuteAsync(async () =>
    {
        if (_csv is not null && _report is { CanImport: true, Saved: false }) { _report = await Gateway.ImportAsync(ClubId, new(_csv, Commit: true), Token); }
    });
}
