using Pino.SharedKernel.Sporting;
using Shouldly;

namespace Pino.BrowserTests;

public sealed partial class SportingWorkflowTests
{
    private static async Task VerifyBibNumbersAsync(BrowserSession session, string path, SportOverview overview)
    {
        var original = overview.Tryouts.Single();
        var tryoutPath = $"{path}/tryouts/{original.Id}";
        var before = await session.GetAsync<TryoutDetail>(tryoutPath);
        var player = before.Roster.Single(value => string.Equals(value.Player.FirstName, "Jordan", StringComparison.Ordinal));
        var second = (await session.GetAsync<PlayerPage>(path + "/players?query=NS-002&archived=false&page=0")).Players.ShouldHaveSingleItem();
        before.Roster.Single(value => value.Player.Id == second.Id).Bib.ShouldBe("22");
        var unenrolled = new PlayerInput { PlayerReference = "BIB-UNENROLLED", FirstName = "Unenrolled", LastName = "Player", GraduationYear = 2030 };
        (await session.PostAsync<SportReply>(path + "/players", unenrolled)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(tryoutPath + "/bib", new BibNumberInput(unenrolled.Id, "23", ""))).Kind.ShouldBe(SportReplyKind.Invalid);
        (await session.PostAsync<SportReply>(tryoutPath + "/bib", new BibNumberInput(player.Player.Id, "22", "17"))).Kind.ShouldBe(SportReplyKind.Invalid);
        (await session.PostAsync<SportReply>(tryoutPath + "/bib", new BibNumberInput(player.Player.Id, new('1', 21), "17"))).Kind.ShouldBe(SportReplyKind.Invalid);
        var edit = new BibNumberInput(player.Player.Id, " 41 ", "17");
        (await session.PostAsync<SportReply>(tryoutPath + "/bib", edit)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(tryoutPath + "/bib", edit)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(tryoutPath + "/bib", edit with { BibNumber = "42" })).Kind.ShouldBe(SportReplyKind.Conflict);
        var saved = await session.GetAsync<TryoutDetail>(tryoutPath);
        var entry = saved.Roster.Single(value => value.Player.Id == player.Player.Id);
        entry.Bib.ShouldBe("41");
        entry.Revision.ShouldBe(player.Revision);
        entry.Decision.ShouldBe(player.Decision);
        saved.History.Count.ShouldBe(before.History.Count);
        var longest = new string('1', 20);
        (await session.PostAsync<SportReply>(tryoutPath + "/bib", new BibNumberInput(player.Player.Id, longest, "41"))).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.GetAsync<TryoutDetail>(tryoutPath)).Roster.Single(value => value.Player.Id == player.Player.Id).Bib.ShouldBe(longest);
        (await session.PostAsync<SportReply>(tryoutPath + "/bib", new BibNumberInput(player.Player.Id, "", longest))).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.GetAsync<TryoutDetail>(tryoutPath)).Roster.Single(value => value.Player.Id == player.Player.Id).Bib.ShouldBeEmpty();
        (await session.PostAsync<SportReply>(tryoutPath + "/bib", new BibNumberInput(player.Player.Id, "17", ""))).Kind.ShouldBe(SportReplyKind.Saved);
        var later = (await session.GetAsync<SportOverview>(path + "/overview")).Tryouts.Single(value => string.Equals(value.Name, "Follow-up evaluation", StringComparison.Ordinal));
        (await session.GetAsync<TryoutDetail>($"{path}/tryouts/{later.Id}")).Roster.Single(value => value.Player.Id == player.Player.Id).Bib.ShouldBe("17");
    }
}
