using System.Diagnostics.CodeAnalysis;
using Pino.Features.Sporting.Data;
using Pino.Features.Sporting.Services;
using Pino.SharedKernel.Sporting;
using Shouldly;
using Xunit;

namespace Pino.UnitTests.Features.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit discovers public test classes.")]
public sealed class NoteRedactionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void RemovesEntireCorrectionChainFromAnyVersionAndPreservesUnrelatedNotes(int selected)
    {
        var club = Guid.NewGuid();
        var tryout = Guid.NewGuid();
        var player = Guid.NewGuid();
        var original = new PlayerNote { Id = Guid.NewGuid(), ClubId = club, TryoutId = tryout, PlayerId = player, Text = "Original sensitive text", Author = "Original author" };
        var correction = new PlayerNote { Id = Guid.NewGuid(), ClubId = club, TryoutId = tryout, PlayerId = player, Text = "Corrected sensitive text", CorrectsId = original.Id };
        var latest = new PlayerNote { Id = Guid.NewGuid(), ClubId = club, TryoutId = tryout, PlayerId = player, Text = "Latest text", CorrectsId = correction.Id };
        var unrelated = new PlayerNote { Id = Guid.NewGuid(), ClubId = club, TryoutId = tryout, PlayerId = player, Text = "Keep this separate observation" };
        var otherClub = new PlayerNote { Id = Guid.NewGuid(), ClubId = Guid.NewGuid(), TryoutId = tryout, PlayerId = player, Text = "Other club", CorrectsId = original.Id };
        PlayerNote[] chain = [original, correction, latest];
        var operation = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero);
        NoteRedaction.Apply([latest, unrelated, correction, original, otherClub], chain[selected], new(operation, chain[selected].Id, " Inappropriate personal content ", true), "admin-id", "Avery Admin", now);
        foreach (var note in chain)
        {
            note.Text.ShouldBeEmpty();
            note.RedactedAt.ShouldBe(now);
            note.RedactedById.ShouldBe("admin-id");
            note.RedactedBy.ShouldBe("Avery Admin");
            note.RedactionReason.ShouldBe("Inappropriate personal content");
            note.RedactionOperationId.ShouldBe(operation);
        }
        original.Author.ShouldBe("Original author");
        correction.CorrectsId.ShouldBe(original.Id);
        latest.CorrectsId.ShouldBe(correction.Id);
        unrelated.Text.ShouldBe("Keep this separate observation");
        unrelated.RedactedAt.ShouldBeNull();
        otherClub.Text.ShouldBe("Other club");
        otherClub.RedactedAt.ShouldBeNull();
    }
}
