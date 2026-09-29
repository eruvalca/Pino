#requires -Version 7.0
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot
$hookScript = Join-Path $PSScriptRoot 'Invoke-DocumentationReviewHook.ps1'
$fixtureRoot = Join-Path $repoRoot ('artifacts/documentation-hook-tests/' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixtureRoot -Force | Out-Null
$passed = 0

function Invoke-FixtureGit([string[]] $Arguments) {
    $output = & git -C $fixtureRoot @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Fixture Git command failed: $($output -join "`n")"
    }
}

function Set-FixtureFile([string] $Path, [string] $Content) {
    $fullPath = Join-Path $fixtureRoot $Path
    New-Item -ItemType Directory -Path (Split-Path $fullPath) -Force | Out-Null
    [IO.File]::WriteAllText($fullPath, $Content)
}

function Invoke-Hook([string] $EventName, [string] $Turn, [hashtable] $Extra = @{}) {
    $payload = @{
        hook_event_name = $EventName
        session_id = 'fixture-session'
        turn_id = $Turn
        cwd = $fixtureRoot
        permission_mode = 'default'
    }
    foreach ($key in $Extra.Keys) {
        $payload[$key] = $Extra[$key]
    }
    $output = $payload | ConvertTo-Json -Compress | & pwsh -NoProfile -NonInteractive -File $hookScript
    if ($LASTEXITCODE -ne 0) {
        throw "Hook exited $LASTEXITCODE."
    }
    return ($output -join "`n") | ConvertFrom-Json -AsHashtable
}

function Assert-Result([string] $Name, [bool] $Condition) {
    if (-not $Condition) {
        throw "FAIL: $Name"
    }
    $script:passed++
    Write-Host "PASS: $Name"
}

# All mutations and commits below belong to this isolated, ignored test repo.
Invoke-FixtureGit @('init', '--quiet')
Invoke-FixtureGit @('config', 'user.name', 'Hook fixture')
Invoke-FixtureGit @('config', 'user.email', 'hook-fixture@example.invalid')
Invoke-FixtureGit @('config', 'commit.gpgsign', 'false')
Invoke-FixtureGit @('config', 'core.hooksPath', '.disabled-hooks')
Set-FixtureFile '.gitignore' "artifacts/`n"
Set-FixtureFile 'README.md' "# Fixture`n"

$result = Invoke-Hook 'UserPromptSubmit' 'unborn'
Assert-Result 'Initial repository receives pre-commit review instructions' (
    $result.hookSpecificOutput.additionalContext -match 'Before any authorized commit')
Assert-Result 'Unchanged untracked files do not trigger a finishing pass' (
    (Invoke-Hook 'Stop' 'unborn').Count -eq 0)
Set-FixtureFile 'new file.md' "# New document`n"
Assert-Result 'New untracked file triggers a review' (
    (Invoke-Hook 'Stop' 'unborn').decision -eq 'block')
Assert-Result 'Repeated Stop does not request another pass' (
    (Invoke-Hook 'Stop' 'unborn').Count -eq 0)

Invoke-FixtureGit @('add', '--all')
Invoke-FixtureGit @('commit', '--quiet', '-m', 'Fixture baseline')
Invoke-Hook 'UserPromptSubmit' 'clean' | Out-Null
Assert-Result 'Read-only turn in a clean repository stays quiet' (
    (Invoke-Hook 'Stop' 'clean').Count -eq 0)

Set-FixtureFile 'README.md' "# Existing dirty edit`n"
Invoke-Hook 'UserPromptSubmit' 'dirty' | Out-Null
Assert-Result 'Pre-existing dirty work alone stays quiet' (
    (Invoke-Hook 'Stop' 'dirty').Count -eq 0)
Set-FixtureFile 'README.md' "# Changed again in the turn`n"
$result = Invoke-Hook 'Stop' 'dirty'
Assert-Result 'Further edits to the same dirty file trigger review' (
    $result.decision -eq 'block' -and $result.reason -match 'README.md')

Invoke-Hook 'UserPromptSubmit' 'staging' | Out-Null
Invoke-FixtureGit @('add', 'README.md')
Assert-Result 'Index-only change is detected' (
    (Invoke-Hook 'Stop' 'staging').decision -eq 'block')
Invoke-Hook 'UserPromptSubmit' 'partial' | Out-Null
Set-FixtureFile 'README.md' "# Unstaged portion of an already-staged file`n"
Assert-Result 'Partial staging does not hide worktree edits' (
    (Invoke-Hook 'Stop' 'partial').decision -eq 'block')

