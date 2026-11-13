param(
    [Parameter(Mandatory)][string]$Godot,
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,
    [string]$OutputDirectory = ''
)
$ErrorActionPreference = 'Stop'
$engine = (Get-Command $Godot).Source
if (!$OutputDirectory) { $OutputDirectory = Join-Path $RepoRoot '.local-verification\battle-windows' }
# 每次使用独立目录，防止把上次完成标记或旧进程号用于本次检查。
$run = Join-Path ([IO.Path]::GetFullPath($OutputDirectory)) ([Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run | Out-Null
function Start-Game([string[]]$Arguments) {
    $start = [Diagnostics.ProcessStartInfo]::new($engine)
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.WorkingDirectory = $RepoRoot
    foreach ($argument in $Arguments) { $start.ArgumentList.Add($argument) }
    return [Diagnostics.Process]::Start($start)
}
function Wait-Game($Child) {
    if (!$Child.WaitForExit(120000)) {
        $Child.Kill($true)
        throw "游戏验收超时，进程号 $($Child.Id)"
    }
    if ($Child.ExitCode -ne 0) { throw "游戏验收失败，退出码 $($Child.ExitCode)，输出位于 $run" }
}
function Wait-File([string]$File) {
    $watch = [Diagnostics.Stopwatch]::StartNew()
    while (!(Test-Path -LiteralPath $File)) {
        if ($watch.Elapsed.TotalSeconds -gt 90) { throw "等待验收输出超时 $File" }
        Start-Sleep -Milliseconds 100
    }
}
function Compare-Observation([string]$Left, [string]$Right) {
    foreach ($name in @('input.json', 'records.jsonl', 'presentation-v1.jsonl')) {
        $a = [IO.File]::ReadAllLines((Join-Path $Left $name))
        $b = [IO.File]::ReadAllLines((Join-Path $Right $name))
        if ($a.Count -ne $b.Count) { throw "$name 行数不同" }
        for ($i = 0; $i -lt $a.Count; $i++) {
            # 各次运行拥有独立的表现会话 ID。案例、快照、事件序号及完整战报严格比较。
            $lineA = $a[$i] -creplace '"sessionId":"[0-9a-f]{32}"', '"sessionId":"session"'
            $lineB = $b[$i] -creplace '"sessionId":"[0-9a-f]{32}"', '"sessionId":"session"'
            if ($lineA -cne $lineB) { throw "$name 在第 $($i + 1) 行不同" }
        }
    }
}
$check = 'res://scenes/tests/battle_window_check.tscn'
& dotnet build (Join-Path $RepoRoot 'Xianxia.Client.csproj') --nologo -v:q
if ($LASTEXITCODE -ne 0) { throw '客户端构建失败。' }

$hostRun = Start-Game @('--rendering-method', 'gl_compatibility', '--path', $RepoRoot, '--log-file', (Join-Path $run 'current.log'), $check, '--', "--battle-window-check=$run")
Wait-Game $hostRun
Wait-File (Join-Path $run 'result.txt')
Get-Content -LiteralPath (Join-Path $run 'result.txt')

$visible = Join-Path $run 'new-window'
New-Item -ItemType Directory -Path $visible | Out-Null
foreach ($mode in @('step', 'auto')) {
    $restarts = if ($mode -eq 'step') { 1 } else { 0 }
    $game = Start-Game @('--path', $RepoRoot, '--rendering-method', 'gl_compatibility',
        '--log-file', (Join-Path $visible "$mode.log"), 'res://scenes/battle/battle_observer.tscn',
        '--', '--battle-observer', "--observer-case-input=$(Join-Path $run 'case.json')",
        "--observer-verify=$visible", "--observer-mode=$mode", '--observer-run=1', "--observer-restarts=$restarts")
    Wait-Game $game
    foreach ($index in 1..(1 + $restarts)) {
        Wait-File (Join-Path $visible "godot-$mode-$index.result.json")
        $directory = [IO.File]::ReadAllText((Join-Path $visible "godot-$mode-$index.session.txt"))
        if ([IO.File]::ReadAllText((Join-Path $directory 'window-mode.txt')) -ne 'NewWindow') { throw '窗口模式错误。' }
        # 独立观察器会在结果写盘后退出，等其释放文件句柄再比较。
        $ownerId = [int]([IO.Path]::GetFileName($directory).Split('-')[-1])
        $ownerProcess = Get-Process -Id $ownerId -ErrorAction SilentlyContinue
        if ($ownerProcess -and !$ownerProcess.WaitForExit(20000)) { throw '独立窗口未正常退出。' }
        Compare-Observation (Join-Path $run 'current-step') $directory
    }
}

$abrupt = Join-Path $run 'parent-exit'
New-Item -ItemType Directory -Path $abrupt | Out-Null
$original = Start-Game @('--headless', '--path', $RepoRoot, '--log-file', (Join-Path $abrupt 'host.log'),
    $check, '--', "--battle-window-check=$abrupt", '--battle-window-parent-exit-check')
Wait-File (Join-Path $abrupt 'host.pid')
$hostInstance = Get-Process -Id ([int][IO.File]::ReadAllText((Join-Path $abrupt 'host.pid')))
$workerInstance = Get-Process -Id ([int][IO.File]::ReadAllText((Join-Path $abrupt 'worker.pid')))
# 使用这次启动后落盘的单独进程号。只结束父进程，检验子进程真的能检测 EOF。
$hostInstance.Kill()
if (!$workerInstance.WaitForExit(15000)) { $workerInstance.Kill(); throw '父进程退出后 worker 残留。' }
"WINDOW_REPLAY_MATCH modes=2 ticks=0..16 restarts=2+1 records=equal parent-exit=clean results=$run"
