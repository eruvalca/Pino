# Pino agent guidance

Pino is a .NET 10 / C# 14 Blazor application with server and WebAssembly rendering,
Aspire orchestration, PostgreSQL, and ASP.NET Core Identity. The repository is a
starting point for a new product in heavy early development; its sample pages
and starter appearance do not establish the product's requirements or visual
identity.

## Working in this repository

- Inspect the relevant implementation and `git status` before editing. Preserve
  unrelated work and keep changes focused on the requested outcome.
- Follow existing feature organization and reuse useful components and services.
  Introduce abstractions and dependencies when a concrete requirement warrants
  them; avoid speculative layers, broad rewrites, and unrelated cleanup.
- Use the user's current brief and confirmed product decisions. Ask about material
  gaps in product behavior or design intent; resolve routine implementation
  choices within these conventions. Do not invent business rules or brand claims.
- Keep this file focused on durable agent instructions. Update supporting docs
  when behavior or workflows change, and remove conflicting guidance. Obtain
  exact SDK/package versions from checked-in configuration rather than copying
  version pins into new instructions.

Read the relevant references as needed:

| Reference | Purpose |
| --- | --- |
| [README.md](README.md) | Setup, Aspire lifecycle, migrations, and local services |
| [build/README.md](build/README.md) | Analyzer rules, SDK compatibility, and Razor validation |
| [tests/README.md](tests/README.md) | Test runner, helpers, assertions, and reports |
| [Impeccable](.agents/skills/impeccable/SKILL.md) | Design discovery, implementation, and review |
| [Aspire](.agents/skills/aspire/SKILL.md) | Orchestration and diagnostics workflows |

## Project map and boundaries

| Location | Responsibility |
| --- | --- |
| `src/Pino` | Web host, Identity, account SSR pages, persistence, server services |
| `src/Pino.Client` | WebAssembly startup and client registrations |
| `src/Pino.UI` | Browser-compatible shared pages, components, and layouts |
| `src/Pino.SharedKernel` | Contracts and domain code usable by server and browser |
| `src/Pino.AppHost` | Aspire resource graph and migration orchestration |
| `src/Pino.ServiceDefaults` | Health checks, telemetry, and service defaults |
| `tests/Pino.UnitTests` | Server logic tests |
| `tests/Pino.ComponentTests` | Blazor rendering and interaction tests with bUnit |
| `build/Pino.Build` | Razor code-behind policy validator |

- Preserve dependency direction: `Pino.Client` and `Pino.UI` must not reference
  `Pino`. Keep server infrastructure and secrets out of browser assemblies and
  `Pino.SharedKernel`; share only contracts and behavior needed across boundaries.
- Keep feature-specific pages, components, services, and outcomes close to their
  feature. Follow the existing `Features/<Feature>/...` organization.
- Enforce authorization and validate untrusted inputs on the server. Hiding UI or
  client-side validation is insufficient to protect an operation.

## Design process

- Use the installed [Impeccable skill](.agents/skills/impeccable/SKILL.md) for design
  and UI work. Its guidance supports the brief and repository's technical
  contracts; its aesthetic preferences are not independent product requirements.
- For new product work, establish the audience, primary tasks, realistic content,
  and constraints before committing to screens. Use Impeccable's `init` workflow
  to capture missing product context in `PRODUCT.md`; use `shape` when a design
  brief is needed before implementation. In Codex, invoke these as
  `$impeccable init` and `$impeccable shape <feature>`.
- Keep confirmed product facts in `PRODUCT.md` and the established visual system
  in `DESIGN.md`. Leave unknowns explicit. Record visual decisions as they are
  established, rather than documenting the scaffold as an approved design system.
  A focused fix can use existing context without reopening product discovery.
- Develop representative workflows and domain models together. Use realistic,
  clearly identified sample data while persistence is incomplete. Account for
  relevant empty, loading, success, validation, failure, permission, and overflow
  states, including minimum and maximum realistic content.
