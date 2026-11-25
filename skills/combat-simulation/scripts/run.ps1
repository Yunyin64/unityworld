#requires -Version 7.4
param(
    [string[]]$Seeds = @('1207', '3169', '5519'),
    [int]$WorldSeed = 12345,
    [string]$Only,
    [string[]]$Pair,
    [string[]]$Templates,
    [switch]$ListTemplates,
    [switch]$HistoricalRoster,
    [ValidateSet('Roster', 'Baseline', 'All', 'Existing')]
    [string]$Scope = 'All',
    [ValidateSet('Both', 'Forward', 'Swapped')]
    [string]$Order = 'Both',
    [ValidateRange(1, 2147483647)][int]$Limit = 2500,
    [string[]]$ExtendPair,
    [ValidateSet('Forward', 'Swapped')]
    [string]$ExtendOrder = 'Forward',
    [Nullable[int]]$ExtendSeed,
    [int]$ExtendedLimit = 5000,
    [switch]$TraceResources,
    [switch]$TraceArtifactPhases,
    [string[]]$TraceManaPair,
    [string]$OutputPath = (Join-Path ([IO.Path]::GetTempPath()) "unityworld-combat-$([guid]::NewGuid()).json"),
    [string]$BenchmarkSubject,
    [string]$BenchmarkOpponent,
    [string]$MeasurementPlan,
    [string]$BenchmarkOutputDirectory
)

