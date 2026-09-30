using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Services;

namespace Pino.UI.Features.Sporting.Pages;

public sealed partial class TryoutReviewPage : SportPageBase
{
    [Parameter] public Guid TryoutId { get; set; }
    private TryoutReview? _review;
    private Guid? _editionId;
    private Guid _closeOperation = Guid.NewGuid();
    private bool _confirmed;
    private bool _confirmationStep;
    private bool _focusStep;
    private ElementReference _stepHeading;
    private static readonly string[] _steps = ["Review results", "Confirm closeout", "Saved results"];
    private int Step => (Edition is not null, _confirmationStep) switch { (true, _) => 2, (_, true) => 1, _ => 0 };
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusStep) { _focusStep = false; await _stepHeading.FocusAsync(); }
    }
    private void ReviewCloseout()
    {
        if (Disabled || _review is not { Tryout.Complete: true, Tryout.Closed: false, Season.Archived: false } || Edition is not null) { return; }
        _confirmationStep = true;
        _confirmed = false;
        _focusStep = true;
    }
    private void BackToResults() { _confirmationStep = false; _confirmed = false; _focusStep = true; }

    private string _reason = "";
    private string _query = "";
    private int _page;
    private void ResetPage() => _page = 0;
    private void ChangeEdition() { ResetPage(); BackToResults(); }
    private void ChangePage(int page) { _page = page; _focusStep = true; }
    private string _decision = "";
    private TryoutCloseoutSummary? Edition => _review?.Closeouts.FirstOrDefault(value => value.Id == _editionId);
    private string ExportUrl => $"/api/clubs/{ClubId}/sport/tryouts/{TryoutId}/results.csv" + (Edition is { } edition ? $"?editionId={edition.Id}" : "");
    private IEnumerable<TryoutResult> Results => (Edition?.Results ?? _review?.Results ?? [])
        .Where(value => $"{value.FullName} {value.Bib} {value.TeamName}".Contains(_query, StringComparison.OrdinalIgnoreCase) &&
            (_decision.Length == 0 || string.Equals(value.Decision.ToString(), _decision, StringComparison.Ordinal)));

    protected override async Task LoadAsync()
    {
        _page = 0;
        _review = await ReadInitialAsync("GetTryoutReviewAsync", () => Gateway.GetTryoutReviewAsync(ClubId, TryoutId, Token));
        _editionId = _review.Tryout.Closed ? _review.Closeouts.FirstOrDefault(value => value.ReopenedAt is null)?.Id : null;
        _confirmed = false;
        _confirmationStep = false;
        _closeOperation = Guid.NewGuid();
    }

    private async Task CloseAsync()
    {
        if (!_confirmationStep || !_confirmed || _review is null || _editionId is not null || !_review.Tryout.Complete) { return; }
        if (await SaveAsync(() => Gateway.CloseTryoutAsync(ClubId, TryoutId, new(_closeOperation, _review.ReviewToken), Token)))
        {
            await ExecuteAsync(LoadAsync);
            _focusStep = true;
        }
    }

    private async Task ReopenAsync()
    {
        if (Edition is not { ReopenedAt: null } edition) { return; }
        if (await SaveAsync(() => Gateway.ReopenTryoutAsync(ClubId, TryoutId, new(edition.Id, _reason), Token)))
        {
            _reason = "";
            await ExecuteAsync(LoadAsync);
            _focusStep = true;
        }
    }
}
