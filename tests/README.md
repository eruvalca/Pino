# Testing Pino

The test projects target .NET 10 and use xUnit with native
Microsoft.Testing.Platform (MTP) integration. Open the repository root so both
the CLI and editor find `global.json` and `Pino.slnx`.

| Project | Scope |
| --- | --- |
| `Pino.UnitTests` | Account services and extensions, outcome decoding, redirects, authentication-state revalidation, Identity endpoint behavior, club field/role rules, profile-image validation, photo-cleanup recovery, and service defaults. |
| `Pino.ComponentTests` | Account sign-in and management workflows, club onboarding and administrator actions, validation, shared account components, navigation, error request IDs, the shared UI's `Counter`, and tryout sample notes, filters, decisions, eligibility, completion, and history, using bUnit. |
| `Pino.BrowserTests` | Opt-in Playwright journeys against local Aspire: registration and email delivery, profile cropping, club setup, CSV import, seasonal evaluation, persisted history, concurrency, membership revocation and antiforgery. |

`Pino.BrowserTests` also runs headless fixture-cleanup regression tests without
Aspire. Its sporting journey injects a lost database commit acknowledgement to
verify import recovery against PostgreSQL, using a direct server-service reference.

The unit suite also covers sporting validation, CSV parsing and player image
normalization. Component tests cover persisted-tryout draft isolation, eligible
team selection, save failures, revoked access and archived/prerendered controls.

## Supported stack

The following versions match the checked-in configuration. Treat
[`global.json`](../global.json) and
[`Directory.Packages.props`](../Directory.Packages.props) as the source of truth
when updating this table; editor extensions are recommended, not pinned.

| Component | Version |
| --- | --- |
| .NET SDK minimum | 10.0.401; `latestFeature` roll-forward within stable 10.0 |
| bunit (component project only) | 2.11.3 |
| xunit.v3.core.mtp-v2 | 4.0.1 |
| Shouldly | 4.3.0 |
| NSubstitute (account tests) | 6.2.0 |
| Bogus (locally seeded fake data) | 35.6.5 |
| Microsoft.Testing.Platform / Platform.MSBuild | 2.4.1 |
| Microsoft.Testing.Extensions.TrxReport | 2.4.1 |
| Microsoft.Testing.Extensions.CodeCoverage | 18.11.2 |
| xunit.analyzers | 2.1.0 |

Package versions are centralized in `Directory.Packages.props`. Explicit MTP
runtime and MSBuild references keep those packages aligned at the selected version.
Test package references use `PrivateAssets="all"`. All test projects
reference xUnit's core MTP package, which supplies the framework and runner
without `xunit.v3.assert`; xUnit analyzers remain included by the root build props.

The root build props recognize project names ending in `.UnitTests`, `.ComponentTests` or `.BrowserTests` before
evaluating shared analyzer references. Tests inherit nullable analysis, code style
rules, and warnings-as-errors. xUnit test classes are public and sealed; their
type-level CA1515 suppression documents the discovery requirement. The server
grants `Pino.UnitTests` and `Pino.ComponentTests` access to its internal types.
The component project uses the Razor SDK and references both `Pino.UI` and the
server project. The server also grants `DynamicProxyGenAssembly2` internal access
so NSubstitute can proxy Identity dependencies closed over the internal
`ApplicationUser` type.

`global.json` selects native .NET 10 MTP mode. Do not add VSTest packages
(`Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`) or the legacy
`TestingPlatformDotnetTestSupport` bridge to these projects. bUnit provides the
component renderer and comparison tools; xUnit and MTP provide discovery and
execution, and Shouldly is the exclusive assertion library.

Use Bogus for realistic generated fixture values with a per-instance seed
(`UseSeed` or a locally assigned `Randomizer`). Do not set the global
`Randomizer.Seed`: test methods and theory rows run in parallel.
Keep explicit literals for boundaries, encoded tokens, and expected results.

