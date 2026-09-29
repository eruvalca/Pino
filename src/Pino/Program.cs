using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Pino.Components;
using Pino.Data;
using Pino.Features.Account.Endpoints;
using Pino.Features.Account.Services;
using Pino.ServiceDefaults;
using Cropper.Blazor.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Pino.Features.Clubs.Endpoints;
using Pino.Features.Clubs.Services;
using Pino.SharedKernel.Clubs;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureBlobServiceClient("profileblobs");
builder.Services.AddCropper();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IProfilePhotoStore, ProfilePhotoStore>();
builder.Services.AddHostedService<PhotoCleanupService>();
builder.Services.AddScoped<ClubService>();
builder.Services.AddScoped<IClubGateway, ServerClubGateway>();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-Pino-CSRF");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AccountSignInService>();
builder.Services.AddScoped<AccountPasskeyService>();
builder.Services.AddScoped<AccountRegistrationService>();
builder.Services.AddScoped<AccountEmailChangeService>();
builder.Services.AddScoped<AccountTwoFactorService>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    // File endpoints also need API status codes; never turn a denied photo into login HTML.
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api/clubs", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
        else { context.Response.Redirect(context.RedirectUri); }
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api/clubs", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
        }
        else { context.Response.Redirect(context.RedirectUri); }
        return Task.CompletedTask;
    };
});

var connectionString = builder.Configuration.GetConnectionString("pinodb")
    ?? throw new InvalidOperationException("Connection string 'pinodb' not found. Start the application through Aspire or configure ConnectionStrings:pinodb.");
// The factory supports one context per Blazor operation and also registers the scoped context used by Identity.
builder.Services.AddDbContextFactory<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
builder.EnrichNpgsqlDbContext<ApplicationDbContext>();
// Bound readiness independently of EF's transient retries for normal application operations.
builder.Services.PostConfigure<HealthCheckServiceOptions>(options =>
{
    var databaseCheck = options.Registrations.SingleOrDefault(registration =>
        string.Equals(registration.Name, nameof(ApplicationDbContext), StringComparison.Ordinal));
    if (databaseCheck is not null)
    {
        databaseCheck.Timeout = TimeSpan.FromSeconds(5);
    }
});
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddScoped<IUserStore<ApplicationUser>, ClubUserStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/clubs", StringComparison.OrdinalIgnoreCase) &&
        context.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } limits)
    {
        limits.MaxRequestBodySize = 2 * 1024 * 1024;
    }
    await next(context);
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Pino.UI.UiAssemblyMarker).Assembly);

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();
ClubEndpoints.Map(app);
app.MapDefaultEndpoints();

await app.RunAsync();