- The starter's neutral palette, system font, navigation layout, native control
  appearance, and lack of motion are provisional. Typography, color, density,
  imagery, iconography, styled controls, and purposeful animation are available
  design choices. Select them for the product and task; do not impose a generic
  marketing aesthetic on operational screens.
- Once a visual direction is established, keep additions consistent with it.
  Distinguish focused refinement from an intended redesign. Preserve product
  facts and working interactions throughout visual changes.

## CSS, interaction, and accessibility

- Use standard CSS with Grid as the default for structured layout and alignment.
  Use normal flow for prose and semantic tables, and positioning for overlays.
  Flexbox is appropriate when its one-dimensional behavior has a specific
  benefit; document that reason near the relevant layout.
- Put shared design tokens and reusable form, action, notice, and table patterns
  in `src/Pino/wwwroot/app.css`. Put component-specific rules in adjacent
  `.razor.css` files. Scope `::deep` narrowly to child markup that requires it.
- Use descriptive classes and reusable CSS custom properties. Avoid Bootstrap
  or utility-framework dependencies, inline layout styles, and `!important` as
  shortcuts around the existing CSS structure.
- Style semantic HTML controls freely. Preserve keyboard operation, accessible
  names, visible focus, associated labels, and appropriate validation feedback.
  Put form labels before their controls. Custom widgets must provide the
  semantics and interactions of the control they replace.
- Make layouts shrink, wrap, and remain usable on narrow screens and with zoom
  or long content. Preserve logical reading and focus order when using Grid.
  Keep sufficient contrast and communicate state with text as well as color.
- Use motion intentionally and honor `prefers-reduced-motion`; essential content
  and actions must remain available with reduced or disabled animation. Give
  meaningful imagery alternative text and hide purely decorative icons from
  assistive technology.
- Shared notices use `class="notice"` with `data-kind="success"`, `"error"`,
  `"warning"`, or `"information"`. Their presentation may evolve; preserve the
  semantic kind and a meaningful message.
- Preserve Blazor/Identity form, passkey, and reconnect hooks when changing markup.
  Verify changed layouts and interactions in a browser; build success and design
  detector results alone do not establish visual or accessibility quality.

## Blazor and Razor contracts

- Account pages use static server-side rendering (SSR). Preserve
  `[ExcludeFromInteractiveRouting]` in their page imports, authorization on
  account-management pages, and HTTP context/cookie behavior. Keep form names,
  POST methods, `[SupplyParameterFromForm]`, and antiforgery hooks intact.
- Shared interactive pages opt into `InteractiveAuto`. Do not introduce a global
  interactive render mode that changes account behavior. Keep shared components
  compatible with both server and browser execution; consider prerendering when
  loading data or invoking JavaScript.
- `BlazorDisableThrowNavigationException` is enabled. Return or otherwise end a
  branch after redirecting when later work must not execute; navigation does
  not provide that control-flow boundary by throwing.
- Every `.razor` page/component has a matching `.razor.cs` partial class, including
  markup-only components. `_Imports.razor` is exempt. Keep members and logic in
  code-behind; use no `@code` or `@functions` blocks. Rendering expressions and
  control flow stay in markup.
- Match the generated component's namespace, accessibility, class name, and
  generic parameters. Add C# imports explicitly; `_Imports.razor` does not supply
  code-behind imports. Preserve routes, render modes, injection, and parameter
  contracts during refactoring. Do not edit generated Razor C#.
- Mark necessary caller-supplied `[Parameter]` properties `[EditorRequired]`.
  Keep optional inputs optional; assess route, query, form, and cascading inputs
  separately. Use ordinary auto-properties, not `required` or `init`.
  `[EditorRequired]` checks Razor call sites; it is not runtime validation.
- Initialize values that depend on parameters in the appropriate lifecycle
  method. Keep `[Parameter]` and `[CascadingParameter]` inputs as properties.

