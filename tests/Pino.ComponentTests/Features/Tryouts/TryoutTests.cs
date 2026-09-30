using System.Diagnostics.CodeAnalysis;
using Bunit;
using Microsoft.AspNetCore.Components;
using Shouldly;
using Xunit;
using TryoutPage = Pino.UI.Features.Tryouts.Pages.Tryout;

namespace Pino.ComponentTests.Features.Tryouts;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal",
    Justification = "xUnit requires public test classes for discovery.")]
public sealed class TryoutTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RosterNavigationWaitsForInteractivityAsync(bool interactive)
    {
        await using var context = new BunitContext();
        context.SetRendererInfo(new("Server", interactive));
        var page = context.Render<TryoutPage>();
        page.Find(".roster-back").HasAttribute("disabled").ShouldBe(!interactive);
    }

    [Fact]
    public async Task MissingDecisionAndTeamErrorsAreAssociatedWithTheirControlsAsync()
    {
        await using var context = new BunitContext();
        context.SetRendererInfo(new("Server", isInteractive: true));
        context.JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true).SetVoidResult();
        var page = context.Render<TryoutPage>();
        await page.Find(".decision-toggle").ClickAsync();
        await page.Find(".decision-section form").SubmitAsync();
        page.Find("#decision-feedback").TextContent.ShouldContain("Choose a decision");
        page.Find(".decision-section fieldset").GetAttribute("aria-invalid").ShouldBe("true");
        page.Find(".decision-section fieldset").GetAttribute("aria-describedby").ShouldBe("decision-feedback");
        await page.Find("#outcome-placed").ChangeAsync("placed");
        await page.Find(".decision-section form").SubmitAsync();
        page.Find("#decision-feedback").TextContent.ShouldContain("Choose a team");
        page.Find("#placement-team").GetAttribute("aria-invalid").ShouldBe("true");
        page.Find("#placement-team").GetAttribute("aria-describedby").ShouldBe("eligibility-help decision-feedback");
        await page.Find("#placement-team").ChangeAsync("2");
        page.Find("#placement-team").GetAttribute("aria-invalid").ShouldBe("false");
    }

    [Fact]
    public async Task FilteringPreservesWholeTryoutProgressAndSelectedPlayerAsync()
    {
        await using var context = new BunitContext();
        context.SetRendererInfo(new("Server", isInteractive: true));
        context.JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true).SetVoidResult();
        var page = context.Render<TryoutPage>();
        await page.Find("#player-search").InputAsync("Avery");
        page.FindAll(".player-row").Count.ShouldBe(1);
        page.Find(".progress-strip").TextContent.ShouldContain("8 awaiting decision");
        await page.Find("#decision-filter").ChangeAsync("placed");
        page.FindAll(".player-row").ShouldBeEmpty();
        page.Find(".no-matches").TextContent.ShouldContain("No matching players");
        page.Find("#player-heading").TextContent.ShouldBe("Avery Morgan");
    }

    [Theory]
    [InlineData("7", "Jordan Ellis")]
    [InlineData("07", "Jordan Ellis")]
    [InlineData(" 07 ", "Jordan Ellis")]
    [InlineData("14", "Avery Morgan")]
    public async Task BibSearchMatchesDisplayedAndUnpaddedNumbersAsync(string search, string expectedPlayer)
    {
        await using var context = new BunitContext();
        context.SetRendererInfo(new("Server", isInteractive: true));
        context.JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true).SetVoidResult();
        var page = context.Render<TryoutPage>();
        await page.Find("#player-search").InputAsync(search);
        page.FindAll(".player-row").Count.ShouldBe(1);
        page.Find(".player-row .player-summary strong").TextContent.ShouldBe(expectedPlayer);
        page.Find(".progress-strip").TextContent.ShouldContain("8 awaiting decision");
    }

    [Fact]
    public async Task FailedNoteRetainsDraftAndRetryAddsExactlyOneObservationAsync()
    {
        await using var context = new BunitContext();
        context.SetRendererInfo(new("Server", isInteractive: true));
        context.JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true).SetVoidResult();
        var page = context.Render<TryoutPage>();
        await page.Find("#shared-note").InputAsync("Keeps the passing lane open.");
        await page.Find("#sample-condition").ChangeAsync("FailNextSave");
        await page.Find(".note-composer form").SubmitAsync();
        await page.WaitForAssertionAsync(() => page.Find("#note-feedback").TextContent.ShouldContain("save failed"));
        page.Find("#shared-note").GetAttribute("value").ShouldBe("Keeps the passing lane open.");
        page.FindAll(".observation").Count.ShouldBe(2);
        await page.Find(".note-composer form").SubmitAsync();
        await page.WaitForAssertionAsync(() => page.FindAll(".observation").Count.ShouldBe(3));
        page.Find(".observation p").TextContent.ShouldBe("Keeps the passing lane open.");
        page.Find("#shared-note").GetAttribute("value").ShouldBe("");
        page.Find("#note-feedback").TextContent.ShouldContain("Note saved in this sample session");
    }

    [Fact]
    public async Task DraftStaysWithItsPlayerAcrossSelectionChangesAsync()
    {
        await using var context = new BunitContext();
        context.SetRendererInfo(new("Server", isInteractive: true));
        context.JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true).SetVoidResult();
        var page = context.Render<TryoutPage>();
        await page.Find("#shared-note").InputAsync("Avery draft");
        await page.FindAll(".player-row")[1].ClickAsync();
        page.Find("#player-heading").TextContent.ShouldBe("Jordan Ellis");
        page.Find("#shared-note").GetAttribute("value").ShouldBe("");
        await page.FindAll(".player-row")[0].ClickAsync();
        page.Find("#shared-note").GetAttribute("value").ShouldBe("Avery draft");
    }

    [Theory]
    [InlineData("withdrawn", "Withdrawn")]
    [InlineData("not-selected", "Not selected")]
    [InlineData("placed", "Placed")]
    public async Task EveryCompletedOutcomeCompletesSinglePlayerTryoutAsync(string kind, string label)
    {
        await using var context = new BunitContext();
        context.SetRendererInfo(new("Server", isInteractive: true));
        context.JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true).SetVoidResult();
        var page = context.Render<TryoutPage>();
        await page.Find("#sample-roster").ChangeAsync("single");
        await page.Find(".sample-fields button").ClickAsync();
        await page.Find(".decision-toggle").ClickAsync();
        await page.Find($"#outcome-{kind}").ChangeAsync(new ChangeEventArgs { Value = kind });
        if (string.Equals(kind, "placed", StringComparison.Ordinal))
        {
            await page.Find("#placement-team").ChangeAsync("2");
        }

        await page.Find(".decision-section form").SubmitAsync();
        await page.WaitForAssertionAsync(() => page.Find(".progress-strip").TextContent.ShouldContain("Tryout complete"));
        page.Find(".decision-badge").TextContent.ShouldBe(label);
        page.Find(".placement-history").TextContent.ShouldContain(label);
    }

    [Theory]
    [InlineData(1, "1", true)]
    [InlineData(1, "2", false)]
    [InlineData(0, "2", true)]
    [InlineData(0, "3", false)]
    [InlineData(2, "3", true)]
    [InlineData(7, "1", false)]
    public async Task GraduationEligibilityIsCheckedEvenForForgedSelectionAsync(int row, string team, bool eligible)
    {
        await using var context = new BunitContext();
        context.SetRendererInfo(new("Server", isInteractive: true));
        context.JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true).SetVoidResult();
        var page = context.Render<TryoutPage>();
        await page.FindAll(".player-row")[row].ClickAsync();
        await page.Find(".decision-toggle").ClickAsync();
        await page.Find("#outcome-placed").ChangeAsync("placed");
        page.Find($"#placement-team option[value='{team}']").HasAttribute("disabled").ShouldBe(!eligible);
        await page.Find("#placement-team").ChangeAsync(team);
        await page.Find(".decision-section form").SubmitAsync();
        await page.WaitForAssertionAsync(() => page.Find(".decision-badge").TextContent.ShouldBe(eligible ? "Placed" : "Awaiting decision"));
        if (!eligible)
        {
            page.Find(".decision-section .notice[data-kind='error']").TextContent.ShouldContain("Choose a team that accepts");
            page.Find(".progress-strip").TextContent.ShouldContain("8 awaiting decision");
        }
    }

    [Fact]
    public async Task ReplacingPlacementKeepsHistoryAndOnlyOneCurrentTeamAsync()
    {
        await using var context = new BunitContext();
        context.SetRendererInfo(new("Server", isInteractive: true));
        context.JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true).SetVoidResult();
        var page = context.Render<TryoutPage>();
        foreach (var team in new[] { "1", "2" })
        {
            await page.Find(".decision-toggle").ClickAsync();
            await page.Find("#outcome-placed").ChangeAsync("placed");
            await page.Find("#placement-team").ChangeAsync(team);
            await page.Find(".decision-section form").SubmitAsync();
        }

        page.Find(".assigned-team").TextContent.ShouldBe("Silver 2031 · Spring 2027");
        page.FindAll(".placement-history li").Count.ShouldBe(3);
        page.Find(".placement-history").TextContent.ShouldContain("Spring 2026");
        await page.FindAll(".view-button")[1].ClickAsync();
        var teams = page.FindAll(".team-section");
        teams[0].TextContent.ShouldNotContain("Avery Morgan");
        teams[1].TextContent.ShouldContain("Avery Morgan");
    }

    [Fact]
    public async Task SavingNotePreservesDecisionAndPendingConflictSimulationAsync()
    {
        await using var context = new BunitContext();
        context.SetRendererInfo(new("Server", isInteractive: true));
        context.JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true).SetVoidResult();
        var page = context.Render<TryoutPage>();
        await page.Find(".decision-toggle").ClickAsync();
        await page.Find("#outcome-not-selected").ChangeAsync("not-selected");
        await page.Find("#sample-condition").ChangeAsync("ConcurrentChange");
        await page.Find("#shared-note").InputAsync("Spots the open passing lane.");
        await page.Find(".note-composer form").SubmitAsync();

        page.Find("#note-feedback").TextContent.ShouldContain("Note saved in this sample session");
        page.FindAll(".observation").Count.ShouldBe(3);
        page.Find(".observation p").TextContent.ShouldBe("Spots the open passing lane.");
        page.Find("#shared-note").GetAttribute("value").ShouldBe("");
        page.Find(".decision-badge").TextContent.ShouldBe("Awaiting decision");
        page.Find(".progress-strip").TextContent.ShouldContain("8 awaiting decision");
        page.FindAll(".placement-history li").Count.ShouldBe(1);
        page.Find(".placement-history li strong").TextContent.ShouldBe("Placed · Silver 2031");

        await page.Find(".decision-section form").SubmitAsync();

        page.Find("#decision-feedback").TextContent.ShouldContain("Coach Lee recorded Withdrawn");
        page.Find(".decision-badge").TextContent.ShouldBe("Withdrawn");
        page.Find(".decision-actions button[type='submit']").HasAttribute("disabled").ShouldBeTrue();
        page.FindAll(".placement-history li").Count.ShouldBe(2);
        page.Find(".placement-history li strong").TextContent.ShouldBe("Withdrawn");
    }

    [Fact]
    public async Task ConcurrentDecisionRequiresReviewBeforeRetryAsync()
    {
        await using var context = new BunitContext();
        context.SetRendererInfo(new("Server", isInteractive: true));
        context.JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true).SetVoidResult();
        var page = context.Render<TryoutPage>();
        await page.Find(".decision-toggle").ClickAsync();
        await page.Find("#outcome-not-selected").ChangeAsync("not-selected");
        await page.Find("#sample-condition").ChangeAsync("ConcurrentChange");
        await page.Find(".decision-section form").SubmitAsync();
        page.Find(".decision-badge").TextContent.ShouldBe("Withdrawn");
        page.Find(".decision-actions button[type='submit']").HasAttribute("disabled").ShouldBeTrue();
        await page.Find(".decision-section .notice button").ClickAsync();
        await page.Find("#outcome-not-selected").ChangeAsync("not-selected");
        await page.Find(".decision-section form").SubmitAsync();
        page.Find(".decision-badge").TextContent.ShouldBe("Not selected");
        page.Find(".placement-history").TextContent.ShouldContain("Coach Lee");
    }

    [Theory]
    [InlineData("Disconnected", "Connection lost")]
    [InlineData("ReadOnly", "permission")]
    public async Task UnavailableWritesKeepNotesAndDraftIntactAsync(string condition, string message)
    {
        await using var context = new BunitContext();
        context.SetRendererInfo(new("Server", isInteractive: true));
        context.JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true).SetVoidResult();
        var page = context.Render<TryoutPage>();
        await page.Find("#shared-note").InputAsync("Keep this draft.");
        await page.Find("#sample-condition").ChangeAsync(condition);
        await page.Find(".note-composer form").SubmitAsync();
        page.Find("#note-feedback").TextContent.ShouldContain(message);
        page.FindAll(".observation").Count.ShouldBe(2);
        page.Find("#shared-note").GetAttribute("value").ShouldBe("Keep this draft.");
    }

    [Fact]
    public async Task EmptyRosterIsNotReportedAsCompletedAndCanBeRestoredAsync()
    {
        await using var context = new BunitContext();
        context.SetRendererInfo(new("Server", isInteractive: true));
        context.JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true).SetVoidResult();
        var page = context.Render<TryoutPage>();
        await page.Find("#sample-roster").ChangeAsync("empty");
        await page.Find(".sample-fields button").ClickAsync();
        page.Find(".empty-workspace").TextContent.ShouldContain("No players");
        page.Find(".progress-strip").TextContent.ShouldNotContain("Tryout complete");
        await page.Find(".empty-workspace button").ClickAsync();
        page.FindAll(".player-row").Count.ShouldBe(16);
    }
}
