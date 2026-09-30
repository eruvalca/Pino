using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Models;
using Pino.UI.Features.Sporting.Services;

namespace Pino.UI.Features.Sporting.Pages;

public sealed partial class TryoutWork : SportPageBase
{
    [Parameter] public Guid TryoutId { get; set; }
    private TryoutDetail? _data;
    private Guid _selectedId;
    private readonly Dictionary<Guid, EvaluationDraft> _drafts = [];
    private string _query = "";
    private string _filter = "";
    private string _year = "";
    private string _teamFilter = "";
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

    private bool CanEnroll => _data is { Season.Archived: false, Tryout.Closed: false };
    private RosterEntry? Selected => _data?.Roster.FirstOrDefault(entry => entry.Player.Id == _selectedId);
    private EvaluationDraft? Draft => _drafts.GetValueOrDefault(_selectedId);
    private IEnumerable<RosterEntry> Filtered => (_data?.Roster ?? []).Where(Matches);
    private bool Matches(RosterEntry entry) =>
        $"{entry.Player.FullName} {entry.Player.PlayerReference} {entry.Bib}".Contains(_query, StringComparison.OrdinalIgnoreCase) &&
        (_filter.Length == 0 || string.Equals(entry.Decision.ToString(), _filter, StringComparison.Ordinal)) &&
        (_year.Length == 0 || string.Equals(entry.Player.GraduationYear.ToString(System.Globalization.CultureInfo.InvariantCulture), _year, StringComparison.Ordinal)) &&
        (_teamFilter.Length == 0 || (string.Equals(_teamFilter, "none", StringComparison.Ordinal) ? entry.CurrentTeamId is null : string.Equals(entry.CurrentTeamId?.ToString(), _teamFilter, StringComparison.Ordinal)));

    protected override async Task LoadAsync()
    {
        var data = await Gateway.GetTryoutAsync(ClubId, TryoutId, Token);
        if (_data?.Tryout.Id != data.Tryout.Id) { _drafts.Clear(); _selectedId = Guid.Empty; }
        _data = data;
        if (!CanEnroll) { _enrolling = false; }
        if (!_data.Roster.Any(entry => entry.Player.Id == _selectedId)) { _selectedId = _data.Roster.Count > 0 ? _data.Roster[0].Player.Id : Guid.Empty; }
        foreach (var entry in _data.Roster) { _drafts.TryAdd(entry.Player.Id, new(entry)); }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusPlayer) { _focusPlayer = false; await _playerHeading.FocusAsync(); }
        if (_focusRoster) { _focusRoster = false; await _rosterHeading.FocusAsync(); }
    }
    private void SelectPlayer(Guid id) { _selectedId = id; _showRoster = false; _focusPlayer = true; }
    private void ShowRoster() { _showRoster = true; _focusRoster = true; }
    private Task RefreshDecisionsAsync() => ExecuteAsync(async () =>
    {
        await LoadAsync();
        foreach (var entry in _data!.Roster) { _drafts[entry.Player.Id].ReloadDecision(entry); }
        Message = "Saved decisions refreshed. Unsaved notes are still here."; MessageKind = "information";
    });
    private Task ToggleEnrollmentAsync() => ExecuteAsync(async () =>
    {
        _enrolling = CanEnroll && !_enrolling;
        if (_enrolling) { _candidates = await Gateway.GetPlayersAsync(ClubId, _candidateQuery, archived: false, _candidatePage, Token); }
    });
    private Task SearchCandidatesAsync() { _candidatePage = 0; return LoadCandidatesAsync(); }
    private Task PreviousCandidatesAsync() { _candidatePage = Math.Max(0, _candidatePage - 1); return LoadCandidatesAsync(); }
    private Task NextCandidatesAsync() { _candidatePage++; return LoadCandidatesAsync(); }
    private Task LoadCandidatesAsync() => ExecuteAsync(async () => _candidates = await Gateway.GetPlayersAsync(ClubId, _candidateQuery, archived: false, _candidatePage, Token));
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
            await ExecuteAsync(LoadAsync);
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
}
