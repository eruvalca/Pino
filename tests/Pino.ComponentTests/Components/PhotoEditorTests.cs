using System.Diagnostics.CodeAnalysis;
using Bunit;
using Cropper.Blazor.Components;
using Cropper.Blazor.Services;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Pino.UI.Components;
using Shouldly;
using Xunit;

namespace Pino.ComponentTests.Components;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit discovers public test classes.")]
[SuppressMessage("Usage", "xUnit1051:Use TestContext.Current.CancellationToken", Justification = "Substitute calls match the cancellation token supplied by the photo component.")]
public sealed class PhotoEditorTests
{
    private static IUrlImageInterop Configure(BunitContext context, bool interactive = true)
    {
        var images = Substitute.For<IUrlImageInterop>();
        images.GetImageUsingStreamingAsync(Arg.Any<IBrowserFile>(), Arg.Any<long>()).Returns("blob:photo-crop");
        context.Services.AddSingleton(images);
        context.Services.AddSingleton(Substitute.For<ICropperJsInterop>());
        context.SetRendererInfo(new("Server", interactive));
        return images;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FileSelectionWaitsForInteractiveRendererAsync(bool interactive)
    {
        await using var context = new BunitContext();
        Configure(context, interactive);
        var editor = context.Render<PhotoEditor>(parameters => parameters
            .Add(value => value.InputId, "test-photo").Add(value => value.Label, "Player photo")
            .Add(value => value.CroppedPhotoChanged, _ => { }).Add(value => value.PendingChanged, _ => { }));
        editor.Find("#test-photo").HasAttribute("disabled").ShouldBe(!interactive);
        editor.Find("label").GetAttribute("for").ShouldBe("test-photo");
    }

    [Theory]
    [InlineData("image/gif", 50)]
    [InlineData("image/png", 5242881)]
    public async Task InvalidUploadKeepsSavedPhotoAndDoesNotStartCropAsync(string contentType, int size)
    {
        await using var context = new BunitContext();
        var images = Configure(context);
        var pending = new List<bool>();
        var changes = new List<string?>();
        var editor = context.Render<PhotoEditor>(parameters => parameters
            .Add(value => value.InputId, "test-photo").Add(value => value.Label, "Player photo")
            .Add(value => value.SavedPhotoUrl, new Uri("/saved-photo", UriKind.Relative))
            .Add(value => value.CroppedPhotoChanged, changes.Add).Add(value => value.PendingChanged, pending.Add));
        editor.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary(new byte[size], "photo", contentType: contentType));
        await editor.WaitForAssertionAsync(() => editor.Find("[role='alert']").TextContent.ShouldContain("JPEG or PNG no larger than 5 MB"));
        editor.Find(".photo-result img").GetAttribute("src").ShouldBe("/saved-photo");
        editor.FindAll(".crop-editor").ShouldBeEmpty();
        pending.ShouldBeEmpty();
        changes.ShouldBeEmpty();
        await images.DidNotReceive().GetImageUsingStreamingAsync(Arg.Any<IBrowserFile>(), Arg.Any<long>());
    }

    [Fact]
    public async Task SelectingThenCancellingCropNotifiesParentAndReleasesSourceAsync()
    {
        await using var context = new BunitContext();
        var images = Configure(context);
        var pending = new List<bool>();
        var changes = new List<string?>();
        var editor = context.Render<PhotoEditor>(parameters => parameters
            .Add(value => value.InputId, "test-photo").Add(value => value.Label, "Player photo")
            .Add(value => value.SavedPhotoUrl, new Uri("/saved-photo", UriKind.Relative))
            .Add(value => value.CroppedPhotoChanged, changes.Add).Add(value => value.PendingChanged, pending.Add));
        editor.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary([1, 2, 3], "photo.png", contentType: "image/png"));
        await editor.WaitForAssertionAsync(() => editor.FindAll(".crop-editor").Count.ShouldBe(1));
        pending.ShouldBe([true]);
        changes.ShouldBeEmpty("Selecting a source must not submit the uncropped image.");
        editor.Find(".crop-controls").HasAttribute("disabled").ShouldBeTrue("The image is not ready yet.");
        await editor.FindAll("button").Single(button => string.Equals(button.TextContent, "Cancel photo change", StringComparison.Ordinal)).ClickAsync();
        await editor.WaitForAssertionAsync(() => editor.FindAll(".crop-editor").ShouldBeEmpty());
        pending.ShouldBe([true, false]);
        changes.ShouldHaveSingleItem().ShouldBeNull();
        editor.Find(".photo-result img").GetAttribute("src").ShouldBe("/saved-photo");
        await images.Received(1).RevokeObjectUrlAsync("blob:photo-crop");
    }

    [Fact]
    public async Task CropperCallbacksRefreshControlsAndLoadErrorsAsync()
    {
        await using var context = new BunitContext();
        Configure(context);
        var editor = context.Render<PhotoEditor>(parameters => parameters
            .Add(value => value.InputId, "test-photo").Add(value => value.Label, "Player photo")
            .Add(value => value.CroppedPhotoChanged, _ => { }).Add(value => value.PendingChanged, _ => { }));
        editor.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary([1, 2, 3], "photo.png", contentType: "image/png"));
        await editor.WaitForAssertionAsync(() => editor.Find(".crop-controls").HasAttribute("disabled").ShouldBeTrue());
        var cropper = editor.FindComponent<CropperComponent>();
        await editor.InvokeAsync(() => cropper.Instance.OnReadyEvent!.Invoke(null!));
        await editor.WaitForAssertionAsync(() => editor.Find(".crop-controls").HasAttribute("disabled").ShouldBeFalse());
        await editor.InvokeAsync(() => cropper.Instance.OnErrorLoadImageEvent!.Invoke(new()));
        await editor.WaitForAssertionAsync(() =>
        {
            editor.Find(".crop-controls").HasAttribute("disabled").ShouldBeTrue();
            editor.Find("[role='alert']").TextContent.ShouldContain("Cancel or choose a valid JPEG or PNG");
        });
    }

    [Fact]
    public async Task FailedSourceLoadRestoresSaveAvailabilityAndReportsRecoveryAsync()
    {
        await using var context = new BunitContext();
        var images = Configure(context);
        images.GetImageUsingStreamingAsync(Arg.Any<IBrowserFile>(), Arg.Any<long>()).Returns<string>(_ => throw new IOException("Unavailable"));
        var pending = new List<bool>();
        var editor = context.Render<PhotoEditor>(parameters => parameters
            .Add(value => value.InputId, "test-photo").Add(value => value.Label, "Player photo")
            .Add(value => value.CroppedPhotoChanged, _ => { }).Add(value => value.PendingChanged, pending.Add));
        editor.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromBinary([1], "photo.png", contentType: "image/png"));
        await editor.WaitForAssertionAsync(() => editor.Find("[role='alert']").TextContent.ShouldContain("Choose a valid JPEG or PNG and try again"));
        pending.ShouldBe([true, false]);
        editor.Find("#test-photo").HasAttribute("disabled").ShouldBeFalse();
    }
}
