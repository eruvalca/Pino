[CmdletBinding()]
param(
    [string] $BrowserChannel = "",
    [string] $ArtifactsDirectory = "",
    [string] $PlayerCsvPath = ""
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
Push-Location $repo
$names = @('PINO_BROWSER_URL', 'PINO_MAILPIT_URL', 'PINO_TEST_DATABASE', 'PINO_BROWSER_CHANNEL', 'PINO_BROWSER_ARTIFACTS', 'PINO_PLAYER_CSV_PATH')
$previous = @{}
foreach ($name in $names) { $previous[$name] = [Environment]::GetEnvironmentVariable($name) }
try {
    # Build the solution before starting Aspire. The running web process locks shared outputs on Windows.
    $raw = (aspire describe --non-interactive --format Json | Out-String)
    if ($LASTEXITCODE -ne 0) { throw 'Start Aspire and wait for pino before running browser tests.' }
    $state = $raw.Substring($raw.IndexOf('{')) | ConvertFrom-Json
    $web = $state.resources | Where-Object displayName -EQ 'pino'
    $mail = $state.resources | Where-Object displayName -EQ 'mailpit'
    if ($web.healthStatus -ne 'Healthy' -or $mail.healthStatus -ne 'Healthy') { throw 'Pino and Mailpit must be healthy.' }
    # Node's API client does not resolve Chromium's special *.localhost aliases.
    $env:PINO_BROWSER_URL = ($web.urls | Where-Object { $_.name -eq 'https' -and ([Uri]$_.url).Host -eq 'localhost' } | Select-Object -First 1).url
    $env:PINO_MAILPIT_URL = ($mail.urls | Where-Object name -EQ 'http' | Select-Object -First 1).url
    $env:PINO_TEST_DATABASE = $web.environment.ConnectionStrings__pinodb
    $env:PINO_BROWSER_CHANNEL = $BrowserChannel
    $env:PINO_BROWSER_ARTIFACTS = if ($ArtifactsDirectory) { [IO.Path]::GetFullPath($ArtifactsDirectory) } else { '' }
    $env:PINO_PLAYER_CSV_PATH = if ($PlayerCsvPath) { (Resolve-Path -LiteralPath $PlayerCsvPath).Path } else { '' }
    dotnet test --project tests/Pino.BrowserTests/Pino.BrowserTests.csproj --no-build
    $result = $LASTEXITCODE
}
finally {
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $previous[$name]) }
    Pop-Location
}
exit $result
