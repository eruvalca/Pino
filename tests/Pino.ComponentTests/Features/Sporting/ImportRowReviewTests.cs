using System.Diagnostics.CodeAnalysis;
using Bunit;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Components;
using Shouldly;
using Xunit;

namespace Pino.ComponentTests.Features.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit discovers public test classes.")]
public sealed class ImportRowReviewTests
{
    [Fact]
    public async Task ReplacementFormUsesSelectedCandidateNameAndEmitsTypedConfirmationAsync()
    {
        await using var context = new BunitContext();
        var candidate = new ImportCandidate(Guid.NewGuid(), "Avery Jo Reed", 2031, true, 7);
        var row = new ImportRow(2, "NEW-1", "Avery Jo Reed", 2031, null, Disposition: ImportDisposition.Review, Candidates: [candidate]);
        var choice = new ImportResolution(2, ImportDisposition.Replace, candidate.Id, candidate.Revision, Guid.NewGuid());
        ImportResolution? changed = null;
        var component = context.Render<ImportRowReview>(parameters => parameters
            .Add(value => value.Row, row)
            .Add(value => value.ClubId, Guid.NewGuid())
            .Add(value => value.Administrator, true)
            .Add(value => value.Resolution, choice)
            .Add(value => value.ResolutionChanged, value => { changed = value; }));
        component.Find("label[for='import-erase-2-name']").TextContent.ShouldBe("Type Avery Jo Reed to confirm");
        component.Find("#import-erase-2-name").GetAttribute("value").ShouldBeEmpty();
        await component.Find("#import-erase-2-name").InputAsync("Avery Jo Reed");
        changed.ShouldNotBeNull().Confirmation.ShouldBe("Avery Jo Reed");
        changed.CandidateId.ShouldBe(candidate.Id);
        changed.Revision.ShouldBe(7);
        changed.AcknowledgeHistory.ShouldBeFalse();
        changed.ErasureOperationId.ShouldBe(choice.ErasureOperationId);
    }
}
