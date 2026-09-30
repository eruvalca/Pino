using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Models;
using Pino.UI.Features.Sporting.Services;

namespace Pino.UI.Features.Sporting.Pages;

public sealed partial class TryoutWork : SportPageBase
{
    [Parameter] public Guid TryoutId { get; set; }
    [SupplyParameterFromQuery(Name = "player")] private string? RequestedPlayer { get; set; }
    private string? _appliedPlayer;
    private TryoutDetail? _data;
    private PlayerNotebook? _notebook;
    private IReadOnlyList<NoteSummary> Notes => _notebook?.Notes ?? [];
    private IReadOnlyList<DecisionSummary> History => _notebook?.History ?? [];
    private Guid _selectedId;
    private readonly Dictionary<Guid, EvaluationDraft> _drafts = [];
    private string _query = "";
    private string _filter = "";
    private string _year = "";
    private string _teamFilter = "";
    private int _rosterPage;
    private bool _showRoster = true;
    private bool _enrolling;
    private PlayerPage? _candidates;
    private string _candidateQuery = "";
    private int _candidatePage;
    private string _bib = "";
    private ElementReference _playerHeading;
    private ElementReference _rosterHeading;
    private bool _focusPlayer;
    private bool _focusRoster;
    private Guid? _savedNoteId;
    private Guid? _redactingId;

    private bool CanEnroll => _data is { Season.Archived: false, Tryout.Closed: false };
    private bool HasPlayerNotice => !Failed && !_enrolling && Selected is not null && Message is not null && string.Equals(MessageKind, "success", StringComparison.Ordinal);
    private RosterEntry? Selected => _data?.Roster.FirstOrDefault(entry => entry.Player.Id == _selectedId);
    private EvaluationDraft? Draft => _drafts.GetValueOrDefault(_selectedId);
    private IEnumerable<RosterEntry> Filtered => (_data?.Roster ?? []).Where(Matches);
    private bool Matches(RosterEntry entry) =>
        $"{entry.Player.FullName} {entry.Player.PlayerReference} {entry.Bib}".Contains(_query, StringComparison.OrdinalIgnoreCase) &&
        (_filter.Length == 0 || string.Equals(entry.Decision.ToString(), _filter, StringComparison.Ordinal)) &&
        (_year.Length == 0 || string.Equals(entry.Player.GraduationYear.ToString(System.Globalization.CultureInfo.InvariantCulture), _year, StringComparison.Ordinal)) &&
        MatchesPlanning(entry) &&
        (_teamFilter.Length == 0 || (string.Equals(_teamFilter, "none", StringComparison.Ordinal) ? entry.CurrentTeamId is null : string.Equals(entry.CurrentTeamId?.ToString(), _teamFilter, StringComparison.Ordinal)));

    protected override async Task LoadAsync()
    {
        var data = await ReadInitialAsync("GetTryoutAsync", () => Gateway.GetTryoutAsync(ClubId, TryoutId, Token));
        if (_data?.Tryout.Id != data.Tryout.Id) { _drafts.Clear(); _selectedId = Guid.Empty; _appliedPlayer = null; }
        _data = data;
        _otherSeasons = (await ReadInitialAsync("GetOverviewAsync", () => Gateway.GetOverviewAsync(ClubId, Token))).Seasons.Where(value => value.Id != data.Season.Id).ToArray();
        if (_previousSeasonId != Guid.Empty) { await LoadPreviousTeamsAsync(); }
        if (!CanEnroll) { _enrolling = false; }
        if (!string.Equals(_appliedPlayer, RequestedPlayer, StringComparison.Ordinal))
        {
            _appliedPlayer = RequestedPlayer;
            if (Guid.TryParse(RequestedPlayer, out var requestedId) && data.Roster.Any(entry => entry.Player.Id == requestedId))
            {
                _selectedId = requestedId; _showRoster = false; _focusPlayer = true; ClearRosterFilters();
            }
            else if (!string.IsNullOrEmpty(RequestedPlayer)) { Message = "This player is not included in the tryout. Open their player record to see the history."; MessageKind = "warning"; }
        }
        if (!_data.Roster.Any(entry => entry.Player.Id == _selectedId)) { _selectedId = _data.Roster.Count > 0 ? _data.Roster[0].Player.Id : Guid.Empty; }
        foreach (var entry in _data.Roster) { _drafts.TryAdd(entry.Player.Id, new(entry)); }
        var retained = _data.Roster.Select(entry => entry.Player.Id).ToHashSet();
        foreach (var id in _drafts.Keys.Where(id => !retained.Contains(id)).ToArray()) { _drafts.Remove(id); }
        _rosterPage = Math.Min(_rosterPage, Math.Max(0, (Filtered.Count() - 1) / 50));
        await LoadNotebookAsync();
    }

