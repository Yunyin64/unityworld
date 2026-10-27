param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [string]$OutputDirectory = ''
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $RepoRoot '.local-verification\battle-observer'
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$project = Join-Path $RepoRoot 'tools\client-observer\ClientObserver.csproj'
& dotnet build $project --nologo -v:q
if ($LASTEXITCODE -ne 0) { throw 'Client observer build failed.' }
$runner = Join-Path $RepoRoot 'tools\client-observer\bin\Debug\net8.0\ClientObserver.dll'

function Invoke-CaseRun([string]$RunName, [string[]]$ExtraArgs) {
    $frames = Join-Path $OutputDirectory ($RunName + '.jsonl')
    $log = Join-Path $OutputDirectory ($RunName + '.log')
    & dotnet $runner "--root=$RepoRoot" "--output=$frames" @ExtraArgs *> $log
    if ($LASTEXITCODE -ne 0) { Get-Content -LiteralPath $log -Tail 12; throw "Run failed: $RunName" }
    Get-Content -LiteralPath $log -Tail 1
    return $frames
}

# Every invocation starts a fresh process, including replay from the complete captured input.
$baselineA = @(Invoke-CaseRun 'baseline-a' @('--no-event-observer'))[-1]
$baselineB = @(Invoke-CaseRun 'baseline-b' @('--no-event-observer'))[-1]
$observed = @(Invoke-CaseRun 'high-observation' @('--observe-repeat=25'))[-1]
$automatic = @(Invoke-CaseRun 'automatic-multitick' @('--mode=play', '--observe-repeat=3'))[-1]
$replay = @(Invoke-CaseRun 'replay' @("--case-input=$baselineA.case.json", '--observe-repeat=25'))[-1]
$comparisons = @()
foreach ($candidate in @($baselineB, $observed, $automatic, $replay)) {
    $message = & dotnet $runner compare $baselineA $candidate
    if ($LASTEXITCODE -ne 0) { throw "Comparison failed: $candidate / $message" }
    Write-Output $message
    $comparisons += [ordered]@{ candidate = [IO.Path]::GetFileName($candidate); result = "$message" }
}
$null = Invoke-CaseRun 'fingerprint-error' @('--expected-fingerprint=deliberate-verification-mismatch', '--expect-initialization-error')
$null = Invoke-CaseRun 'sink-error' @('--expect-sink-error')

# Verify the comparator reports the first actual field mismatch, rather than only final results.
$changedPath = Join-Path $OutputDirectory 'deliberate-field-difference.jsonl'
$firstTwo = @(Get-Content -LiteralPath $baselineA -TotalCount 2)
$changedFrame = $firstTwo[1] | ConvertFrom-Json
$changedFrame.snapshot.participants[0].hp += 1
$changedLines = @($firstTwo[0], ($changedFrame | ConvertTo-Json -Depth 100 -Compress))
[IO.File]::WriteAllLines($changedPath, $changedLines, [Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath "$baselineA.case.json" -Destination "$changedPath.case.json"
$difference = & dotnet $runner compare $baselineA $changedPath 2>&1
if ($LASTEXITCODE -ne 2 -or "$difference" -notmatch 'line=2 .*participants\[0\]\.hp') {
    throw "First field difference was not identified: $difference"
}

$last = Get-Content -LiteralPath $baselineA -Tail 1 | ConvertFrom-Json
$identity = Get-Content -LiteralPath "$baselineA.identity.json" -Raw | ConvertFrom-Json
$records = @(Get-Content -LiteralPath "$replay.records.jsonl" | ForEach-Object { $_ | ConvertFrom-Json })
$events = @($records | Where-Object source -eq 'event')
$traces = @($records | Where-Object source -eq 'trace')
if ($traces.Count -ne $last.trace.Count -or @($events.eventId | Select-Object -Unique).Count -ne $events.Count -or
    @($records.sequence | Select-Object -Unique).Count -ne $records.Count) { throw 'Duplicate or missing observer records.' }
$summary = [ordered]@{
    verifiedAtUtc = [DateTime]::UtcNow.ToString('o')
    case = $identity.case
    dataFileCount = $identity.dataIdentity.files.Count
    frames = $last.snapshot.tick + 1
    result = $last.snapshot.result
    worldTick = $last.snapshot.worldTick
    comparisons = $comparisons
    highObservationReadsPerTick = 25
    automaticSummary = (Get-Content -LiteralPath (Join-Path $OutputDirectory 'automatic-multitick.log') -Tail 1)
    records = [ordered]@{ total = $records.Count; events = $events.Count; uniqueEventIds = $events.Count; traces = $traces.Count }
    changedDataRejected = $true
    frameSinkErrorStoppedWithoutFalseResult = $true
    firstFieldDifference = "$difference"
    artifactsDirectory = $OutputDirectory
    scope = 'Shared adapter verification; actual Godot interaction and original console baseline are separate evidence.'
}
$summary | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'verification-summary.json') -Encoding utf8
Write-Output "OBSERVER_VERIFICATION_OK $OutputDirectory"
