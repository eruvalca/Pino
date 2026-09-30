using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Services;

namespace Pino.UI.Features.Sporting.Pages;

public sealed partial class TryoutEnrollments : SportPageBase
{
    [Parameter] public Guid TryoutId { get; set; }
    private TryoutDetail? _tryout;
    private IReadOnlyList<EnrollmentDetail> _entries = [];
    private PlayerNotebook? _notebook;
    private Guid _selectedId;
    private Guid? _redactingId;
    private string _query = "";
    private string _status = "retained";
    private readonly HashSet<Guid> _selectedPlayers = [];
    private IReadOnlyList<EnrollmentDetail>? _reviewPlayers;
    private bool _reviewRemove;
    private Guid _reviewId;
    private SportingBatchReport? _batchReport;
    private Dictionary<Guid, string> _names = [];
    private int _page;
    private ElementReference _heading;
    private bool _focusHeading;
    private bool _focusBatch;
    private ElementReference _batchHeading;
    private static readonly string[] _batchSteps = ["Select players", "Review roster change", "Roster results"];
    private int BatchStep => (_batchReport?.Kind == SportReplyKind.Saved, _reviewPlayers is not null) switch { (true, _) => 2, (_, true) => 1, _ => 0 };
    private void BackToPlayers() { _reviewPlayers = null; _batchReport = null; _focusBatch = true; }

    private bool CanEdit => _tryout is { Tryout.Closed: false, Season.Archived: false };
    private EnrollmentDetail? Selected => _entries.FirstOrDefault(value => value.Entry.Player.Id == _selectedId);
    private IEnumerable<EnrollmentDetail> BatchSelection => _entries.Where(value => _selectedPlayers.Contains(value.Entry.Player.Id));
    private bool CanExclude => _selectedPlayers.Count > 0 && BatchSelection.All(value => !value.Removed);
    private bool CanRestore => _selectedPlayers.Count > 0 && BatchSelection.All(value => value.Removed);
    private IEnumerable<EnrollmentDetail> Filtered => _entries.Where(value =>
        $"{value.Entry.Player.FullName} {value.Entry.Bib}".Contains(_query, StringComparison.OrdinalIgnoreCase) &&
        (string.Equals(_status, "all", StringComparison.Ordinal) || value.Removed == string.Equals(_status, "removed", StringComparison.Ordinal)));

    protected override async Task LoadAsync()
    {
        _tryout = await ReadInitialAsync("GetTryoutAsync", () => Gateway.GetTryoutAsync(ClubId, TryoutId, Token));
        _entries = await ReadInitialAsync("GetEnrollmentsAsync", () => Gateway.GetEnrollmentsAsync(ClubId, TryoutId, Token));
        _names = _entries.ToDictionary(value => value.Entry.Player.Id, value => value.Entry.Player.FullName);
        _page = Math.Min(_page, Math.Max(0, (Filtered.Count() - 1) / 50));
        await LoadNotebookAsync();
    }

    private async Task LoadNotebookAsync()
    {
        _notebook = null;
        if (Selected is not null) { _notebook = await ReadInitialAsync("GetNotebookAsync", () => Gateway.GetNotebookAsync(ClubId, TryoutId, _selectedId, Token)); }
    }

    private Task SelectAsync(Guid id) => ExecuteAsync(async () =>
    {
        _selectedId = id;
        _redactingId = null;
        await LoadNotebookAsync();
        _focusHeading = true;
    });

    private async Task SaveCorrectionAsync(EnrollmentChangeInput input)
    {
        if (await SaveAsync(() => Gateway.ChangeEnrollmentAsync(ClubId, TryoutId, input, Token))) { await ExecuteAsync(LoadAsync); }
    }

    private void SelectPlayer(Guid id, ChangeEventArgs args)
    {
        if (args.Value is not true) { _selectedPlayers.Remove(id); return; }
        if (_selectedPlayers.Count >= 1000) { Message = "Choose up to 1,000 players per group."; MessageKind = "warning"; return; }
        _selectedPlayers.Add(id);
    }

    private void SelectMatching()
    {
        var ids = Filtered.Select(value => value.Entry.Player.Id).ToHashSet();
        ids.UnionWith(_selectedPlayers);
        if (ids.Count > 1000) { Message = "More than 1,000 players match. Narrow the filters first."; MessageKind = "warning"; return; }
        _selectedPlayers.UnionWith(ids);
    }

    private void ReviewBatch(bool remove)
    {
        if (Disabled || !CanEdit || !(remove ? CanExclude : CanRestore)) { return; }
        _reviewPlayers = BatchSelection.ToArray();
        _reviewRemove = remove;
        _reviewId = Guid.NewGuid();
        _batchReport = null;
        _focusBatch = true;
    }

    private Task SaveBatchAsync(BulkEnrollmentChangeInput input) => ExecuteAsync(async () =>
    {
        _batchReport = await Gateway.ChangeEnrollmentsAsync(ClubId, TryoutId, input, Token);
        _focusBatch = true;
        if (_batchReport.Kind != SportReplyKind.Saved) { return; }
        _reviewPlayers = null;
        _selectedPlayers.Clear();
        await LoadAsync();
    });

    private void ChangeStatus() { _selectedPlayers.Clear(); ResetPage(); }

    private async Task RedactAsync(RedactNoteInput input)
    {
        if (await SaveAsync(() => Gateway.RedactNoteAsync(ClubId, TryoutId, input, Token))) { _redactingId = null; await ExecuteAsync(LoadNotebookAsync); }
    }

    private void ResetPage() => _page = 0;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusBatch) { _focusBatch = false; await _batchHeading.FocusAsync(); }
        if (_focusHeading) { _focusHeading = false; await _heading.FocusAsync(); }
    }
}