    private async Task LoadNotebookAsync()
    {
        _notebook = null;
        if (_selectedId == Guid.Empty) { return; }
        _notebook = await ReadInitialAsync("GetNotebookAsync", () => Gateway.GetNotebookAsync(ClubId, TryoutId, _selectedId, Token));
        if (_data is not null)
        {
            _data = _data with { Roster = _data.Roster.Select(value => value.Player.Id == _selectedId ? value with { HasObservations = _notebook.Notes.Any(note => note.RedactedAt is null) } : value).ToArray() };
            _rosterPage = Math.Min(_rosterPage, Math.Max(0, (Filtered.Count() - 1) / 50));
        }
        ClearRedactedDrafts();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusPlayer) { _focusPlayer = false; await _playerHeading.FocusAsync(); }
        if (_focusRoster) { _focusRoster = false; await _rosterHeading.FocusAsync(); }
    }
    private Task SelectPlayerAsync(Guid id) => ExecuteAsync(async () =>
    {
        _selectedId = id; _showRoster = false; _focusPlayer = true; _redactingId = null;
        await LoadNotebookAsync();
    });
    private void ShowRoster() { _showRoster = true; _focusRoster = true; }
    private void ResetRosterPage() => _rosterPage = 0;
    private void ChangeRosterPage(int direction)
    {
        _rosterPage += direction;
        _focusRoster = true;
    }
    private Task RefreshDecisionsAsync() => ExecuteAsync(async () =>
    {
        await LoadAsync();
        foreach (var entry in _data!.Roster) { _drafts[entry.Player.Id].ReloadDecision(entry); }
        Message = "Saved decisions refreshed. Unsaved notes are still here."; MessageKind = "information";
    });
    private Task ToggleEnrollmentAsync() => ExecuteAsync(async () =>
    {
        _enrolling = CanEnroll && !_enrolling;
        if (_enrolling) { _candidates = await ReadInitialAsync("GetPlayersAsync", () => Gateway.GetPlayersAsync(ClubId, _candidateQuery, archived: false, _candidatePage, Token)); }
    });
    private Task SearchCandidatesAsync() { _candidatePage = 0; return LoadCandidatesAsync(); }
    private Task PreviousCandidatesAsync() { _candidatePage = Math.Max(0, _candidatePage - 1); return LoadCandidatesAsync(); }
    private Task NextCandidatesAsync() { _candidatePage++; return LoadCandidatesAsync(); }
    private Task LoadCandidatesAsync() => ExecuteAsync(async () => _candidates = await ReadInitialAsync("GetPlayersAsync", () => Gateway.GetPlayersAsync(ClubId, _candidateQuery, archived: false, _candidatePage, Token)));
    private async Task EnrollAsync(Guid playerId)
    {
        if (!CanEnroll) { return; }
        if (await SaveAsync(() => Gateway.EnrollAsync(ClubId, TryoutId, new(playerId, _bib), Token)))
        {
            _bib = "";
            await ExecuteAsync(LoadAsync);
        }
    }
    private async Task SaveBibNumberAsync()
    {
        if (Selected is not { } entry || Draft is not { } draft) { return; }
        var input = new BibNumberInput(entry.Player.Id, draft.BibNumber, draft.SavedBibNumber);
        if (await SaveAsync(() => Gateway.SaveBibNumberAsync(ClubId, TryoutId, input, Token)))
        {
            draft.ReloadBibNumber(input.BibNumber.Trim());
            await ExecuteAsync(LoadAsync);
        }
    }
    private Task ReloadBibNumberAsync() => ExecuteAsync(async () =>
    {
        await LoadAsync();
        if (Selected is { } entry && Draft is { } draft) { draft.ReloadBibNumber(entry.Bib); }
        Message = "Saved bib number reloaded. Other drafts are still here."; MessageKind = "information";
    });
    private async Task SaveNoteAsync()
    {
        if (Selected is not { } entry || Draft is not { } draft) { return; }
        var input = new NoteInput(draft.NoteId, entry.Player.Id, draft.Note, draft.CorrectsId);
        if (!await ReconcileNoteAsync(draft, input)) { return; }
        draft.LastNoteAttempt = input;
        if (await SaveAsync(() => Gateway.AddNoteAsync(ClubId, TryoutId, input, Token)))
        {
            _savedNoteId = input.Id;
            draft.Note = ""; draft.NoteId = Guid.NewGuid(); draft.CorrectsId = null;
            draft.LastNoteAttempt = null;
            await ExecuteAsync(LoadNotebookAsync);
        }
    }
    private async Task SaveDecisionAsync()
    {
        if (Selected is not { } entry || Draft is not { } draft) { return; }
        var input = new DecisionInput(draft.OperationId, entry.Player.Id, draft.Kind, draft.Kind == DecisionKind.Placed ? draft.TeamId : null, draft.Revision, draft.PlacementRevision, draft.Reason);
        if (!await ReconcileDecisionAsync(draft, input)) { return; }
        draft.LastDecisionAttempt = input;
        if (await SaveAsync(() => Gateway.DecideAsync(ClubId, TryoutId, input, Token)))
        {
            await ExecuteAsync(LoadAsync);
            if (Selected is { } current) { draft.ReloadDecision(current); }
        }
    }

    private async Task PlacePreviousTeamAsync()
    {
        if (!CanEnroll || Selected is not { PreviousPlacement: { } previous } entry || Draft is not { } draft ||
            _data?.Teams.FirstOrDefault(value => value.Id == previous.TeamId) is not { Archived: false, Excluded: false } team ||
            entry.Player.GraduationYear < team.GraduationYear) { return; }
        draft.Kind = DecisionKind.Placed;
        draft.TeamId = previous.TeamId;
        await SaveDecisionAsync();
    }
    private void CorrectNote(NoteSummary note)
    {
        if (Draft is not { } draft) { return; }
        if (draft.Note.Length > 0) { Message = "Save or clear your current note before starting a correction."; MessageKind = "warning"; return; }
        draft.Note = note.Text; draft.CorrectsId = note.Id; draft.NoteId = Guid.NewGuid(); draft.LastNoteAttempt = null;
    }
    private void CancelCorrection()
    {
        if (Draft is { } draft) { draft.Note = ""; draft.CorrectsId = null; draft.NoteId = Guid.NewGuid(); draft.LastNoteAttempt = null; }
    }

    private async Task RedactNoteAsync(RedactNoteInput input)
    {
        if (await SaveAsync(() => Gateway.RedactNoteAsync(ClubId, TryoutId, input, Token)))
        {
            _redactingId = null;
            await ExecuteAsync(LoadNotebookAsync);
        }
    }

    private void ClearRedactedDrafts()
    {
        var removed = (_notebook?.Notes ?? []).Where(note => note.RedactedAt is not null).Select(note => note.Id).ToHashSet();
        foreach (var draft in _drafts.Values.Where(draft => (draft.CorrectsId is { } id && removed.Contains(id)) || removed.Contains(draft.NoteId)))
        {
            draft.Note = "";
            draft.CorrectsId = null;
            draft.NoteId = Guid.NewGuid();
            draft.LastNoteAttempt = null;
        }
    }

}
