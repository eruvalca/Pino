namespace Pino.UI.Features.Tryouts.Models;

// Fictional, per-page state. There is deliberately no API, persistence, or real club access here.
internal sealed class SampleTryoutSession
{
    internal IReadOnlyList<Team> Teams { get; } = [new(1, "Blue 2030", 2030), new(2, "Silver 2031", 2031), new(3, "Elite 2032", 2032)];
    internal List<SamplePlayer> Players { get; private set; } = [];
    internal SamplePlayer? Selected { get; set; }
    internal SampleCondition Condition { get; set; }
    internal bool Busy { get; private set; }
    internal string Search { get; set; } = "";
    internal string YearFilter { get; set; } = "";
    internal string DecisionFilter { get; set; } = "";
    internal string TeamFilter { get; set; } = "";
    internal int Remaining => Players.Count(player => !player.Decision.IsComplete);
    internal bool HasFilters => Search.Length > 0 || YearFilter.Length > 0 || DecisionFilter.Length > 0 || TeamFilter.Length > 0;
    internal IEnumerable<SamplePlayer> FilteredPlayers => Players.Where(MatchesFilter);

    internal void Load(string size)
    {
        Players = SampleRoster.Create(size, Teams);
        Selected = Players.FirstOrDefault();
        ClearFilters();
        Condition = SampleCondition.Available;
    }

    internal void ClearFilters()
    {
        Search = "";
        YearFilter = "";
        DecisionFilter = "";
        TeamFilter = "";
    }

    internal async Task<SampleWriteOutcome> SaveNoteAsync(SamplePlayer player)
    {
        var text = player.Draft.Trim();
        if (text.Length is 0 or > 2000)
        {
            return new SampleWriteOutcome.Failed("Write a note of 1 to 2,000 characters.");
        }

        var failure = await PrepareWriteAsync();
        if (failure is not null)
        {
            return failure;
        }

        player.Notes.Insert(0, new("Coach Sam (you)", DateTimeOffset.Now, text));
        player.Draft = "";
        return new SampleWriteOutcome.Saved("Note saved in this sample. Reloading or leaving clears it.");
    }

    internal async Task<SampleWriteOutcome> SaveDecisionAsync(SamplePlayer player, string kind, int? teamId, int expectedVersion)
    {
        var team = Teams.FirstOrDefault(team => team.Id == teamId);
        if (string.Equals(kind, "placed", StringComparison.Ordinal) && (team is null || player.GraduationYear < team.GraduationYear))
        {
            return new SampleWriteOutcome.Failed("Choose a team that accepts this player's graduation year.");
        }

        Decision? decision = kind switch
        {
            "placed" when team is not null => new Decision.Placement(team),
            "withdrawn" => new Decision.Withdrawal(),
            "not-selected" => new Decision.NonSelection(),
            _ => null,
        };
        if (decision is null)
        {
            return new SampleWriteOutcome.Failed("Choose a decision before saving.");
        }

        var failure = await PrepareWriteAsync();
        if (failure is not null)
        {
            return failure;
        }

        if (Condition == SampleCondition.ConcurrentChange)
        {
            Condition = SampleCondition.Available;
            RecordDecision(player, new Decision.Withdrawal(), "Coach Lee");
            return new SampleWriteOutcome.Conflict("Coach Lee recorded Withdrawn while you were working. Review the latest decision before saving. Your note draft is still here.");
        }

        if (player.Version != expectedVersion)
        {
            return new SampleWriteOutcome.Conflict("This decision changed. Load the latest decision, review it, then save again.");
        }

        RecordDecision(player, decision, "Coach Sam (you)");
        return new SampleWriteOutcome.Saved("Decision saved in this sample. Reloading or leaving clears it.");
    }

    private async Task<SampleWriteOutcome?> PrepareWriteAsync()
    {
        if (Busy)
        {
            return new SampleWriteOutcome.Failed("A save is already in progress. Please wait.");
        }

        Busy = true;
        await Task.Delay(300);
        Busy = false;
        if (Condition == SampleCondition.Disconnected)
        {
            return new SampleWriteOutcome.Failed("Connection lost. Your draft is still here. Restore the connection in Sample controls, then retry.");
        }

        if (Condition == SampleCondition.ReadOnly)
        {
            return new SampleWriteOutcome.Failed("You don't have permission to save. Your draft is still here. Ask a club administrator for access.");
        }

        if (Condition == SampleCondition.FailNextSave)
        {
            Condition = SampleCondition.Available;
            return new SampleWriteOutcome.Failed("The save failed. Your draft is still here. Try saving again.");
        }

        return null;
    }

    private static void RecordDecision(SamplePlayer player, Decision decision, string author)
    {
        player.Decision = decision;
        player.Version++;
        var outcome = decision.AssignedTeam is { } team ? $"Placed · {team.Name}" : decision.Label;
        player.History.Insert(0, new("Spring 2027", "Spring 2027 tryout", outcome, author, DateTimeOffset.Now));
    }

    private bool MatchesFilter(SamplePlayer player)
    {
        return (player.Name.Contains(Search.Trim(), StringComparison.OrdinalIgnoreCase) || player.Bib.ToString("00", System.Globalization.CultureInfo.InvariantCulture).Contains(Search.Trim(), StringComparison.Ordinal))
            && (YearFilter.Length == 0 || string.Equals(player.GraduationYear.ToString(System.Globalization.CultureInfo.InvariantCulture), YearFilter, StringComparison.Ordinal))
            && (DecisionFilter.Length == 0 || string.Equals(player.Decision.Kind, DecisionFilter, StringComparison.Ordinal))
            && (TeamFilter.Length == 0 || string.Equals(player.Decision.AssignedTeam?.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), TeamFilter, StringComparison.Ordinal));
    }
}
