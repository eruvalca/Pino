using System.Diagnostics.CodeAnalysis;
using Bunit;
using Pino.UI.Components;
using Shouldly;
using Xunit;

namespace Pino.ComponentTests.Components;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit discovers public test classes.")]
public sealed class StaffAvatarTests
{
    [Theory]
    [InlineData("Avery Coach", "AC")]
    [InlineData("  Avery   Morgan Coach  ", "AC")]
    [InlineData("Élodie", "É")]
    [InlineData("", "?")]
    [InlineData("𐐀very Coach", "𐐀C")]
    public async Task MissingPhotoShowsReadableInitialsAsync(string name, string initials)
    {
        await using var context = new BunitContext();
        var component = context.Render<StaffAvatar>(parameters => parameters.Add(value => value.Name, name));
        component.Find(".staff-avatar").TextContent.ShouldBe(initials);
        component.Find(".staff-avatar").GetAttribute("aria-hidden").ShouldBe("true");
        component.Find(".staff-avatar").GetAttribute("data-size").ShouldBe("regular");
        component.FindAll("img").ShouldBeEmpty();
    }

    [Fact]
    public async Task FailedPhotoFallsBackAndNewPhotoCanLoadAsync()
    {
        await using var context = new BunitContext();
        var photo = new Uri("/api/clubs/photos/staff-one", UriKind.Relative);
        var component = context.Render<StaffAvatar>(parameters => parameters.Add(value => value.Name, "Avery Coach")
            .Add(value => value.PhotoUrl, photo).Add(value => value.Compact, true));
        component.Find("img").GetAttribute("src").ShouldBe(photo.ToString());
        component.Find("img").GetAttribute("alt").ShouldBeEmpty();
        component.Find(".staff-avatar").GetAttribute("data-size").ShouldBe("small");
        await component.Find("img").TriggerEventAsync("onerror", EventArgs.Empty);
        component.FindAll("img").ShouldBeEmpty();
        component.Find(".staff-avatar").TextContent.ShouldBe("AC");
        var replacement = new Uri("/api/clubs/photos/staff-two", UriKind.Relative);
        component.Render(parameters => parameters.Add(value => value.PhotoUrl, replacement).Add(value => value.Name, "Taylor Staff"));
        component.Find("img").GetAttribute("src").ShouldBe(replacement.ToString());
        await component.Find("img").TriggerEventAsync("onerror", EventArgs.Empty);
        component.Find(".staff-avatar").TextContent.ShouldBe("TS");
    }
}
