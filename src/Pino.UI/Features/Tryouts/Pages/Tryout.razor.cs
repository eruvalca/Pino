using Pino.UI.Features.Tryouts.Models;

namespace Pino.UI.Features.Tryouts.Pages;

public sealed partial class Tryout
{
    private readonly SampleTryoutSession _session = new();
    private bool _showTeams;
    private bool _showRoster;
    private bool _loading;
    private int _playerFocusRequest;
    private int _rosterFocusRequest;

    protected override void OnInitialized() => _session.Load("standard");

    private void Refresh() => StateHasChanged();
    private void ShowEvaluation() => _showTeams = false;
    private void ShowTeams() => _showTeams = true;
    private Task ShowRosterAsync()
    {
        _showRoster = true;
        _rosterFocusRequest++;
        return Task.CompletedTask;
    }

    private Task OpenPlayerAsync(int id)
    {
        _session.Selected = _session.Players.First(player => player.Id == id);
        _playerFocusRequest++;
        _showTeams = false;
        _showRoster = false;
        return Task.CompletedTask;
    }

    private void RestoreRoster() => _session.Load("standard");

    private async Task ReloadAsync(string size)
    {
        _loading = true;
        await Task.Delay(500);
        _session.Load(size);
        _showTeams = false;
        _showRoster = false;
        _loading = false;
    }
}
