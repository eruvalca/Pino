# Pino

.NET 10 Blazor application with a hosted WebAssembly client, shared UI and kernel,
Aspire orchestration, and PostgreSQL-backed ASP.NET Core Identity.

Pino is in heavy early development. Existing development data is disposable;
model and database changes have no backward-compatibility or data-preservation
requirement. Prefer one fresh initial migration when the persisted model changes.
See the [early-development migration workflow](#ef-migrations).

## Documentation

- [Product context](PRODUCT.md): confirmed users, workflows, requirements, and open decisions.
- [Agent guidance](AGENTS.md): project boundaries, design process, and authoring rules.
- [Build conventions](build/README.md): analyzers, formatting, and Razor policy.
- [Testing](tests/README.md): routine commands, assertions, and test isolation.
- [Service defaults options](build/service-defaults-options.md): optional discovery and telemetry configuration.
- [Documentation hooks](build/agent-hooks.md): automatic agent review and hook maintenance.

## Local prerequisites

- .NET SDK **10.0.401 or a later stable 10.0 SDK**, selected by
  `global.json` with `rollForward: latestFeature` and `allowPrerelease: false`.
- PowerShell 7 for the repository's `.ps1` scripts.
- Aspire CLI **13.5.4** on `PATH`, matching the AppHost SDK and stable integrations.
  Follow the [Aspire installation guide](https://aspire.dev/get-started/install/).
- Docker Desktop running Linux containers, or another Aspire-supported container runtime.
- A trusted .NET development HTTPS certificate: `dotnet dev-certs https --trust`.
- Optional VS Code with this repository's recommended C# and Aspire extensions.
  The editor opens `Pino.slnx` by default.

Run `dotnet --version`, `aspire --version`, and `aspire doctor` from the repository
root to verify the environment. The AppHost is explicitly located by the root
`aspire.config.json`.

## First run and application identity

Use PowerShell 7 for the scripts in this repository. From the solution root,
run `dotnet build Pino.slnx`, then `aspire run`. The first build/start may
restore NuGet packages, download Aspire/EF tooling, and pull container images.
The initial migration creates an empty Identity schema; no accounts or local
credentials are included.

This repository is an independent application. Review SDK, package, and skill
updates against its own code and checked-in configuration.

When running separate copies side by side, use distinct development hostnames:
different ports do not isolate browser cookies on the same hostname. Hostnames
and user-secrets IDs are checked into the launch settings and project files;
copying the repository reuses them. Choose separate IDs when copies need
independent local credentials. Aspire's default data-volume name depends on the
AppHost path; moving or renaming a checkout can select a different volume. Keep
the original secrets and volume together when preserving development data.

## Run through Aspire

Aspire is the default entry point for running and debugging the application. It
starts PostgreSQL, applies migrations, supplies configuration, and starts the web
project. The hosted WebAssembly client runs through that web project.

For interactive development, run from the repository root:

```powershell
aspire run
```

The CLI builds the AppHost and its projects before starting them. For a background
run (including agent validation), use:

```powershell
aspire start --non-interactive
aspire wait pino --timeout 120 --non-interactive
aspire describe --non-interactive
```

`aspire start` prints the dashboard login URL. The dashboard and `aspire describe`
show the current application endpoints. Open the HTTPS endpoint for
`pino.dev.localhost`; all ports are allocated dynamically and can change on
restart. Chromium browsers resolve `.localhost` names locally. Command-line
clients that do not resolve subdomains can use the internal `https://localhost`
endpoint shown by Aspire. Do not copy ports or dashboard login tokens into source.

The AppHost defaults to its first launch profile, `https`. `AddProject` selects the
matching web profile and derives its endpoints from `applicationUrl`; there is no
AppHost override for the web profile or its ports. The web profiles retain
`pino.dev.localhost:0`: the hostname is our local convention, and port `0` asks
Aspire to allocate an available port instead of using the template's fixed ports.
The other web launch settings preserve the template's environment and Blazor debugging.

The AppHost's `https` profile omits dashboard, OTLP, and resource-service addresses:
Aspire 13.5.4 supplies secure, dynamically allocated local endpoints. The optional
AppHost `http` profile sets `ASPIRE_ALLOW_UNSECURED_TRANSPORT=true`, which opts into
HTTP endpoints with allocated ports. This opt-in applies only when that profile
is selected; the default `https` profile keeps Aspire's secure defaults.

Use the web resource's **Rebuild** command to apply compiled application changes.
For AppHost changes or a full solution build, stop first to release Windows file locks:

```powershell
aspire stop --non-interactive
dotnet build Pino.slnx
aspire start --non-interactive
```

Stop the application with `aspire stop --non-interactive` (or Ctrl+C for a foreground
run). PostgreSQL and any started pgAdmin container stop with Aspire. The database
volume survives. Aspire retains its generated local PostgreSQL password in AppHost
user secrets; preserve those secrets along with the volume when reusing local data.
No database password or connection string needs to be committed.

## Debug through Aspire

In VS Code, install the recommended Aspire and C# extensions and open the repository
root. Select **Aspire: Pino** in Run and Debug, then press **F5**. The checked-in
`.vscode/launch.json` uses the Aspire debugger and discovers the AppHost through
`aspire.config.json`; no fixed ports or connection strings are needed. The extension
starts the resource graph and attaches the .NET debugger to the web project.
Use **Ctrl+F5** with the same configuration to run without debugging.

Stop any CLI-started instance with `aspire stop --non-interactive` before starting
an IDE debug session. Use the IDE's **Stop Debugging** action to end its session.
Open the dashboard through **Aspire: Open Dashboard** or the Aspire view; editor
settings control whether it opens automatically. This workspace sets
`aspire.dashboardBrowser` to `openExternalBrowser`, so the dashboard opens in the
configured external browser for both F5 and Ctrl+F5. This overrides a user-level
`debugEdge`/`debugChrome` preference; C# application debugging remains enabled with F5.

If VS Code reports **Unable to launch browser: "Unable to attach to browser"**,
check **Output → Aspire Extension**. An entry such as **Failed to start debug
browser (pwa-msedge), falling back to default browser** identifies the dashboard's
browser-debugger session. That session is separate from the AppHost and web C#
debuggers and is not needed to use the dashboard. Keep the workspace's
`openExternalBrowser` setting and remove any `dashboardBrowser: "debugEdge"` or
`"debugChrome"` override in the selected `launch.json` configuration, which takes
precedence over workspace settings. Then stop and restart the Aspire session.

Set server breakpoints in the web project's code-behind, such as the registration
handler in `src/Pino/Features/Account/Pages/Register.razor.cs`, then exercise the
page through the HTTPS endpoint shown by Aspire. Inspect variables and the call
stack in the debugger, and use Aspire's logs and traces for the corresponding
HTTP and database activity. A breakpoint can temporarily interrupt health checks;
resume execution before diagnosing the paused resource as unhealthy.

In Visual Studio, set **Pino.AppHost** as the startup project, select its
**https** profile, and use F5 (debug) or Ctrl+F5 (run). Launching `Pino` or
`Pino.Client` alone does not start the database/migration dependency graph.

The existing unit and component tests remain headless and can run/debug through
Test Explorer without Aspire.

See the [Aspire VS Code extension guide](https://aspire.dev/get-started/aspire-vscode-extension/).

## Design and styling

The application uses standard CSS with Grid as the default for structured layout
and alignment. Its current system font, neutral palette, navigation layout, and
control appearance are starter choices. The product's visual identity is open:
typography, color, imagery, iconography, styled controls, and purposeful motion
can evolve with the product's users and workflows.

Use the installed [Impeccable skill](.agents/skills/impeccable/SKILL.md) for design
work. In Codex, `$impeccable init` captures confirmed product context in
`PRODUCT.md`, and `$impeccable shape <feature>` develops a brief before coding.
Record the visual system in `DESIGN.md` as it is established; do not treat the
starter appearance as an approved design system. These records are created
during product/design work, rather than inferred from the template.

Shared tokens and styles live in `src/Pino/wwwroot/app.css`: sizing, typography,
forms (`account-form`, `form-field`, `checkbox-field`), action groups (`actions`),
notices (`notice` with a semantic `data-kind`), and table overflow
(`table-container`). Component-specific styles belong in adjacent `.razor.css`
files. Use narrowly scoped `::deep` selectors for child component markup.

Use normal flow for prose and semantic tables, and positioning for overlays.
Flexbox needs a specific layout benefit; use descriptive classes and the CSS
cascade rather than framework utilities, inline layout styles, or `!important`.
Styled controls retain semantic behavior, associated labels, and visible focus.
Motion respects `prefers-reduced-motion`. Check narrow screens, zoom, keyboard
navigation, contrast, and text wrapping, and preserve Blazor/Identity form,
passkey, and reconnect hooks. See [AGENTS.md](AGENTS.md) for authoring conventions.

## Interactivity and JavaScript

Prefer C# and Blazor for application interactivity, using native HTML/CSS behavior
where sufficient. Use JavaScript only for capabilities or browser lifecycles that
require it, or when using C# would create greater complexity, maintenance,
reliability, or performance problems. Existing passkey browser APIs on static SSR
account pages and reconnection controls that work without a live server circuit
are examples of justified JavaScript.

Strongly prefer component-owned, colocated `Component.razor.js` ES modules.
Introduce shared modules for actual reuse and global scripts/APIs or app-wide
loading only for a concrete application-level requirement. Component ownership
still applies when a module must be loaded from the app shell, as with the
passkey custom element. See the [Blazor and Razor contracts](AGENTS.md#blazor-and-razor-contracts)
for scope and lifecycle conventions and Microsoft's
[JavaScript location guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/location-of-javascript?view=aspnetcore-10.0)
for Blazor module loading.

## Resource graph and database

```text
postgres (PostgreSQL 18.3, managed data volume)
  ├─ pinodb (physical database: pino)
  │    └─ pino-migrations (EF database update)
  │         └─ pino (Blazor server + hosted WebAssembly)
  └─ pgadmin (explicit start)
```

After a normal startup, these dashboard states are expected:

| Resource | Expected state | Meaning |
| --- | --- | --- |
| `postgres`, `pinodb` | Running / Healthy | PostgreSQL and the application database are ready. |
| `pino-migrations` | Finished | The one-shot migration command completed successfully. It is not a long-running service. |
| `pino` | Running / Healthy | The web application is ready and its database readiness check passes. |
| `pgadmin` | Not started | Optional database UI; start it when needed. |

With hidden resources displayed, `pino-rebuilder` can also be **Not started**
until the web project's Rebuild command is used, and the EF tool resource can be
**Finished**. These helper states do not indicate an application startup failure.
An **Exited**, **Failed**, or **Unhealthy** application/database resource, or web
startup blocked by failed migrations, does require investigation through its logs.

The PostgreSQL image version is the built-in default of
`Aspire.Hosting.PostgreSQL` 13.5.4. Container lifetime remains Aspire's default
session lifetime. Data uses an Aspire-managed named volume rather than a repository
file. This is a fresh PostgreSQL schema; the original SQLite scaffold is removed.

Aspire supplies `ConnectionStrings:pinodb` to both the application and migration
tool. The application uses Npgsql EF Core 10.0.3, EF Core 10.0.12, and Aspire's
Npgsql EF integration 13.5.4. Its non-pooled `IDbContextFactory<ApplicationDbContext>`
supports a context per Blazor operation; Identity can still resolve the scoped
context. Dispose factory-created contexts with `await using`.

Identity schema version 3 is retained, including the `AspNetUserPasskeys` table.
Development registration uses the existing no-op email sender: follow the confirmation
link on the registration confirmation page before logging in. This setup does not
configure a production email service or deployment infrastructure.

Before production, replace `IdentityNoOpEmailSender` and remove or deliberately
gate the scaffold confirmation link. The current shortcut checks the sender type,
not the hosting environment; it is not restricted to Development. External-login
provider credentials, production secrets, HTTPS/domain configuration, deployment,
and the desired exposure of health endpoints also require application-specific work.

## EF migrations

`Aspire.Hosting.EntityFrameworkCore` **13.5.4-preview.1.26464.4** manages
`pino-migrations` and its **dotnet-ef 10.0.12** tool. It does not alter the machine's
global EF tool. On every start, migrations wait for PostgreSQL and the database;
the web application starts only after migration success. Failure blocks web startup.
Migrations use the actual web startup registration, keeping Identity's design-time
and runtime models aligned.

During early development, replace the migration history for entity/model changes
that affect persistence instead of accumulating incremental migrations. Delete
the old migrations, their designer files, and the model snapshot, then generate
one new initial migration for the complete current model. Existing local accounts
and other application data do not need to survive this process; do not add
backfills or compatibility layers solely to retain them.

Replacing migration files also requires recreating databases built from the old
history. Deleting files or clearing `__EFMigrationsHistory` alone does not remove
the old schema. Each developer using the changed branch must recreate their own
local `pino` database. This policy authorizes that reset as part of schema/model
work without a separate data-preservation confirmation; it does not extend to
unrelated databases or volumes.

For a schema change, use this reset workflow:

1. If a local `pino` database exists, start the current Aspire graph if needed,
   then stop the `pino` web resource and close other application/database clients.
   On `pino-migrations`, run **Drop Database**, accepting its confirmation for this
   checkout's local database. Keep PostgreSQL running for the command and retain
   its managed volume and credentials.
2. Stop Aspire. Update the model and remove the existing migration files, designer
   files, and `ApplicationDbContextModelSnapshot.cs` from `src/Pino/Data/Migrations`.
   Before removing them, identify any still-needed custom schema SQL or seed
   definitions that must be carried into the new migration; EF cannot reconstruct
   arbitrary migration customizations from the model.
3. Build the solution and start Aspire. While the new initial migration is absent,
   migration startup can fail and block the web resource; the migration authoring
   commands remain available. This intermediate state is not successful validation.
4. On `pino-migrations` in the dashboard, run **Add Migration...** with a name such
   as `InitialCreate`. Review the complete schema in `src/Pino/Data/Migrations`,
   using namespace `Pino.Migrations`, and restore any still-needed customizations.
5. Apply the repository's conventions to the handwritten migration class (file-scoped
   namespace and `internal sealed partial class`); leave generated designer code alone.
   Keep the model snapshot in the same migrations directory. EF can choose a directory
   from the legacy namespace when regenerating a missing snapshot, so check its location.
6. Stop Aspire, rebuild, and start again. Migration commands are disabled after
   authoring until the target project is rebuilt. The built-in migration resource
   applies the newly compiled initial migration to the empty database before
   starting the web application.
7. Run **Get Database Status** on `pino-migrations` to verify that the single new
   initial migration is applied and no model changes or migrations are pending.
   Verify application/database health and exercise the changed persistence behavior.
   Recreate disposable sample data or accounts as needed.

CLI equivalents for inspecting and updating the current compiled model:

```powershell
aspire resource pino-migrations ef-database-status --non-interactive
aspire resource pino-migrations ef-database-update --non-interactive
aspire logs pino-migrations --non-interactive
```

The built-in **Remove Migration**, **Drop Database**, and **Reset Database** commands
are also available. **Reset Database** drops and recreates the database using the
currently compiled migrations; it does not replace the migration source files.
Use it when a reset is needed after rebuilding the new initial migration. Ordinary
restarts retain local data, and routine validation does not require a reset.

This workflow is appropriate while all affected databases are disposable. Before
production or any environment whose data must be retained, update this policy,
keep applied migration history, and use incremental, data-preserving migrations.
There is no application-startup migration routine or custom worker. See
[EF Core's reset guidance](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/managing#resetting-all-migrations)
and [Aspire's EF migration integration](https://aspire.dev/integrations/databases/efcore/migrations/).

## Health, telemetry, and pgAdmin

The application references `Pino.ServiceDefaults`. In Development, `/health`
checks readiness including PostgreSQL connectivity; `/alive` checks process liveness
independently of PostgreSQL. Aspire monitors `/health`. The database readiness check
has a five-second timeout so EF's transient retries do not hold an unhealthy response
open for minutes. Cancellation is cooperative: an in-flight Npgsql connection attempt
can delay the response (about 15 seconds in local outage validation). Normal
application operations retain Aspire's retry behavior.

The dashboard exposes server logs, request traces, Npgsql database spans, and metrics.
Useful CLI commands include `aspire logs pino`, `aspire otel traces`, and
`aspire otel logs`. The Aspire MCP server provides the same resource and telemetry
visibility to agents. Database query telemetry can contain application information;
keep exports and runtime logs out of Git.

pgAdmin is available only in run mode. Start `pgadmin` from its dashboard action or:

```powershell
aspire resource pgadmin start --non-interactive
aspire wait pgadmin --timeout 120 --non-interactive
```

Open its generated HTTP endpoint, expand **Servers → postgres → Databases → pino**.
Aspire configures the connection and credentials. Stop it from the dashboard when done.

## Agent tools and skills

Shared repository skills live in `.agents/skills`, supported by Codex and
Copilot CLI. Copilot desktop inherits repository/CLI skills and MCP configuration.
Impeccable's installed provider integrations also include `.github/skills`,
`.github/agents`, `.github/hooks`, and `.codex/hooks.json`. Preserve its
installer-managed files and hook entries and update them through Impeccable's
installer. The Codex manifest also contains separate project-owned documentation
hooks; retain those when reviewing an installer update. Keep project-specific
guidance in `AGENTS.md` and product/design records.

Reload your agent after cloning or updating the installed skills. Impeccable's
launcher can download its engine on first use; hook trust is local to the agent
environment and is not granted by checking in the hook manifest. Follow the
[installed skill](.agents/skills/impeccable/SKILL.md) and your agent's hook controls.

The project-owned Codex hooks remind the agent to review documentation before
committing or finishing work, then request one final review if the workspace
changed during the turn. The agent makes any necessary edits in the normal task;
the hook itself only stores ignored comparison state. It does not block Git
commits. Reload Codex and review/trust the new hooks using its hook controls
(`/hooks` in the CLI). See [documentation hooks](build/agent-hooks.md) for the
scope, limitations, and regression check.

The Aspire skills were reconciled with the first-party **aspire-skills v0.0.1**
bundle referenced by CLI 13.5.4 (including its published SHA-512). Its descriptions
still refer to Aspire 13.4; those upstream headings are intentionally unchanged.
Use installed package/API evidence and current documentation when versions differ.

The existing user-level Codex and Copilot MCP entries run **`aspire agent mcp`**.
Keep a single entry per agent. No repository MCP duplicate or VS Code agent
configuration is required. On a new machine, install Aspire on `PATH`, then:

- Codex: `codex mcp add aspire -- aspire agent mcp` (unless already configured).
- Copilot CLI: use `/mcp add` to add a local/stdio server named `aspire`, command
  `aspire`, arguments `agent mcp`, at user scope. Its configuration is stored in
  `~/.copilot/mcp-config.json` and is inherited by Copilot desktop.
- Restart/reload the agent after configuration. Start the application from this
  repository and use the Aspire MCP resource-list tool to verify the connection.
  If several AppHosts are running, select this repository's AppHost explicitly.

The checked-in Aspire skills require no additional installation on clone. To
refresh those skills, use the matching CLI's `aspire agent init` with the
**standard** skill location (`.agents/skills`), then review the diff. Avoid
creating alternate Aspire skill copies under `.github`, `.codex`, or VS Code
agent configuration. Keep user secrets, dashboard tokens, runtime `.aspire`
state, telemetry exports, and temporary browser output out of source control.

References: [Aspire MCP](https://aspire.dev/reference/cli/commands/aspire-agent-mcp/),
[Codex skills](https://learn.chatgpt.com/docs/build-skills#where-codex-loads-local-skills),
[Copilot CLI skills](https://docs.github.com/en/copilot/how-tos/copilot-cli/customize-copilot/add-skills),
[Copilot desktop](https://docs.github.com/en/copilot/how-tos/github-copilot-app/customize-github-copilot-app).

## Validation

The existing tests run headlessly with native Microsoft.Testing.Platform and
Shouldly; they do not require Aspire or PostgreSQL:

```powershell
dotnet build Pino.slnx
dotnet test --solution Pino.slnx
```

See [tests/README.md](tests/README.md) for test conventions and
[build/README.md](build/README.md) for Razor code-behind validation.
