# Service defaults options

These optional examples apply to
[`src/Pino.ServiceDefaults/Extensions.cs`](../src/Pino.ServiceDefaults/Extensions.cs).
They are not active configuration. The application currently uses the OTLP
exporter when `OTEL_EXPORTER_OTLP_ENDPOINT` is set, with ASP.NET Core, HTTP client,
and runtime instrumentation. Add an option only when the application needs it.

For new dependencies, select a compatible version in the root
[`Directory.Packages.props`](../Directory.Packages.props) and add a versionless
`PackageReference` to
[`Pino.ServiceDefaults.csproj`](../src/Pino.ServiceDefaults/Pino.ServiceDefaults.csproj).
Build and validate the adopted option through Aspire before relying on it.

## Restrict service discovery to HTTPS

In `AddServiceDefaults`, after registering service discovery, configure the
allowed schemes. Add `using Microsoft.Extensions.ServiceDiscovery;` to the file.

```csharp
builder.Services.Configure<ServiceDiscoveryOptions>(options =>
{
    options.AllowAllSchemes = false;
    options.AllowedSchemes = ["https"];
});
```

`AllowAllSchemes` defaults to `true`, which ignores `AllowedSchemes`. Set both
properties, and ensure the services being resolved offer HTTPS endpoints.
This configures service discovery; it does not configure incoming server TLS.
See [service discovery scheme selection](https://learn.microsoft.com/dotnet/core/extensions/service-discovery#scheme-selection-when-resolving-https-endpoints).

## Instrument outgoing gRPC calls

Add the `OpenTelemetry.Instrumentation.GrpcNetClient` package to the service
defaults project, using central package management as described above. In
`ConfigureOpenTelemetry`, add `AddGrpcClientInstrumentation`
to the tracing builder chain inside `WithTracing`, alongside the existing ASP.NET
Core and HTTP client instrumentation:

```csharp
.AddGrpcClientInstrumentation()
```

The existing `using OpenTelemetry.Trace;` supplies the extension-method namespace.
Keep HTTP client instrumentation for the underlying requests. The upstream
[gRPC instrumentation guide](https://github.com/open-telemetry/opentelemetry-dotnet-contrib/tree/main/src/OpenTelemetry.Instrumentation.GrpcNetClient)
currently marks this package as prerelease; check its compatibility when selecting
a version. No gRPC instrumentation package is currently referenced by Pino.

## Export to Azure Monitor

Add the `Azure.Monitor.OpenTelemetry.AspNetCore` package to the service defaults
project through central package management. Add
`using Azure.Monitor.OpenTelemetry.AspNetCore;` to `Extensions.cs`. Place the
registration in `AddOpenTelemetryExporters`, before its `return builder;`:

```csharp
if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddOpenTelemetry()
        .UseAzureMonitor();
}
```

Supply the Application Insights connection string through environment
configuration or user secrets. Keep local values out of source control.
Choose whether Azure Monitor supplements or replaces OTLP export; adding this
block leaves the existing OTLP registration in place. Verify the intended
destinations and instrumentation when adopting it. See
[OpenTelemetry with Application Insights](https://learn.microsoft.com/dotnet/core/diagnostics/observability-applicationinsights).