## C# and dependencies

- Follow `.editorconfig`: UTF-8 without BOM, LF line endings, file-scoped C#
  namespaces, braces around control-flow bodies, and the configured formatting.
- Use the narrowest practical visibility and seal concrete classes unless
  inheritance has a purpose. Default implementation types to `internal sealed`
  at namespace scope and `private sealed` when nested. Preserve necessary public
  contracts and assess cross-project inheritance before sealing or hiding types.
- Concrete Razor components can be `public sealed partial class`; intentional
  base components remain inheritable. Keep implementation members private.
  Document analyzer exceptions on the affected type with a concrete reason.
  MA0053 cannot see inheritance in other projects; `.razor.cs` is exempt from
  CA1515, not from other conventions. See the build guide for these boundaries.
- Use primary constructors whenever a handwritten class or struct needs an
  explicit instance constructor, including code-behind. Additional overloads
  delegate through `this(...)`. Do not add empty constructors when unnecessary.
  Static constructors and existing `@inject`/`[Inject]` injection are unaffected.
- Prefer C# 14 extension blocks for handwritten extensions; follow the
  [extension members skill](.agents/skills/csharp-extension-members/SKILL.md).
  Use properties for inexpensive, side-effect-free facts and methods for
  transformations, enumeration, asynchronous work, or side effects. Preserve
  public contracts and document any need for classic extension-method syntax.
  Keep core behavior and factories on types we own; do not convert every static
  helper. Stay on C# 14 rather than copying preview syntax or SDK settings.
- Nullable analysis and analyzer references are centralized in
  `Directory.Build.props`. NuGet versions belong in `Directory.Packages.props`;
  consuming `PackageReference` items stay versionless and retain metadata such
  as `PrivateAssets`. SDK versions stay in SDK declarations or `global.json`.
  Do not add per-project overrides or nested central package files without a
  documented need.
- Compiler/analyzer warnings fail builds. Fix the cause without weakening shared
  settings. Keep necessary suppressions narrow and justified; retain the
  documented SDK compatibility workaround in `build/README.md`.

## Operation outcomes

- Use OneOf for meaningful alternatives with different handling or payloads,
  including mutually exclusive states. Keep ordinary optional values,
  independent booleans, and adequate contracts such as simple `IdentityResult`.
  Do not introduce a universal result abstraction.
- Prefer named source-generated unions for reusable service outcomes:
  `[GenerateOneOf] internal sealed partial class ... : OneOfBase<...>`.
  Use descriptive payload records and marker structs for cases without data;
  let the generator supply constructors/conversions. Small local helpers need
  not become named union classes.
- Use exhaustive `Match` for values and `Switch` for synchronous side effects.
  Return and await `Match<Task>` or `Match<Task<T>>` for asynchronous branches;
  never pass async lambdas to `Switch`. Use `TryPick` for deliberate separation
  of one case; avoid routine `.Value`, `IsTn`, or `AsTn` control flow. Direct case
  inspection is appropriate in tests.
- Represent anticipated failure and partial completion accurately, including
  earlier writes that succeeded. Preserve unexpected exceptions and cancellation.
  Keep outcomes feature-local and separate from forms, cookies, persistence,
  and HTTP transport models. Share cases only when their meaning is identical.
- Test each meaningful case, its payload, and observable handling. Verify that
  failures skip later operations and partial completion never reports full success.
  See [PasskeySubmission](src/Pino/Features/Account/Models/PasskeySubmission.cs) and
  [EnableAuthenticatorOutcome](src/Pino/Features/Account/Models/EnableAuthenticatorOutcome.cs).

## Aspire, persistence, and local data

- During this early-development phase, existing application data is disposable.
  Model, schema, and data-format decisions do not need backward compatibility
  with earlier versions or data-preserving upgrade paths. Avoid compatibility
  layers, backfills, or transitional schemas solely to preserve old local data.