$ErrorActionPreference = 'Stop'
if (-not ($IsWindows -and [Environment]::Is64BitProcess)) { throw '后端模拟需要 Windows x64 和 PowerShell 7.4 或更高版本。' }
if ($BenchmarkSubject) {
    $supported = @('BenchmarkSubject','BenchmarkOpponent','MeasurementPlan','BenchmarkOutputDirectory','Seeds','WorldSeed','Limit')
    foreach ($option in $PSBoundParameters.Keys) {
        if ($option -notin $supported) { throw "木桩基准模式不支持 -$option。" }
    }
    $benchmarkDir = Join-Path $PSScriptRoot '../../../UnityWorld/tools/combat_benchmark'
    $options = @('--subject', $BenchmarkSubject, '--world-seed', $WorldSeed)
    if ($BenchmarkOpponent) { $options += @('--opponent', $BenchmarkOpponent) }
    if ($MeasurementPlan) {
        if ($MeasurementPlan -eq 'dummy-baseline.json' -and -not (Test-Path -LiteralPath $MeasurementPlan)) {
            $MeasurementPlan = Join-Path $benchmarkDir $MeasurementPlan
        }
        $options += @('--plan', $MeasurementPlan)
    }
    if ($PSBoundParameters.ContainsKey('Seeds')) { $options += "--seeds=$($Seeds -join ',')" }
    if ($PSBoundParameters.ContainsKey('Limit')) { $options += @('--limit', $Limit) }
    if ($BenchmarkOutputDirectory) { $options += @('--output', $BenchmarkOutputDirectory) }
    & python -X utf8 (Join-Path $benchmarkDir 'benchmark.py') @options
    exit $LASTEXITCODE
}
if ($BenchmarkOpponent -or $MeasurementPlan -or $BenchmarkOutputDirectory) { throw '请使用 -BenchmarkSubject 指定待测角色。' }
if (-not ($ListTemplates -or $Pair -or $Templates -or $Only -or $PSBoundParameters.ContainsKey('Scope'))) {
    throw '请选择 -ListTemplates、-Pair、-Templates、-Only 或 -Scope。'
}
if (($Pair -and $Templates) -or (($Pair -or $Templates) -and $PSBoundParameters.ContainsKey('Scope') -and -not $HistoricalRoster)) {
    throw '-Pair、-Templates、-Scope 分别选择对战范围，请只指定一种。'
}
# 同时支持 pwsh -File 的逗号字符串和 PowerShell 进程内调用时的数组。
[int[]]$battleSeeds = @($Seeds -split ',' | ForEach-Object { [int]::Parse($_.Trim()) })
if ($battleSeeds.Count -eq 0) { throw '至少需要一个战斗种子。' }
if ($Pair) { $Pair = @($Pair -split ',' | ForEach-Object { $_.Trim() }) }
if ($Templates) { $Templates = @($Templates -split ',' | ForEach-Object { $_.Trim() }) }
if ($ExtendPair) { $ExtendPair = @($ExtendPair -split ',' | ForEach-Object { $_.Trim() }) }
if ($TraceManaPair) {
    $TraceManaPair = @($TraceManaPair -split ',' | ForEach-Object { $_.Trim() })
    $TraceResources = $true
}
if ($ExtendPair -and ($ExtendPair.Count -ne 2 -or $ExtendedLimit -le $Limit)) {
    throw '延长复跑需要两位模板，且延长上限必须大于原上限。'
}
$repo = Get-Item -LiteralPath $PSScriptRoot
while ($repo -and -not (Test-Path -LiteralPath (Join-Path $repo.FullName 'UnityWorld/Console/UnityWorld.Console.csproj'))) {
    $repo = $repo.Parent
}
if (-not $repo) { throw '请使用 UnityWorld 仓库内的战斗模拟技能。' }
$root = Join-Path $repo.FullName 'UnityWorld'
$bin = Join-Path $root 'Console/bin/Debug/net8.0'
$engine = Join-Path $bin 'UnityWorld.dll'
if (-not (Test-Path -LiteralPath $engine)) { throw '请先在仓库根运行 dotnet build UnityWorld/Console/UnityWorld.Console.csproj。' }
$native = Join-Path $bin 'runtimes/win-x64/native'
$env:PATH = "$native;$env:PATH"
Get-ChildItem -LiteralPath $bin -Filter '*.dll' |
    Where-Object { $_.Name -notmatch '^(UnityWorld\.Console|System\.)' } |
    ForEach-Object { [void][System.Runtime.Loader.AssemblyLoadContext]::Default.LoadFromAssemblyPath($_.FullName) }

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
[UnityWorld.Core.LogMgr]::MinLevel = [UnityWorld.Core.LogLevel]::Error
[UnityWorld.Game.World.WorldMgr]::Initialize($WorldSeed, $root, 'china-inspired-v1')
try {
    [UnityWorld.Game.World.WorldMgr]::Start()
    $defines = [UnityWorld.Game.Data.NpcDefineMgr]::Instance
    $catalog = @($defines.GetAll() | Sort-Object ID)
    if ($ListTemplates) {
        $catalog | Select-Object ID, DisplayName | Format-Table -AutoSize
        return
    }
    $rosterIds = @($catalog | Where-Object ID -Like 'roster_*' | ForEach-Object ID)
    $existingIds = @($catalog | Where-Object ID -NotLike 'roster_*' | ForEach-Object ID)
    if ($HistoricalRoster) {
        # 旧表按文件名排列新模板，旧模板顺序也会影响同 Tick 的扫描和实例 ID。
        $rosterIds = @(Get-ChildItem -LiteralPath (Join-Path $root 'Data/Npc') -Filter 'roster_*.json' |
            Sort-Object Name | ForEach-Object { (Get-Content -Raw -LiteralPath $_.FullName | ConvertFrom-Json).ID })
        $existingIds = @('test_npc_a', 'test_npc_b', 'monster_wolf_0')
    }
    $ids = @(switch ($Scope) {
        Roster { $rosterIds }
        Existing { $existingIds }
        default { $rosterIds + $existingIds }
    })
    if ($Pair -or $Templates) {
        if ($Pair -and $Pair.Count -ne 2) { throw '-Pair 需要两位不同的模板。' }
        $ids = @(if ($Pair) { $Pair } else { $Templates })
    }
    # 名字只在唯一匹配时可用，结果和复跑参数统一记录真实定义 ID。
    function Resolve-TemplateId([string]$key) {
        if ($key -cin $catalog.ID) { return $key }
        $matchesByName = @($catalog | Where-Object DisplayName -CEQ $key)
        if ($matchesByName.Count -ne 1) {
            throw "模板名或 ID「$key」无法唯一匹配。请用 -ListTemplates 查看候选并指定 ID。"
        }
        return $matchesByName[0].ID
    }
    $ids = @($ids | ForEach-Object { Resolve-TemplateId $_ })
    if ($Pair) { $Pair = $ids }
    if (@($ids | Sort-Object -Unique).Count -ne $ids.Count) { throw '参赛模板重复。' }
    if ($ids.Count -lt 2) { throw '至少需要两份模板。' }
    function Resolve-TracePair([string[]]$keys) {
        $resolved = @($keys | ForEach-Object { Resolve-TemplateId $_ })
        if ($resolved.Count -ne 2 -or $resolved[0] -eq $resolved[1] -or
            $resolved[0] -notin $ids -or $resolved[1] -notin $ids) { throw '追踪或延长对局需要本批次两位不同的模板。' }
        return $resolved
    }
    if ($ExtendPair) { $ExtendPair = @(Resolve-TracePair $ExtendPair) }
    if ($TraceManaPair) { $TraceManaPair = @(Resolve-TracePair $TraceManaPair) }
    $OutputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
    [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($OutputPath))

    # 记录实际输入文件的路径、内容与程序集版本，供两批战报核对。
    $digest = [Security.Cryptography.IncrementalHash]::CreateHash('SHA256')
    try {
        [string[]]$paths = @(Get-ChildItem -LiteralPath (Join-Path $root 'Data') -Recurse -File |
            Where-Object Extension -In '.json','.lua' | ForEach-Object { [IO.Path]::GetRelativePath($root, $_.FullName).Replace('\','/') })
        # 与历史报告中 sorted(WindowsPath) 一致，按忽略大小写的路径段排序。
        [string[]]$sortKeys = @($paths | ForEach-Object { $_.ToLowerInvariant().Replace([char]'/', [char]0) })
        [Array]::Sort($sortKeys, $paths, [StringComparer]::Ordinal)
        foreach ($path in $paths) {
            $digest.AppendData([Text.Encoding]::UTF8.GetBytes($path))
            $digest.AppendData([byte[]]@(0))
            $digest.AppendData([IO.File]::ReadAllBytes((Join-Path $root $path)))
            $digest.AppendData([byte[]]@(0))
        }
        $dataHash = [Convert]::ToHexString($digest.GetHashAndReset()).ToLowerInvariant()
    }
    finally { $digest.Dispose() }

    $manager = [UnityWorld.Game.Domain.NpcMgr]::Instance
    $teamA = [CombatTeam]::TeamA
    $teamB = [CombatTeam]::TeamB
    $pairType = [System.ValueTuple[UnityWorld.Game.Domain.Npc,CombatTeam]]
    $results = [System.Collections.Generic.List[object]]::new()
    $cardMgr = [UnityWorld.Game.Domain.CardMgr]::Instance
    $cardCountBefore = @($cardMgr.GetAll()).Count
    $history = [UnityWorld.Core.LogMgr]::History
    $layouts = [ordered]@{}
    $orders = switch ($Order) {
        Forward { $false }
        Swapped { $true }
        Both { $false; $true }
    }

    $extendedRunFound = $false
    :rosterPairs for ($i = 0; $i -lt $ids.Count; $i++) {
        for ($j = $i + 1; $j -lt $ids.Count; $j++) {
            $left, $right = $ids[$i], $ids[$j]
            if ($Only -and $left -notmatch $Only -and $right -notmatch $Only) { continue }
            if ($Scope -eq 'Baseline' -and (($left -in $existingIds) -eq ($right -in $existingIds))) { continue }
            $traceMana = $left -in $TraceManaPair -and $right -in $TraceManaPair
            foreach ($seed in $battleSeeds) {
                foreach ($swapped in $orders) {
                    $extendBattle = $ExtendPair -and $left -in $ExtendPair -and $right -in $ExtendPair -and
                        ($swapped -eq ($ExtendOrder -eq 'Swapped')) -and ($null -eq $ExtendSeed -or $seed -eq $ExtendSeed)
                    $battleLimit = if ($extendBattle) { $ExtendedLimit } else { $Limit }
                    $recordTrace = $TraceResources -and (-not $ExtendPair -or $extendBattle)
                    $first, $second = if ($swapped) { $right, $left } else { $left, $right }
                    $scene = [UnityWorld.Game.Domain.Combat.CombatScene]::new()
                    $a = $b = $null
                    $errorsBefore = $history.Count
                    try {
                        $a = $manager.Assemble($defines.Get($first))
                        $b = $manager.Assemble($defines.Get($second))
                        foreach ($entry in @(@($first, $a), @($second, $b))) {
                            $id, $npc = $entry
                            $layout = [ordered]@{
                                SP = $npc.GetFieldSize(); SPMax = $npc.GetSpMax()
                                HP = $npc.GetHpMax(); MP = $npc.GetMpMax()
                            }
                            if ($HistoricalRoster -and $id -in $rosterIds -and ($layout.SP -notin (8..12) -or $layout.SPMax -notin (8..12))) {
                                throw "模板 $id 开场 SP 越界。"
                            }
                            $layouts[$id] = $layout
                        }

                        $participants = [Array]::CreateInstance($pairType, 2)
                        $participants.SetValue([Activator]::CreateInstance($pairType, [object[]]@($a, $teamA)), 0)
                        $participants.SetValue([Activator]::CreateInstance($pairType, [object[]]@($b, $teamB)), 1)
                        $scene.Init($seed, $participants, $battleLimit)
                        $scene.EnablePresentationRecording($true)
                        $scene.PreStart()
                        if ($HistoricalRoster) {
                            foreach ($npc in $scene.GetCombatants().Values) {
                                foreach ($card in $npc.GetField()) {
                                    if ($card.DefineId -like 'roster_*' -and $card.LuaHooks.Count -eq 0) {
                                        throw "卡 $($card.DefineId) 未加载实际 Lua 回调。"
                                    }
                                }
                            }
                        }
                        $scene.Start()
                        $resourceTrace = [System.Collections.Generic.List[object]]::new()
                        while ($true) {
                            if ($recordTrace -and ($scene.IsFinished -or $scene.CurrentTick % 250 -eq 0)) {
                                $states = [ordered]@{}
                                foreach ($actor in $scene.GetCombatants().Values) {
                                    $id = if ($actor.Team -eq $teamA) { $first } else { $second }
                                    $states[$id] = [ordered]@{
                                        hp = $actor.GetHp(); mp = $actor.GetMp(); shield = $actor.ShieldValue
                                        sp = $actor.GetSp(); round = $actor.Backpack.Round
                                    }
                                    if ($traceMana) {
                                        $board = $actor.Backpack
                                        $byColor = @{}
                                        foreach ($element in $board.Mana.Values) { $byColor[$element.ToString()]++ }
                                        $cards = @($actor.GetField() | ForEach-Object {
                                            $card = $_
                                            $cost = @{}
                                            foreach ($entry in $card.GetCombatManaCost().GetEnumerator()) {
                                                $cost[$entry.Key.ToString()] = $entry.Value
                                            }
                                            [ordered]@{
                                                define = $card.DefineId; cost = $cost
                                                assigned = @($card.AssignedElements | ForEach-Object { $_.ToString() })
                                                canAssign = @($board.Mana.Values | Where-Object { $card.CanAcceptMana($_) } |
                                                    ForEach-Object { $_.ToString() } | Sort-Object -Unique)
                                            }
                                        })
                                        $states[$id]['board'] = [ordered]@{
                                            empty = $board.EmptyCells().Count; mana = $byColor; cards = $cards
                                        }
                                    }
                                }
                                $resourceTrace.Add([ordered]@{ tick = $scene.CurrentTick; states = $states })
                            }
                            if ($scene.IsFinished) { break }
                            $scene.Tick()
                        }

                        $result = $scene.GetResult()
                        if ($history.Count -gt $errorsBefore) {
                            $messages = $history.GetRecent($history.Count - $errorsBefore).Message -join [Environment]::NewLine
                            throw "战斗记录到错误 $messages"
                        }
                        $winner = $result.GetObject('WinnerTeam')
                        $reason = $result.GetObject('EndReason')
                        $map = @{}
                        foreach ($actor in $scene.GetCombatants().Values) {
                            $name = if ($actor.Team -eq $teamA) { $first } else { $second }
                            $map[$actor.Id] = $name
                        }
                        $metrics = @{}
                        foreach ($actor in $scene.GetCombatants().Values) {
                            $metrics[$map[$actor.Id]] = [ordered]@{
                                hp = $actor.GetHp(); mp = $actor.GetMp(); shield = $actor.ShieldValue
                                rounds = $actor.Backpack.Round; status = $actor.Status.ToString()
                                sp = $actor.GetSp(); wounds = @{}; remainingWounds = @{}
                                uses = @{}; effects = @{}; hpDamage = 0
                                pending = @{}; pendingValues = @{}; maxShieldAbsorption = 0; unsourcedDamageEvents = 0
                            }
                            foreach ($fieldCard in $actor.GetField()) {
                                if ($fieldCard.HasKeyword('Wound')) { $metrics[$map[$actor.Id]].remainingWounds[$fieldCard.DefineId]++ }
                            }
                        }
                        $artifactPhases = @{}
                        foreach ($event in $scene.ReadPresentationEvents()) {
                            if ($TraceArtifactPhases -and $event.Kind.ToString() -eq 'CardApplied' -and
                                $event.Applied.IsPassive -and 'FaBao' -in $event.Card.Keywords) {
                                $artifactPhases["$($event.Card.DefineId)/$($event.Applied.Origin)/$($event.Card.Phase)"]++
                            }
                            $metric = $metrics[$map[$event.NpcId]]
                            switch ($event.Kind.ToString()) {
                                'CardUsed' { $metric.uses[$event.Card.DefineId]++ }
                                'CardAddedToBackpack' {
                                    if ('Wound' -in $event.Card.Keywords) { $metric.wounds[$event.Card.DefineId]++ }
                                }
                                'CardApplied' {
                                    if ($event.Applied.IsPassive) {
                                        $metric.effects["$($event.Card.DefineId)/$($event.Applied.Origin)"]++
                                    }
                                }
                                'PendingChanged' {
                                    if ($event.Pending.Entered) {
                                        $metric.pending[$event.Card.DefineId]++
                                        $metric.pendingValues["$($event.Card.DefineId)/$($event.Pending.Value)"]++
                                    }
                                }
                                'DamageResolved' {
                                    $metric.hpDamage += $event.Damage.HpDamage
                                    if ($null -eq $event.Damage.SourceNpcId) { $metric.unsourcedDamageEvents++ }
                                    $metric.maxShieldAbsorption = [Math]::Max($metric.maxShieldAbsorption, $event.Damage.ShieldAbsorbed)
                                }
                            }
                        }
                        $battle = [ordered]@{
                            left = $left; right = $right; seed = $seed; swapped = $swapped
                            firstScanned = if ($a.Id -lt $b.Id) { $first } else { $second }
                            winner = switch ($winner) { TeamA { $first } TeamB { $second } default { $null } }
                            reason = "$reason"; ticks = $scene.CurrentTick
                            leftState = $metrics[$left]; rightState = $metrics[$right]
                        }
                        if ($recordTrace) { $battle['resourceTrace'] = $resourceTrace }
                        if ($artifactPhases.Count -gt 0) { $battle['artifactPhases'] = $artifactPhases }
                        if ($extendBattle) { $battle['limit'] = $battleLimit }
                        $results.Add($battle)
                    }
                    catch {
                        $results.Add([ordered]@{
                            left = $left; right = $right; seed = $seed; swapped = $swapped
                            reason = 'Error'; error = $_.Exception.ToString()
                        })
                    }
                    finally {
                        $scene.Cleanup()
                        if ($a) { $manager.Remove($a.Id) }
                        if ($b) { $manager.Remove($b.Id) }
                    }
                    if ($extendBattle) {
                        $extendedRunFound = $true
                        break rosterPairs
                    }
                }
            }
        }
    }

    if ($ExtendPair -and -not $extendedRunFound) { throw '当前配对或装配顺序没有命中延长复跑目标。' }
    [ordered]@{
        seeds = $battleSeeds; worldSeed = $WorldSeed; scope = $Scope; pair = $Pair; order = $Order; limit = $Limit
        templateIds = $ids; only = $Only; historicalRoster = [bool]$HistoricalRoster
        extendPair = $ExtendPair; extendOrder = $ExtendOrder; extendSeed = $ExtendSeed; extendedLimit = $ExtendedLimit
        traceResources = [bool]$TraceResources; traceManaPair = $TraceManaPair; traceArtifactPhases = [bool]$TraceArtifactPhases
        dataSha256 = $dataHash; engineSha256 = (Get-FileHash -LiteralPath $engine).Hash.ToLowerInvariant()
        worldCardCountBefore = $cardCountBefore; worldCardCountAfter = @($cardMgr.GetAll()).Count
        layouts = $layouts; battles = $results
    } |
        ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $OutputPath -Encoding utf8
    Write-Host "战报 $OutputPath"
    $results | Group-Object { $_.reason } | Select-Object Name, Count | Format-Table
    $results | Where-Object { $_.reason -eq 'Error' } | Select-Object -First 1 | ConvertTo-Json -Depth 4
    if ($results.Count -eq 0 -or ($results | Where-Object { $_.reason -eq 'Error' } | Select-Object -First 1)) { exit 1 }
}
finally {
    [UnityWorld.Game.World.WorldMgr]::End()
}
exit 0
