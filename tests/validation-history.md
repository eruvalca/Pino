# Test validation history

These records preserve the validation reported during the initial test setup and
coverage expansion on September 26-28, 2026. They describe those runs, including
older assertion and parallelism settings, and have not been rerun as part of the
documentation review. Counts, coverage, source line numbers, and editor behavior
are historical evidence rather than guarantees about the current checkout.

Use the [current test guide](README.md) for commands and conventions. Run the
appropriate checks to establish results for a new change.

## Recorded dependency and editor baseline

| Component | Version |
| --- | --- |
| .NET SDK | 10.0.401 |
| bunit (component project only) | 2.11.3 |
| xunit.v3.core.mtp-v2 | 4.0.1 |
| Shouldly | 4.3.0 |
| NSubstitute (account tests) | 6.2.0 |
| Bogus (locally seeded fake data) | 35.6.5 |
| Microsoft.Testing.Platform / Platform.MSBuild | 2.4.1 |
| Microsoft.Testing.Extensions.TrxReport | 2.4.1 |
| Microsoft.Testing.Extensions.CodeCoverage | 18.11.2 |
| xunit.analyzers | 2.1.0 |
| C# Dev Kit (stable baseline) | 3.40.210 |

## Coverage expansion (September 28, 2026)

The September 28, 2026 run recorded **413 passing cases**: 184 unit tests and
229 component tests, up from 235 cases. The 178 additions cover account management, authentication
revalidation, endpoint responses, shared component contracts, navigation, and
validation boundaries. They also verify failure paths do not write account data,
refresh sessions, send email, or generate credentials unexpectedly.

Validation completed with zero build warnings/errors and zero failed/skipped tests:

```powershell
dotnet build Pino.slnx --no-incremental
dotnet test --solution Pino.slnx --no-build --report-trx --coverage --coverage-output-format cobertura --results-directory TestResults/final
```

Coverage below merges both projects' Cobertura reports by distinct source filename
and line number, counting a line covered when either project executes it. It
measures `Pino`, `Pino.UI`, and `Pino.ServiceDefaults`; it is line coverage,
not a claim of complete branch or browser coverage. The collector configuration
was not changed to exclude uncovered files.

| Measured scope | Before | After |
| --- | --- | --- |
| Handwritten application code, excluding generated files, migrations, and startup | 918 / 1,480 (62.03%) | 1,449 / 1,480 (97.91%) |
| All instrumented production code, including those categories | 1,100 / 2,954 (37.24%) | 1,794 / 2,954 (60.73%) |

The 31 remaining application lines comprise 20 defensive guard lines whose states
are prevented by initialization or absent forms, three OTLP exporter configuration
lines, and eight trivial lines (no-op email methods, layout/content slots, the
database-context constructor, and the fixed revalidation interval). Tests do not
mutate cached private state or invoke private event handlers merely to cover them.
Generated logging/union code, migrations, startup, build-tool execution, actual
WebAuthn JavaScript, database persistence, and live external providers remain
outside this unit/component testing scope.

Endpoint tests invoke mapped delegates against in-memory HTTP contexts with
substituted dependencies; they never start their application or send a network
request. Revalidation tests invoke the framework's protected validation hook
directly, avoiding its 30-minute background timer. bUnit tests use actual form
events; the existing form helper supplies posted values only where bUnit does not
run static SSR form mapping. For example, an empty phone string fails validation,
while an explicitly null posted phone value removes the saved number.

## Initial test scope

The account outcome suite exercises the real sign-in, passkey, registration,
email-change, and two-factor services with substituted Identity dependencies.
It verifies each outcome, payloads, operation ordering, short-circuited failures,
credential/token decoding, and the passkey limit. Component tests exercise the
real pages and services to check validation, redirects, account-privacy responses,
partial-failure messages, and recovery-code rendering. Every test owns its mutable
state; no database, Aspire process, browser, or external provider is required.

The following initial-scope descriptions and acceptance records document the
original test setup before the account outcome suite was added.

The 16 server unit cases cover null/empty/relative destinations, absolute destinations within
the application, rejection of external destinations, query replacement and
encoding, current-page redirects, and status-cookie values and attributes.
Tests use an in-memory recording `NavigationManager` and `DefaultHttpContext`;
they do not start the web server, Aspire, a browser, or a database.

The five component cases exercise the real `Counter`: initial markup, count updates
after one/two/five clicks, and independent state in separate contexts.

## Acceptance record

Verified on September 26, 2026, on Windows x64 with .NET SDK 10.0.401,
VS Code **1.139.1**, C# Dev Kit **3.40.210**, and C# **2.160.4**.
The initial acceptance checks below used `parallelMode: "collections"` and xUnit
assertions; the subsequent parallel configuration and Shouldly migration are
documented separately. Source line references in historical editor checks refer
to the files as they existed during those checks.