Account tests use `context.ConfigureAccount()` and `context.CaptureLogs<TComponent>()`
from `BunitAccountExtensions`. The resulting `AccountTestContext` holds each test's
HTTP context and Identity dependencies. `ComponentFormExtensions` supplies
`SetFormValue` and `SetInputValue` where static SSR form mapping needs to be simulated;
`LoggerTestExtensions.GetLoggedEventIds()` inspects captured logging calls. Keep
these receiver-focused helpers in the account test namespace and use a fresh,
asynchronously disposed `BunitContext` for every test.

For components that inspect `RendererInfo`, set the test renderer explicitly
after registering services, for example `context.SetRendererInfo(new("Server",
isInteractive: true))`. Use `isInteractive: false` when checking prerendered
controls; this prevents tests from overlooking input lost during hydration.

## Build and run

When configuring or verifying NSubstitute calls, match the cancellation token
supplied by the code under test. Use `Arg.Any<CancellationToken>()` when token
identity is outside the assertion; use an exact token when forwarding is the
behavior being tested. Automatically inserting the test runner's token into
substitute setup can stop it matching a component's operation token. Test watchdog
timeouts use `TimeProvider.System` even when the subject uses a substitute clock.

An explicit result type may intentionally verify a fluent API's concrete return
type at compile time. Preserve that check when applying `var` suggestions and
justify any necessary suppression on the affected test method.

`ApplicationDbContextTests` inspect the PostgreSQL model without opening a
database connection. They verify that application entities have named sets and
conventional table names, Identity retains its user/passkey mappings, and the
migration snapshot matches the model. Schema changes also require the Aspire
database reset/migration validation in the root README and the browser
persistence journeys below.

Run these commands from the repository root:

```powershell
dotnet build Pino.slnx
dotnet test --solution Pino.slnx
```

Stop Aspire before the full build on Windows to release application file locks.
Unit/component tests need no running application or database. Browser journeys are
reported as skipped unless explicitly configured. During development,
choose the relevant project or class instead of repeatedly running the full suite:

```powershell
dotnet test --project tests/Pino.UnitTests/Pino.UnitTests.csproj
dotnet test --project tests/Pino.ComponentTests/Pino.ComponentTests.csproj
dotnet test --project tests/Pino.ComponentTests/Pino.ComponentTests.csproj --filter-class "Pino.ComponentTests.Features.Counter.Pages.CounterTests"
```

`dotnet test` restores and builds by default. Use `--no-build` only after building
the same configuration and making no subsequent source changes. Use xUnit MTP's
`--filter-class` or `--filter-method` directly, without a `--` separator or VSTest
`--filter` expressions. Confirm that the reported names and counts match the
requested scope; a zero-test run does not establish validation.

### Local browser and persistence tests

Build the whole solution with Aspire stopped, then start Aspire and wait for
`pino` to be healthy. The helper discovers the application, Mailpit and PostgreSQL
connection directly from this checkout's running AppHost. It keeps credentials
in the child process environment, never in source or printed output.

```powershell
dotnet build Pino.slnx
aspire start --non-interactive
aspire wait pino --timeout 120 --non-interactive
pwsh ./scripts/Invoke-BrowserTests.ps1 -BrowserChannel msedge -ArtifactsDirectory TestResults/browser
aspire stop --non-interactive
```

Use `-BrowserChannel chrome` for installed Chrome. To use Playwright's bundled
Chromium, install it once with
`pwsh tests/Pino.BrowserTests/bin/Debug/net10.0/playwright.ps1 install chromium`
and omit `-BrowserChannel`. The helper uses `--no-build` to avoid Windows file
locks; rebuild before starting Aspire whenever test or application source changes.

Each test creates unique disposable accounts and a club. Cleanup deletes only
the exact accounts and clubs owned by that test, queues its images for the normal
cleanup worker, and removes its captured Mailpit messages. It never resets the
database or volume. Tests refuse non-local hosts and databases other than `pino`.
Final diagnostic captures are best-effort. Every cleanup stage runs even when an
earlier capture or cleanup stage fails; cleanup failures are reported together.
Do not use this helper against a deployed application. Abruptly killing a test
process can leave its clearly named test records for manual cleanup.

For another local harness, supply `PINO_BROWSER_URL`, `PINO_MAILPIT_URL` and
`PINO_TEST_DATABASE`; optional variables are `PINO_BROWSER_CHANNEL` and
`PINO_BROWSER_ARTIFACTS`. Both browser journeys and the cleanup tests must execute with zero skips for
browser validation. Test artifacts are optional and ignored by Git.

