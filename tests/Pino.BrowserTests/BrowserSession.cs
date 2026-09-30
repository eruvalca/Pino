using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Playwright;
using Npgsql;
using Pino.SharedKernel.Clubs;
using Shouldly;
using SkiaSharp;
using Xunit;

namespace Pino.BrowserTests;

internal sealed partial class BrowserSession(IPlaywright playwright, IBrowser browser, IBrowserContext context) : IAsyncDisposable
{
    internal const string Password = "Pino-test-Only!42";
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    private readonly List<string> _emails = [];
    private readonly List<string> _messages = [];
    internal IBrowserContext Context => context;
    internal IPage Page { get; private set; } = default!;
    internal Uri BaseUrl { get; } = new(Environment.GetEnvironmentVariable("PINO_BROWSER_URL")!);
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    internal static async Task<BrowserSession> CreateAsync()
    {
        var url = new Uri(Environment.GetEnvironmentVariable("PINO_BROWSER_URL")!);
        var database = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("PINO_TEST_DATABASE"));
        (url.Host.EndsWith(".localhost", StringComparison.Ordinal) || url.IsLoopback).ShouldBeTrue("Browser tests run only against local development.");
        database.Host.ShouldBeOneOf("localhost", "127.0.0.1", "::1");
        database.Database.ShouldBe("pino");
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, Channel = Environment.GetEnvironmentVariable("PINO_BROWSER_CHANNEL") });
        var context = await browser.NewContextAsync(new() { BaseURL = url.AbsoluteUri, IgnoreHTTPSErrors = true, ViewportSize = new() { Width = 1440, Height = 1000 } });
        context.SetDefaultTimeout(20000);
        var session = new BrowserSession(playwright, browser, context) { Page = await context.NewPageAsync() };
        return session;
    }

    internal async Task<string> RegisterAsync()
    {
        var email = $"pino-browser-{Guid.NewGuid():N}@example.test";
        _emails.Add(email);
        await Page.GotoAsync("/Account/Register");
        await Page.GetByLabel("Email", new() { Exact = true }).FillAsync(email);
        await Page.GetByLabel("Password", new() { Exact = true }).FillAsync(Password);
        await Page.GetByLabel("Confirm Password", new() { Exact = true }).FillAsync(Password);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Register", Exact = true }).ClickAsync();
        await Page.WaitForURLAsync("**/Account/RegisterConfirmation?**");
        await Page.GotoAsync(await EmailLinkAsync(email, "Confirm your Pino account"));
        await Page.GetByText("Thank you for confirming your email.").WaitForAsync();
        await LoginAsync(email, Password);
        return email;
    }

    internal async Task LoginAsync(string email, string password)
    {
        await Page.GotoAsync("/Account/Login");
        await Page.GetByLabel("Email", new() { Exact = true }).FillAsync(email);
        await Page.GetByLabel("Password", new() { Exact = true }).FillAsync(password);
        await Page.GetByRole(AriaRole.Button, new() { Name = "Log in", Exact = true }).ClickAsync();
        await Page.WaitForURLAsync(url => !url.Contains("/Account/Login", StringComparison.Ordinal));
    }

    internal async Task<string> EmailLinkAsync(string email, string subject)
    {
        using var http = new HttpClient { BaseAddress = new(Environment.GetEnvironmentVariable("PINO_MAILPIT_URL")!) };
        for (var attempt = 0; attempt < 40; attempt++)
        {
            using var list = JsonDocument.Parse(await http.GetStringAsync(new Uri("api/v1/messages", UriKind.Relative), Token));
            foreach (var item in list.RootElement.GetProperty("messages").EnumerateArray())
            {
                if (!string.Equals(item.GetProperty("Subject").GetString(), subject, StringComparison.Ordinal) ||
                    !item.GetProperty("To").EnumerateArray().Any(to => string.Equals(to.GetProperty("Address").GetString(), email, StringComparison.Ordinal))) { continue; }
                var id = item.GetProperty("ID").GetString()!;
                _messages.Add(id);
                using var message = JsonDocument.Parse(await http.GetStringAsync(new Uri($"api/v1/message/{id}", UriKind.Relative), Token));
                return message.RootElement.GetProperty("Text").GetString()!.Split('\n').Select(line => line.Trim()).Single(line => line.StartsWith("http", StringComparison.Ordinal));
            }
            await Task.Delay(250, Token);
        }
        throw new TimeoutException($"No {subject} mail arrived for the disposable account.");
    }

    internal static byte[] Photo()
    {
        using var bitmap = new SKBitmap(512, 512);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(30, 60, 105));
        using var paint = new SKPaint { Color = new SKColor(230, 190, 140), IsAntialias = true };
        canvas.DrawCircle(80, 50, 30, paint);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    internal async Task CompleteProfileAsync(bool throughUi)
    {
        if (!throughUi)
        {
            var result = await PostAsync<ClubReply>("/api/clubs/profile", new ProfileInput { FirstName = "Avery", LastName = "Coach", CroppedPhoto = Convert.ToBase64String(Photo()) });
            result.Kind.ShouldBe(ClubReplyKind.Saved, result.Message);
            return;
        }
        await Page.GotoAsync("/club/access");
        await Page.GetByLabel("First name", new() { Exact = true }).FillAsync("Avery");
        await Page.GetByLabel("Last name", new() { Exact = true }).FillAsync("Coach");
        await Page.GetByLabel("Profile photo", new() { Exact = true }).SetInputFilesAsync(new FilePayload { Name = "test-profile.png", MimeType = "image/png", Buffer = Photo() });
        await Page.Locator(".cropper-crop-box").WaitForAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save profile", Exact = true }).ClickAsync();
        await Page.GetByRole(AriaRole.Heading, new() { Name = "Find your club" }).WaitForAsync();
    }

    internal async Task<T> GetAsync<T>(string path)
    {
        var response = await context.APIRequest.GetAsync(path);
        response.Status.ShouldBe(200, await response.TextAsync());
        return JsonSerializer.Deserialize<T>(await response.TextAsync(), _json)!;
    }

    internal async Task<T> PostAsync<T>(string path, object input)
    {
        var token = await GetAsync<string>("/api/clubs/token");
        var response = await context.APIRequest.PostAsync(path, new() { Data = JsonSerializer.Serialize(input, _json), Headers = new Dictionary<string, string>(StringComparer.Ordinal) { ["X-Pino-CSRF"] = token, ["Content-Type"] = "application/json" } });
        response.Status.ShouldBe(200, await response.TextAsync());
        return JsonSerializer.Deserialize<T>(await response.TextAsync(), _json)!;
    }

    internal async Task CaptureAsync(string name)
    {
        var output = Environment.GetEnvironmentVariable("PINO_BROWSER_ARTIFACTS");
        if (string.IsNullOrWhiteSpace(output)) { return; }
        Directory.CreateDirectory(output);
        await Page.ScreenshotAsync(new() { Path = Path.Combine(output, name + ".png"), FullPage = true });
        (await Page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth")).ShouldBeTrue("The page must not overflow horizontally.");
    }

    public async ValueTask DisposeAsync() => await BrowserCleanup.RunAsync(CaptureFinalAsync,
        () => context.CloseAsync(), () => browser.CloseAsync(),
        () => { playwright.Dispose(); return Task.CompletedTask; }, CleanDatabaseAsync, CleanMessagesAsync);

    private async Task CaptureFinalAsync()
    {
        if (Environment.GetEnvironmentVariable("PINO_BROWSER_ARTIFACTS") is { Length: > 0 } output && !Page.IsClosed)
        {
            Directory.CreateDirectory(output);
            var prefix = Path.Combine(output, _emails.Count > 0 ? _emails[0] : "browser");
            await File.WriteAllTextAsync(prefix + ".txt", await Page.Locator("body").InnerTextAsync(), CancellationToken.None);
            await Page.ScreenshotAsync(new() { Path = prefix + ".png", FullPage = true });
        }
    }

    private async Task CleanMessagesAsync()
    {
        using var http = new HttpClient { BaseAddress = new(Environment.GetEnvironmentVariable("PINO_MAILPIT_URL")!) };
        if (_messages.Count > 0)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, new Uri("api/v1/messages", UriKind.Relative)) { Content = JsonContent.Create(new { IDs = _messages }) };
            using var response = await http.SendAsync(request, CancellationToken.None);
            response.EnsureSuccessStatusCode();
        }
    }
}