| Requirement | Evidence |
| --- | --- |
| Full solution builds | `dotnet build Pino.slnx`: 0 warnings, 0 errors. |
| Project execution | `dotnet test --project tests/Pino.UnitTests/Pino.UnitTests.csproj`: 16 passed, 0 failed, 0 skipped. |
| Solution execution and reports | The solution command above passed all 16 cases; TRX counters and Cobertura XML were parsed successfully. |
| Exact dependencies and analyzers | `project.assets.json` resolved the versions above; MSBuild `ResolveReferences` included xUnit and all shared analyzers. Nullable and warnings-as-errors remained enabled. |
| Configuration deployment | Output contained `Pino.UnitTests.testconfig.json` with the configured xUnit options. |
| Empty selection fails | The unmatched `--filter-method` command returned nonzero; MTP reported exit code 8. |
| Editor discovery | C# Dev Kit discovered all 16 cases, including facts and individual theory rows. Discovery was automatic; no manual refresh command was exposed. |
| Editor execution | Run All and class execution passed 16/16; an individual fact and the null-input theory row each passed 1/1. The fact was run from its editor gutter. |
| Editor debugging | Debug Test paused at `IdentityRedirectManagerTests.cs:102` and `IdentityRedirectManager.cs:29`, then completed. Temporary breakpoints were removed. |
| Build on run and failure display | Temporarily changing the current-page assertion produced 15/16 passing and an `Assert.Equal` failure without a manual build. The assertion was restored. |
| Editor coverage | Run Tests with Coverage rebuilt the restored source, passed 16/16, populated Test Coverage, and displayed covered-line decorations in the redirect manager. |
| Reload | After Developer: Reload Window and solution initialization, discovery returned automatically and Run All passed 16/16 again. |

After enabling `all` / `conservative` / `1x`, `dotnet build Pino.slnx` passed
with 0 warnings and 0 errors. Running
`dotnet test --project tests/Pino.UnitTests/Pino.UnitTests.csproj --no-build`
passed all 16 tests (0 failed, 0 skipped). The deployed
`Pino.UnitTests.testconfig.json` contained the updated parallel settings.

The source assertions were reviewed against the requested behavior matrix:

| Requirement | Evidence |
| --- | --- |
| Null, empty, and relative destinations | `RedirectToRelativeDestinationNavigatesOnce` (4 rows). |
| Absolute destinations inside the application | `RedirectToAbsoluteDestinationWithinApplicationConvertsToRelativePath` (3 rows). |
| Rejected external absolute destinations | `RedirectToAbsoluteDestinationOutsideApplicationThrowsWithoutNavigating` (4 rows). |
| Query replacement and encoding | `RedirectToQueryParametersReplacesExistingQueryAndEncodesValues`. |
| Empty replacement query | `RedirectToEmptyQueryParametersRemovesExistingQueryAndFragment`. |
| Current-page redirect | `RedirectToCurrentPageRemovesQueryAndFragment`. |
| Status-cookie contents and attributes | `RedirectToWithStatusWritesStatusCookieAndNavigates` and `RedirectToCurrentPageWithStatusWritesStatusCookieAndRemovesQueryAndFragment`. |

### Component project acceptance

The bUnit addition was verified on September 26, 2026 with the same SDK and editor
versions above, using `all` / `conservative` / `1x` in both projects.

| Requirement | Evidence |
| --- | --- |
| bUnit version checked at the time | NuGet's live version feed and the published release identify 2.11.3; the restored assets resolve that exact version. |
| Full build | `dotnet build Pino.slnx --no-incremental`: 0 warnings, 0 errors. |
| Direct component execution | `dotnet test --project tests/Pino.ComponentTests/Pino.ComponentTests.csproj --no-build`: 5 passed, 0 failed, 0 skipped. |
| Solution execution and reports | The solution coverage command passed 21/21; separate TRX and Cobertura reports were generated for both projects. The component TRX contains five passing results, and Cobertura includes `Counter.razor` and `Counter.razor.cs`. |
| Inherited checks and deployed settings | MSBuild reports `IsTestProject=true`, nullable enabled, warnings-as-errors enabled, and xUnit/Meziantou/Sonar/Roslynator analyzers. The output contains `Pino.ComponentTests.testconfig.json` with the parallel settings above. |
| Empty selection fails | The component project's unmatched `--filter-method NoSuchTestMustNotExist` returned nonzero; MTP reported exit code 8. |
| Editor discovery and execution | C# Dev Kit automatically discovered both projects, facts, and all three component theory rows. Run All passed 21/21; running only the two-click theory row passed 1/1. |
| Editor debugging | Debug Test for `IndependentContextsKeepCounterStateIsolatedAsync` hit breakpoints at `CounterTests.cs:52` and `Counter.razor.cs:9`, then passed 1/1. Both temporary breakpoints were removed. |
| Editor gutter execution | Run Test from the gutter beside `IndependentContextsKeepCounterStateIsolatedAsync` passed 1/1. |
| Editor coverage | Run Tests with Coverage passed 21/21, populated Test Coverage, and displayed covered-line decorations on `Counter.razor.cs`. |

| Component behavior | Exact test evidence |
| --- | --- |
| Initial markup, status, and button | `CounterTests.RenderShowsInitialCountAndIncrementButtonAsync` uses semantic markup comparison. |
| One, two, and five clicks update the displayed status | `CounterTests.ClickingIncrementButtonUpdatesDisplayedCountAsync` (three theory rows) dispatches real Blazor click events and awaits the rendered assertion. |
| Component state is isolated | `CounterTests.IndependentContextsKeepCounterStateIsolatedAsync` verifies clicking in one context leaves the other at zero. |

### Shouldly migration acceptance

Verified on September 26, 2026: `dotnet build Pino.slnx` completed with
0 warnings and 0 errors, and `dotnet test --solution Pino.slnx --no-build`
completed with 21 passed, 0 failed, and 0 skipped. Both projects resolve Shouldly
4.3.0 and `xunit.v3.core.mtp-v2` 4.0.1, retain xUnit analyzers 2.1.0, and no longer
resolve `xunit.v3.assert`. All existing assertion calls were migrated to Shouldly,
including the component's semantic markup difference check. The parallel and MTP
runner settings are unchanged.