### Optional reports and coverage

TRX and coverage are opt-in. Collect coverage from the headless projects:

```powershell
dotnet test --project tests/Pino.UnitTests/Pino.UnitTests.csproj --report-trx --coverage --coverage-output-format cobertura --results-directory TestResults
dotnet test --project tests/Pino.ComponentTests/Pino.ComponentTests.csproj --report-trx --coverage --coverage-output-format cobertura --results-directory TestResults
```

Reports go into the ignored `TestResults` directory; there is no coverage
percentage gate. Do not infer browser behavior or database correctness from
these headless coverage reports.

### Empty-filter diagnostic

Use this only when checking test-discovery failure behavior. This deliberately
unmatched filter must fail instead of silently passing an empty run:

```powershell
dotnet test --project tests/Pino.UnitTests/Pino.UnitTests.csproj --filter-method NoSuchTestMustNotExist
```

MTP reports exit code 8 (zero tests); the outer `dotnet test` command returns a
nonzero exit code.

## Configuration

### Assertions

Use the centrally pinned Shouldly version for every assertion in the test
projects. Import `Shouldly` in test files and use APIs such as `ShouldBe`,
`ShouldBeTrue`, `ShouldBeEmpty`,
`ShouldHaveSingleItem`, and `Should.Throw<T>`. xUnit attributes such as `[Fact]`,
`[Theory]`, and `[InlineData]` continue to define tests.

Do not add the xUnit assertion package, FluentAssertions, or use bUnit assertion
helpers such as `MarkupMatches`. For semantic HTML checks, obtain differences
with `component.CompareTo(expectedMarkup)` and assert `differences.ShouldBeEmpty()`.
This preserves bUnit's semantic comparison instead of comparing raw HTML strings.
Use Shouldly inside `WaitForAssertionAsync` when awaiting a render.

Preserve the strength of existing checks during migrations: single-item checks
must still verify cardinality, and exact exception-type checks use
`Should.Throw<T>(action).ShouldBeOfType<T>()` so derived exceptions do not pass.

### Runner settings

Each project's `testconfig.json` uses xUnit's MTP configuration section:

- `failWarns: true` fails tests that produce xUnit warnings.
- `parallelMode: "all"` permits independent tests, including methods and theory
  rows within the same class, to run in parallel (requires xUnit v3 4.0+).
- `parallelAlgorithm: "conservative"` starts another test when an execution slot
  becomes available, bounding the number of active tests.
- `maxParallelThreads: "1x"` sets that limit to the logical processor count,
  adapting to the developer machine or build agent.
- `preEnumerateTheories: true` discovers each theory data row separately.

MTP automatically copies and renames the file to
`<AssemblyName>.testconfig.json` beside each executable. No manual copy item or
separate `xunit.runner.json` is needed.

### Writing tests for parallel execution

Give each test its own mutable state and resources. Shared fixtures must support
concurrent access. In `all` mode, putting tests in the same class or named
collection does not serialize them. Tests that require exclusive access must
explicitly opt out, for example with `[Fact(DisableParallelism = true)]` or a
`[CollectionDefinition("Exclusive", DisableParallelization = true)]` applied to
the relevant collection. Keep these exceptions narrow and document the shared
resource that requires them.

The default aims to use all available processors for CPU-bound unit tests while
limiting scheduling and memory overhead. If a future suite spends substantial
time awaiting I/O, measure before increasing the multiplier or choosing
`aggressive`; aggressive scheduling changes timing and timeout behavior.

MTP's `--max-parallel-test-modules` controls parallel execution of test modules
separately and defaults to the processor count. With multiple test projects,
budget module concurrency together with each project's xUnit concurrency.

### Writing component tests

Use `BunitContext` and `Render<TComponent>()` (the bUnit 2 APIs). Create a fresh
context inside each test with `await using`; do not share a context through static
state or class/collection fixtures. This keeps the renderer, services, component
state, and JS interop setup isolated while methods and theory rows run in parallel.
Avoid changing bUnit's static defaults from individual tests.

