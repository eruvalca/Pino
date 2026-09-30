using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Clubs;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Services;

namespace Pino.UI.Features.Sporting.Pages;

public sealed partial class BulkEnrollment : SportPageBase
{
    [Parameter] public Guid TryoutId { get; set; }
    private TryoutDetail? _tryout;
    private IReadOnlyList<EnrollmentCandidate> _candidates = [];
    private Dictionary<Guid, string> _names = [];
    private readonly HashSet<Guid> _selected = [];
    private BulkEnrollmentInput? _input;
    private SportingBatchReport? _report;
    private string _query = "";
    private int? _year;
    private int _page;
    private bool _showEnrolled;
    private ElementReference _listHeading;
    private static readonly string[] _steps = ["Select players", "Review selection", "Enrollment results"];
    private bool _focusStep;
    private int Step => (_report?.Kind == SportReplyKind.Saved, _input is not null) switch { (true, _) => 2, (_, true) => 1, _ => 0 };
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusStep) { _focusStep = false; await _listHeading.FocusAsync(); }
    }
    private bool CanEdit => _tryout is { Tryout.Closed: false, Season.Archived: false };
    private IEnumerable<EnrollmentCandidate> Filtered => _candidates.Where(value => (_showEnrolled || !value.Enrolled) && (_year is null || value.Player.GraduationYear == _year) && value.Player.FullName.Contains(_query.Trim(), StringComparison.OrdinalIgnoreCase));

    protected override async Task LoadAsync()
    {
        if (Membership?.Role != ClubRole.Administrator) { throw new UnauthorizedAccessException(); }
        _tryout = await ReadInitialAsync("GetTryoutAsync", () => Gateway.GetTryoutAsync(ClubId, TryoutId, Token));
        _candidates = await ReadInitialAsync("GetEnrollmentCandidatesAsync", () => Gateway.GetEnrollmentCandidatesAsync(ClubId, TryoutId, Token));
        _names = _candidates.ToDictionary(value => value.Player.Id, value => value.Player.FullName);
        _selected.Clear();
        _input = null;
        _report = null;
        _page = 0;
        Message = null;
    }

    private void Select(Guid id, ChangeEventArgs args)
    {
        if (args.Value is true)
        {
            if (_selected.Count >= 1000) { Message = "A batch can contain up to 1,000 players."; MessageKind = "warning"; return; }
            _selected.Add(id);
        }
        else { _selected.Remove(id); }
    }

    private void SelectMatching()
    {
        var ids = Filtered.Where(value => !value.Enrolled).Select(value => value.Player.Id).ToHashSet();
        ids.UnionWith(_selected);
        if (ids.Count > 1000) { Message = "More than 1,000 players match. Choose a graduation year or a smaller selection."; MessageKind = "warning"; return; }
        _selected.UnionWith(ids);
    }

    private void Review()
    {
        if (Disabled || !CanEdit || _selected.Count == 0) { return; }
        _input = new(Guid.NewGuid(), _candidates.Where(value => _selected.Contains(value.Player.Id)).Select(value => new EnrollmentSelection(value.Player.Id, value.Player.Revision)).ToArray());
        _report = null;
        _page = 0;
        _focusStep = true;
    }

    private Task EnrollAsync() => ExecuteAsync(async () =>
    {
        if (_input is null) { return; }
        Message = null;
        _report = await Gateway.EnrollBulkAsync(ClubId, TryoutId, _input, Token);
        _focusStep = true;
        _page = 0;
    });

    private void ResetPage() => _page = 0;
    private async Task ChangePageAsync(int page) { _page = page; await _listHeading.FocusAsync(); }
    private void BackToSelection() { _input = null; _report = null; _page = 0; _focusStep = true; }
}
