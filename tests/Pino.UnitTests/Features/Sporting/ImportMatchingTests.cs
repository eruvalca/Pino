using System.Diagnostics.CodeAnalysis;
using Pino.Features.Sporting.Data;
using Pino.Features.Sporting.Services;
using Pino.SharedKernel.Sporting;
using Shouldly;
using Xunit;

namespace Pino.UnitTests.Features.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit discovers public test classes.")]
public sealed class ImportMatchingTests
{
    private static Player Existing(bool archived = false) => new() { Id = Guid.NewGuid(), FirstName = "Avery", MiddleName = "Jo", LastName = "Reed", GraduationYear = 2031, PlayerReference = "OLD-1", Revision = 7, Archived = archived };
    private static PlayerInput Incoming() => new() { FirstName = "Avery", MiddleName = "Jo", LastName = "Reed", GraduationYear = 2031, PlayerReference = "NEW-1" };
    private static ImportRow Row() => new(2, "NEW-1", "Avery Jo Reed", 2031, null);

    [Fact]
    public void DifferentReferenceStillFindsActiveNameCandidateWithoutUpdatingIt()
    {
        var existing = Existing();
        var incoming = Incoming();
        incoming.FirstName = " AVERY ";
        incoming.ContactEmail = "new@example.test";
        var row = ImportMatching.Review(incoming, Row(), [existing], null);
        row.Disposition.ShouldBe(ImportDisposition.Skip);
        row.PlayerId.ShouldBe(existing.Id);
        row.Candidates.ShouldNotBeNull().ShouldHaveSingleItem().Revision.ShouldBe(7);
        existing.FirstName.ShouldBe("Avery");
        existing.ContactEmail.ShouldBeEmpty();
        existing.Revision.ShouldBe(7);
    }

    [Theory]
    [InlineData("Jo", 2031, true)]
    [InlineData("", 2031, true)]
    [InlineData("Pat", 2031, false)]
    [InlineData("Jo", 2030, false)]
    public void GraduationAndKnownMiddleNamesDistinguishCandidates(string middle, int year, bool matches)
    {
        var incoming = Incoming();
        incoming.MiddleName = middle;
        incoming.GraduationYear = year;
        ImportMatching.SameNames(incoming, Existing()).ShouldBe(matches);
    }

    [Fact]
    public void ArchivedIdentityRequiresExplicitCurrentCandidateAndCanBeSkipped()
    {
        var existing = Existing(archived: true);
        ImportMatching.Review(Incoming(), Row(), [existing], null).Disposition.ShouldBe(ImportDisposition.Review);
        ImportMatching.Review(Incoming(), Row(), [existing], new(2, ImportDisposition.Skip)).Disposition.ShouldBe(ImportDisposition.Skip);
        ImportMatching.Review(Incoming(), Row(), [existing], new(2, ImportDisposition.Reactivate, existing.Id, 6)).Disposition.ShouldBe(ImportDisposition.Review);
        var restored = ImportMatching.Review(Incoming(), Row(), [existing], new(2, ImportDisposition.Reactivate, existing.Id, 7));
        restored.Disposition.ShouldBe(ImportDisposition.Reactivate);
        restored.PlayerId.ShouldBe(existing.Id);
        existing.Archived.ShouldBeTrue("Reviewing a choice must not modify the record.");
    }

    [Theory]
    [InlineData("Avery Jo Reed", true, true)]
    [InlineData("Avery Reed", true, false)]
    [InlineData("Avery Jo Reed", false, false)]
    public void ReplacementRequiresExactArchivedNameAndHistoryAcknowledgement(string confirmation, bool acknowledge, bool allowed)
    {
        var existing = Existing(archived: true);
        var operationId = Guid.NewGuid();
        var result = ImportMatching.Review(Incoming(), Row(), [existing], new(2, ImportDisposition.Replace, existing.Id, 7, operationId, confirmation, acknowledge));
        result.Disposition.ShouldBe(allowed ? ImportDisposition.Replace : ImportDisposition.Review);
        if (allowed) { result.ErasureOperationId.ShouldBe(operationId); }
    }

    [Fact]
    public void ReferenceCollisionDoesNotAuthorizeErasingADifferentNameOrSeveralCandidates()
    {
        var existing = Existing(archived: true);
        var incoming = Incoming();
        incoming.PlayerReference = existing.PlayerReference;
        incoming.FirstName = "Someone";
        var choice = new ImportResolution(2, ImportDisposition.Replace, existing.Id, 7, Guid.NewGuid(), "Avery Jo Reed", true);
        ImportMatching.Review(incoming, Row(), [existing], choice).Disposition.ShouldBe(ImportDisposition.Review);
        ImportMatching.Review(Incoming(), Row(), [existing, Existing(archived: true)], choice).Disposition.ShouldBe(ImportDisposition.Review);
        ImportMatching.Review(Incoming(), Row(), [existing], choice with { ErasureOperationId = Guid.Empty }).Disposition.ShouldBe(ImportDisposition.Review);
    }

    [Fact]
    public void ParsingErrorsAndUnresolvedRowsBlockCommitWhileSkippedDuplicatesDoNot()
    {
        var error = ImportMatching.Review(Incoming(), Row() with { Error = "Missing year" }, [], null);
        error.Disposition.ShouldBe(ImportDisposition.Error);
        new ImportReport([error], false, "").CanImport.ShouldBeFalse();
        new ImportReport([Row() with { Disposition = ImportDisposition.Review }], false, "").CanImport.ShouldBeFalse();
        new ImportReport([Row(), Row() with { Disposition = ImportDisposition.Skip }], false, "").CanImport.ShouldBeTrue();
        new ImportReport([Row()], true, "").CanImport.ShouldBeFalse();
    }
}
