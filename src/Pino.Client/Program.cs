using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Cropper.Blazor.Extensions;
using Pino.Client.Features.Clubs;
using Pino.SharedKernel.Clubs;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthenticationStateDeserialization();
builder.Services.AddCropper();
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<IClubGateway, HttpClubGateway>();

await builder.Build().RunAsync();
