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
    private IReadOnlyList<string> _columns = [];
    private CsvColumnMapping? _mapping;
    private readonly Dictionary<int, ImportResolution> _resolutions = [];
    private Guid _operationId = Guid.NewGuid();
    private bool _needsPreview;
    private int _page;
    private bool _attentionOnly;
    private IEnumerable<ImportRow> FilteredRows => _report?.Rows.Where(value => !_attentionOnly || value.Error is not null || value.Disposition is ImportDisposition.Review or ImportDisposition.Replace or ImportDisposition.Reactivate) ?? [];
    private int FilteredCount => FilteredRows.Count();
    private IEnumerable<ImportRow> VisibleRows => FilteredRows.Skip(_page * 50).Take(50);
    private string PreviewKind => _report switch { { Saved: true } => "success", { CanImport: true } => "information", _ => "error" };
    protected override Task LoadAsync() => Task.CompletedTask;
    private Task ChooseAsync(InputFileChangeEventArgs args) => ExecuteAsync(async () =>
    {
        _report = null; _csv = null; _fileName = null; _columns = []; _mapping = null;
        ResetReview();
        if (args.File.Size > 4 * 1024 * 1024) { Message = "Choose a CSV no larger than 4 MB."; MessageKind = "error"; return; }
        await using var stream = args.File.OpenReadStream(4 * 1024 * 1024, Token);
        using var reader = new StreamReader(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true));
        _csv = await reader.ReadToEndAsync(Token);
        _fileName = args.File.Name;
        _report = await Gateway.ImportAsync(ClubId, new(_csv, Commit: false), Token);
        _columns = _report.Columns ?? [];
        _mapping = _report.Mapping;
    });
    private Task PreviewAsync() => ExecuteAsync(async () =>
    {
        if (_csv is not null)
        {
            _report = await Gateway.ImportAsync(ClubId, new(_csv, Commit: false, _mapping, _resolutions.Values.ToArray()), Token);
            _needsPreview = false;
            _page = 0;
        }
    });
    private Task ImportAsync() => ExecuteAsync(async () =>
    {
        if (_csv is not null && !_needsPreview && _report is { CanImport: true, Saved: false })
        {
            _report = await Gateway.ImportAsync(ClubId, new(_csv, Commit: true, _mapping, _resolutions.Values.ToArray()) { OperationId = _operationId }, Token);
            if (_report.Saved) { _csv = null; _resolutions.Clear(); _attentionOnly = false; _page = 0; }
        }
    });
    private void MappingChanged(CsvColumnMapping mapping) { _mapping = mapping; _report = null; ResetReview(); }
    private void ResetReview() { _resolutions.Clear(); _operationId = Guid.NewGuid(); _needsPreview = false; _page = 0; _attentionOnly = false; }
    private void ResolutionChanged(ImportResolution choice)
    {
        if (choice.Action == ImportDisposition.Create) { _resolutions.Remove(choice.Row); }
        else { _resolutions[choice.Row] = choice; }
        _needsPreview = true;
    }
    private void FilterChanged() => _page = 0;
}