Assert rendered DOM values with Shouldly, or use `CompareTo` followed by
`ShouldBeEmpty` for semantic markup. Dispatch UI events with helpers such as
`ClickAsync`, then use `WaitForAssertionAsync` for Shouldly assertions
that depend on a render; awaiting an event handler alone does not guarantee its
render cycle has finished. Register component dependencies in `context.Services`
before rendering and configure required calls on `context.JSInterop` (strict by
default). Keep tests in C# files; any future Razor test helpers must follow the
repository's matching code-behind policy.

bUnit tests run in process without starting Aspire, a web server, or a browser.
They verify component behavior, not browser layout, real JavaScript execution, or
server/WebAssembly render-mode transitions; those need browser-level tests.

Club persistence also requires integration checks through Aspire: concurrent
requests/approvals and last-administrator changes, cross-club authorization,
antiforgery, private photo access and replacement cleanup. Use disposable users,
and wait for interactive controls and server-confirmed notices before asserting
results. Static prerendered content is not evidence that a browser event ran.

## Visual Studio Code

Install and enable the recommended **C# Dev Kit** and **C#** extensions from
[`.vscode/extensions.json`](../.vscode/extensions.json), using a VS Code version
supported by those extensions. The editor versions used during initial validation
are recorded in [validation-history.md](validation-history.md); the repository
does not pin editor or extension versions.

1. Open the repository root and let C# Dev Kit load `Pino.slnx`.
2. Build the solution and open the **Testing** view. Allow C# Dev Kit to finish
   loading the solution and discovering the tests; an empty view during startup
   is temporary. Diagnose discovery through the extension's output if tests
   remain absent; verify that the repository CLI commands work as well.
3. Expand the project, namespace, class, and theories to see individual cases.
4. Use Test Explorer or editor gutter actions to run or debug tests. Set
   breakpoints in the test and its production code (`IdentityRedirectManager` or
   `Counter.razor.cs`) to step through both.
5. Use **Run Tests with Coverage** to show coverage in the editor.

Keep automatic discovery and build-on-refresh/run enabled (the defaults). Keep
experimental source-only discovery (`dotnet.testWindow.discoverTestsFromSource`)
disabled. No `launch.json` or custom test protocol setting is required. In
particular, do not add the historical
`dotnet.testWindow.useTestingPlatformProtocol`. If discovery changes after an
extension update, verify the installed extension's supported settings before
introducing workarounds.

## Recorded validation

Past test counts, coverage measurements, and editor acceptance checks are kept in
[validation-history.md](validation-history.md). They document dated runs, not the
current state of the suite. Report results from the commands run for each change.

## References

- [Shouldly 4.3.0](https://www.nuget.org/packages/Shouldly/4.3.0)
- [Shouldly assertion documentation](https://docs.shouldly.org/)
- [xUnit framework and assertion package separation](https://xunit.net/docs/nuget-packages-v3)
- [bUnit project setup](https://bunit.dev/docs/getting-started/create-test-project.html)
- [bUnit 2.11.3 release](https://github.com/bUnit-dev/bUnit/releases/tag/v2.11.3)
- [bUnit context and rendering](https://bunit.dev/api/Bunit.BunitContext.html)
- [bUnit event dispatch and asynchronous rendering](https://bunit.dev/docs/interaction/trigger-event-handlers.html)
- [xUnit MTP setup](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform)
- [xUnit testconfig.json](https://xunit.net/docs/config-testconfig-json)
- [xUnit parallel execution and opt-outs](https://xunit.net/docs/running-tests-in-parallel)
- [.NET 10 dotnet test integration](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test)
- [MTP test-module concurrency](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test-mtp#options)
- [Microsoft code coverage extension](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-extensions-code-coverage)
- [VS Code C# testing](https://code.visualstudio.com/docs/csharp/testing)
- [C# Dev Kit 3.40.210 manifest](https://ms-dotnettools.gallery.vsassets.io/_apis/public/gallery/publisher/ms-dotnettools/extension/csdevkit/3.40.210/assetbyname/Microsoft.VisualStudio.Code.Manifest)