- For entity/model changes that affect persistence, prefer replacing all existing
  migrations, their designer files, and the model snapshot with one fresh initial
  migration for the complete current model. Recreate the local database to match;
  do not accumulate incremental migrations by default. Carry forward still-needed
  custom schema SQL or seed definitions that EF cannot regenerate. Follow the
  README's reset procedure and validate creation from an empty database.
- This policy provides standing authorization to drop/recreate this checkout's
  local development `pino` database when required for schema/model work; no
  separate data-preservation confirmation is needed. Scope resets to that
  database, preserving unrelated databases, files, volumes, and secrets. Before
  introducing production or other data that must survive upgrades, replace this
  policy with an incremental, data-preserving migration workflow.
- Run the application through Aspire from the repository root;
  `aspire.config.json` selects `src/Pino.AppHost/Pino.AppHost.csproj`. Use the CLI
  for agent runs and the **Aspire: Pino** configuration for VS Code debugging;
  Visual Studio uses `Pino.AppHost`. Do not run the web/client projects alone.
- For agent validation, use `aspire start --non-interactive`,
  `aspire wait pino --timeout 120 --non-interactive`, and
  `aspire describe --non-interactive`. Discover endpoints from Aspire; do not
  hard-code ports. Stop Aspire before full builds on Windows, and stop instances
  started for agent validation when finished. Account for an existing user-run
  session before interrupting it.
- PostgreSQL resource `postgres` hosts `pinodb` (physical database `pino`). Aspire
  injects `ConnectionStrings:pinodb`. Keep the managed data-volume configuration
  and session-scoped container lifetime; reset the application database through
  the migration resource rather than deleting the volume. Do not restore SQLite
  or hard-code credentials.
- Use `IDbContextFactory<ApplicationDbContext>` for independent Blazor operations
  and dispose created contexts. Preserve Identity schema version 3 and passkeys.
- Author migrations through the built-in `pino-migrations` resource using the
  web startup model. Keep migrations/snapshot in `src/Pino/Data/Migrations`,
  namespace `Pino.Migrations`, and rebuild after generation. The web resource
  waits for successful migration completion. Do not add startup migration code
  or a custom migration worker. Follow the README's migration procedure.
- Keep Aspire packages/SDK, the selected preview EF integration, and the managed
  EF tool version aligned with checked-in configuration and the README.
- Preserve `/health` database readiness, its bounded check, and Aspire's health
  monitoring. `/alive` checks process liveness independently. These endpoints
  are Development-only; changing exposure requires a deployment decision.
- pgAdmin is an explicit-start, run-mode tool. In normal operation it is **Not
  started**, migrations are **Finished**, and web/database resources are
  **Running / Healthy**. Inspect commands/logs before treating optional or
  one-shot resource states as failures.
- Use disposable accounts for validation. Outside a schema/model reset, clean up
  only data created for that task; routine validation does not require a reset.
  Keep secrets, dashboard tokens, local runtime state, telemetry exports, and
  temporary browser captures out of source control. Do not enable preview
  browser logging unless requested.
- The no-op email sender and scaffold confirmation link are development setup,
  not a production delivery mechanism. Follow the README's production requirements
  before deployment; do not weaken authentication to make a design demo work.

## Validation and tests

Run commands from the repository root so `global.json` selects the SDK and native
Microsoft.Testing.Platform (MTP) runner. After code changes, run the full solution
build and both test projects; use relevant focused tests during development:

```powershell
dotnet build Pino.slnx
dotnet test --solution Pino.slnx
```

- Unit and component tests are headless and need no Aspire, database, browser,
  or editor. Use `Pino.UnitTests` for server logic and `Pino.ComponentTests` for
  rendering/interactions. See the test guide for focused project commands.