Invoke-Hook 'UserPromptSubmit' 'commit' | Out-Null
Invoke-FixtureGit @('add', 'README.md')
Invoke-FixtureGit @('commit', '--quiet', '-m', 'Fixture change')
Assert-Result 'A commit during the turn still triggers review' (
    (Invoke-Hook 'Stop' 'commit').decision -eq 'block')

Invoke-Hook 'UserPromptSubmit' 'delete' | Out-Null
Remove-Item -LiteralPath (Join-Path $fixtureRoot 'new file.md')
Assert-Result 'Deleted files trigger review' (
    (Invoke-Hook 'Stop' 'delete').decision -eq 'block')

Invoke-Hook 'UserPromptSubmit' 'guard' | Out-Null
Set-FixtureFile 'source.cs' 'class Fixture;'
Assert-Result 'Codex stop_hook_active guard prevents continuation loops' (
    (Invoke-Hook 'Stop' 'guard' @{ stop_hook_active = $true }).Count -eq 0)
Assert-Result 'A different session cannot consume the baseline' (
    (Invoke-Hook 'Stop' 'guard' @{ session_id = 'other-session' }).Count -eq 0)
Assert-Result 'A missing baseline stays quiet' (
    (Invoke-Hook 'Stop' 'missing').Count -eq 0)
Assert-Result 'Plan-mode prompt does not set up a finishing pass' (
    (Invoke-Hook 'UserPromptSubmit' 'plan' @{ permission_mode = 'plan' }).Count -eq 0)
Assert-Result 'Plan-mode Stop never continues editing' (
    (Invoke-Hook 'Stop' 'guard' @{ permission_mode = 'plan' }).Count -eq 0)

# A duplicate prompt event must not overwrite the original comparison baseline.
Invoke-Hook 'UserPromptSubmit' 'duplicate' | Out-Null
Set-FixtureFile 'source.cs' 'class ChangedFixture;'
Invoke-Hook 'UserPromptSubmit' 'duplicate' | Out-Null
Assert-Result 'Duplicate start event preserves the original baseline' (
    (Invoke-Hook 'Stop' 'duplicate').decision -eq 'block')

$invalidJson = '{invalid' | & pwsh -NoProfile -NonInteractive -File $hookScript
Assert-Result 'Malformed input fails open with a JSON warning' (
    $LASTEXITCODE -eq 0 -and ($invalidJson | ConvertFrom-Json -AsHashtable).systemMessage)
$result = Invoke-Hook 'UserPromptSubmit' 'no-repo' @{ cwd = (Join-Path $fixtureRoot 'does-not-exist') }
Assert-Result 'Missing repository warns without dropping the early reminder' (
    $result.systemMessage -and $result.hookSpecificOutput.additionalContext)

# Execute the actual Windows launch command from a subdirectory, preserving stdin.
$manifest = Get-Content -LiteralPath (Join-Path $repoRoot '.codex/hooks.json') -Raw | ConvertFrom-Json
$startHandler = $manifest.hooks.UserPromptSubmit[0].hooks[0]
$stopHandler = $manifest.hooks.Stop[0].hooks[0]
Assert-Result 'Manifest registers separate project handlers and retains Impeccable' (
    $startHandler.type -eq 'command' -and $stopHandler.type -eq 'command' -and
    $manifest.hooks.PostToolUse[0].hooks[0].command -match 'impeccable' -and
    $manifest.hooks.Stop[1].hooks[0].command -match 'impeccable')

if ($IsWindows) {
    $scriptDirectory = Join-Path $fixtureRoot 'scripts'
    New-Item -ItemType Directory -Path $scriptDirectory -Force | Out-Null
    Copy-Item -LiteralPath $hookScript -Destination $scriptDirectory
    $payload = @{
        hook_event_name = 'UserPromptSubmit'
        session_id = 'launcher'
        turn_id = 'subdirectory'
        cwd = $scriptDirectory
    } | ConvertTo-Json -Compress
    Push-Location $scriptDirectory
    try {
        $output = $payload | & $env:ComSpec /d /s /c $startHandler.commandWindows
        $result = ($output -join "`n") | ConvertFrom-Json -AsHashtable
        Assert-Result 'Windows manifest launcher resolves from a subdirectory and reads stdin' (
            $LASTEXITCODE -eq 0 -and $result.hookSpecificOutput.additionalContext -and -not $result.systemMessage)
    }
    finally {
        Pop-Location
    }
}

Assert-Result 'Hook state is ignored by Git' (
    -not ((& git -C $fixtureRoot status --porcelain --untracked-files=all) -match 'artifacts/'))
Write-Host "Documentation hook checks: $passed passed, 0 failed, 0 skipped."
Write-Host "Fixture: $fixtureRoot"
