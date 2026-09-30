using Microsoft.Playwright;
using Shouldly;
using SkiaSharp;

namespace Pino.BrowserTests;

internal sealed partial class BrowserSession
{
    internal async Task<string> ConfirmPhotoAsync(string saveButton, string captureName)
    {
        var useCrop = Page.GetByRole(AriaRole.Button, new() { Name = "Use this crop", Exact = true });
        await Page.Locator(".crop-controls:not([disabled])").WaitForAsync();
        (await Page.GetByRole(AriaRole.Button, new() { Name = saveButton, Exact = true }).IsDisabledAsync()).ShouldBeTrue();
        var initialWidth = await Page.Locator(".cropper-canvas").EvaluateAsync<double>("element => element.getBoundingClientRect().width");
        await Page.Locator(".cropper-face").HoverAsync();
        await Page.Mouse.WheelAsync(0, -240);
        await Page.WaitForFunctionAsync("initial => document.querySelector('.cropper-canvas').getBoundingClientRect().width > initial", initialWidth);
        var zoomedWidth = await Page.Locator(".cropper-canvas").EvaluateAsync<double>("element => element.getBoundingClientRect().width");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Zoom out", Exact = true }).ClickAsync();
        await Page.WaitForFunctionAsync("zoomed => document.querySelector('.cropper-canvas').getBoundingClientRect().width < zoomed", zoomedWidth);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Move right", Exact = true }).FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await CapturePhotoStatesAsync(captureName + "-crop");
        await useCrop.ClickAsync();
        await Page.GetByText("Crop ready to save", new() { Exact = true }).WaitForAsync();
        var preview = await Page.Locator(".photo-result img").GetAttributeAsync("src");
        preview.ShouldNotBeNull().ShouldStartWith("data:image/jpeg;base64,");
        using var image = SKBitmap.Decode(Convert.FromBase64String(preview[(preview.IndexOf(',', StringComparison.Ordinal) + 1)..]));
        image.Width.ShouldBe(512);
        image.Height.ShouldBe(512);
        (await Page.GetByRole(AriaRole.Button, new() { Name = saveButton, Exact = true }).IsEnabledAsync()).ShouldBeTrue();
        await CapturePhotoStatesAsync(captureName + "-preview");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Adjust crop", Exact = true }).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = saveButton, Exact = true }).And(Page.Locator(":disabled")).WaitForAsync();
        (await Page.GetByRole(AriaRole.Button, new() { Name = saveButton, Exact = true }).IsDisabledAsync()).ShouldBeTrue();
        await Page.Locator(".cropper-face").WaitForAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Zoom in", Exact = true }).ClickAsync();
        await useCrop.ClickAsync();
        await Page.GetByText("Crop ready to save", new() { Exact = true }).WaitForAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = saveButton, Exact = true }).And(Page.Locator(":enabled")).WaitForAsync();
        (await Page.GetByRole(AriaRole.Button, new() { Name = saveButton, Exact = true }).IsEnabledAsync()).ShouldBeTrue();
        preview = (await Page.Locator(".photo-result img").GetAttributeAsync("src")).ShouldNotBeNull();
        return preview;
    }

    private async Task CapturePhotoStatesAsync(string name)
    {
        foreach (var (width, height, suffix) in new[] { (1440, 1000, "desktop"), (390, 844, "mobile"), (320, 844, "narrow") })
        {
            await Page.SetViewportSizeAsync(width, height);
            await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
            await Page.EvaluateAsync("window.scrollTo(0, 0)");
            // Cropper debounces responsive layout separately from CSS animation.
            // Wait for the rendered geometry to settle before recording it.
            await Page.WaitForFunctionAsync("""
                () => new Promise(resolve => {
                    let previous = '', stableSince = performance.now();
                    const check = () => {
                        const elements = [...document.querySelectorAll('.cropper-container, .cropper-crop-box, .cropper-canvas')];
                        if (!elements.length) { resolve(true); return; }
                        const geometry = JSON.stringify(elements.map(element => {
                            const r = element.getBoundingClientRect(); return [r.x, r.y, r.width, r.height];
                        }));
                        if (geometry !== previous) { previous = geometry; stableSince = performance.now(); }
                        if (performance.now() - stableSince >= 350) { resolve(true); return; }
                        requestAnimationFrame(check);
                    };
                    check();
                })
                """);
            await CaptureAsync(name + "-" + suffix);
        }
        await Page.SetViewportSizeAsync(1440, 1000);
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.NoPreference });
    }
}