- Use xUnit discovery/execution with `xunit.v3.core.mtp-v2` and Shouldly assertions
  exclusively. Do not add xUnit assertions, FluentAssertions, or bUnit assertion
  helpers such as `MarkupMatches`. For semantic markup, assert that `CompareTo`
  differences `ShouldBeEmpty()`. Use Shouldly inside `WaitForAssertionAsync` for
  asynchronous rendering. Preserve exact exception checks with
  `Should.Throw<T>(action).ShouldBeOfType<T>()` where derived types must fail.
- Preserve `all` / `conservative` / `1x` parallel settings. Each test owns its
  mutable state; create and dispose a fresh `BunitContext` with `await using`.
  Shared fixtures must support concurrent access; justify parallelism opt-outs.
  Seed Bogus per instance using `UseSeed` or a local `Randomizer`, never global
  `Randomizer.Seed`. Use explicit boundary values, tokens, and expected results.
- Reuse `ConfigureAccount`, `CaptureLogs`, and account form helpers. Exercise
  rendered events. Use `SetFormValue`/`SetInputValue` only for posted values
  normally supplied by static SSR form mapping. Do not invoke private handlers
  or mutate private state to inflate coverage.
- Use xUnit MTP's `--filter-class` or `--filter-method` directly, without a `--`
  separator or VSTest `--filter` expressions. Verify selected names and counts;
  zero executed tests is not validation. `dotnet test` builds/restores by default;
  use `--no-build` only after matching configuration builds with no later source
  edits. Coverage and report generation are opt-in for routine work.
- Builds enforce the Razor code-behind policy. After modifying that policy or
  upgrading the SDK, also run `pwsh ./scripts/Test-RazorCodeBehind.ps1`.
  Fix violations; an empty matching partial class is valid for markup-only UI.
- Check changed UI in the running browser at relevant desktop/narrow widths,
  including keyboard navigation and meaningful states. bUnit does not verify
  real JavaScript, CSS layout, or server/WebAssembly transitions. Persistence
  changes need integration validation when they affect database behavior.
- For documentation-only changes, verify accuracy, referenced paths, and the diff;
  application builds/tests are unnecessary unless executable behavior also changes.
- Report what changed, the checks actually run, and any remaining limitations.
  For tests, report actual passed, failed, and skipped counts. State blockers
  clearly rather than claiming unperformed validation.

## Agent tooling maintenance

- Review documentation as part of each implementation task, before any authorized
  commit and before reporting completion. Update affected instructions and docs
  alongside the implementation; leave accurate documentation unchanged. Briefly
  report the review outcome. A review is not permission to commit or to edit files
  during a read-only task.
- Keep durable agent conventions here, setup and runtime workflows in `README.md`,
  build rules in `build/README.md`, and test conventions in `tests/README.md`.
  Use feature docs for detailed behavior and `PRODUCT.md` / `DESIGN.md` for
  established product/design decisions. Update existing sources rather than
  duplicating rules, version pins, or historical validation results.
- The project-owned Codex documentation hooks prompt this review and request at
  most one finishing pass for a turn that changed the workspace. They are advisory,
  not proof of documentation accuracy or a Git commit gate. See
  [agent hook maintenance](build/agent-hooks.md) for behavior and validation.

- Use `.agents/skills` for shared repository skills and read relevant skills
  before applying them. Keep upstream packages intact; put project-specific
  conventions here or in the appropriate project documentation.
- Impeccable also has installed provider-specific files under `.github` and
  `.codex`. Preserve its generated hook entries and use the upstream installer
  to update its integrations together; retain the separate project-owned
  documentation entries in `.codex/hooks.json` when reviewing installer changes.
  Product/design records belong to this project, outside the installed skill.
- Refresh Aspire skills with `aspire agent init` using the standard
  `.agents/skills` location and review the diff. Preserve the single user-level
  `aspire agent mcp` entry for each agent; do not add repository MCP duplicates.
- Prefer checked-in configuration and installed API evidence when upstream skill
  text describes a different version. Keep tool upgrades explicit and reviewable.
