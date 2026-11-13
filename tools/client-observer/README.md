# Battle observer verification

## 验证两种战斗窗口

客户端调用 `BattleLauncher.OpenBattle(battleCase, BattleWindowMode.CurrentWindow)` 或 `NewWindow`。两种方式使用同一份冻结输入并从 Tick 0 暂停。模板编辑器固定传 `NewWindow`。同窗调用保留并暂停原页面，点击「返回」还原焦点和处理状态。后台观察进程负责真实模拟，重开只替换该进程。独立观察窗口内的重开沿用原先的新进程重启。

在仓库根用 PowerShell 7 运行验收。将 `$engine` 指向本机的 Godot 4.7 .NET 编辑器可执行文件。

```powershell
$engine = (Get-Command godot).Source
./tools/client-observer/verify-windows.ps1 -Godot $engine
```

验收宿主使用世界种子 4521、战斗种子 7324 和 16 步上限，依次验证当前窗口自动播放和单步、两次重开、打断管道、模板装配失败、数据版本变化和返回来源。它也通过同一公开入口打开和关闭一个真实独立窗口。在 Windows 上检查父窗口及新开的观察窗口有窗口句柄，后台模拟没有可见主窗口。脚本随后启动原独立观察场景，操作实际播放、暂停和单步按钮，比较两种方式从 Tick 0 到 16 的快照、表现事件、输入及完整战报，验证独立窗口重开一次；最后仅杀掉专用验收父进程并确认其 worker 自行退出。

成功时终端显示 `BATTLE_WINDOW_OK` 和 `WINDOW_REPLAY_MATCH`。本次命令、模式与比较结果可从输出目录中的 `case.json`、`result.txt`、两个同窗日志副本与 `new-window/*.session.txt` 追溯，默认位于 `.local-verification/battle-windows/<本次运行>/`。观察会话目录里的 `window-mode.txt` 单独记录窗口方式。故障用例预期出现的错误也保存在该次日志。开发场景 `res://scenes/tests/battle_window_check.tscn` 仅用于复现同窗调用，传 `--battle-window-check=<输出目录>` 启动检查。

This standalone .NET 8 program compiles the same value contracts, case adapter, and session as the Godot client. It references the shared core, including the presentation-only observation and readonly stat extensions. Original console test entry points and gameplay data remain unchanged. Its own `bin/` and `obj/` stay under this tool directory; Godot compiles only `scripts/client/**/*.cs`.

Run from the outer repository root in PowerShell:

`& tools/client-observer/verify.ps1` performs the full fresh-process matrix, expected-error checks, record uniqueness checks, and first-field-difference check, then writes `verification-summary.json` under `.local-verification/battle-observer/`. The individual commands are:

```powershell
dotnet build tools/client-observer/ClientObserver.csproj
dotnet tools/client-observer/bin/Debug/net8.0/ClientObserver.dll --root=C:\Unity\Game\unityworld --output=.local-verification/battle-observer/baseline-a.jsonl --no-event-observer
dotnet tools/client-observer/bin/Debug/net8.0/ClientObserver.dll --root=C:\Unity\Game\unityworld --output=.local-verification/battle-observer/baseline-b.jsonl --no-event-observer
dotnet tools/client-observer/bin/Debug/net8.0/ClientObserver.dll --root=C:\Unity\Game\unityworld --output=.local-verification/battle-observer/observed.jsonl --observe-repeat=25
dotnet tools/client-observer/bin/Debug/net8.0/ClientObserver.dll --root=C:\Unity\Game\unityworld --output=.local-verification/battle-observer/automatic.jsonl --mode=play --observe-repeat=3
dotnet tools/client-observer/bin/Debug/net8.0/ClientObserver.dll compare .local-verification/battle-observer/baseline-a.jsonl .local-verification/battle-observer/baseline-b.jsonl
dotnet tools/client-observer/bin/Debug/net8.0/ClientObserver.dll compare .local-verification/battle-observer/baseline-a.jsonl .local-verification/battle-observer/observed.jsonl
dotnet tools/client-observer/bin/Debug/net8.0/ClientObserver.dll compare .local-verification/battle-observer/baseline-a.jsonl .local-verification/battle-observer/automatic.jsonl
```

Each command starts a fresh process. The baseline has event observation disabled, but takes one audited value projection after each Tick so state can be compared. High observation additionally projects the core 25 times per Tick; ordinary Godot rendering never does this. Automatic playback uses varying elapsed frame durations, including frames that commit multiple Ticks. Both modes check that paused wall time is discarded, background world time stays zero, and a completed session rejects further stepping. `maxRowsPerTick` reports observed scan movement between committed snapshots; it does not change or reconstruct simulation decisions.

Outputs:

- `*.jsonl`: Tick 0 and each complete logical Tick, with `snapshot` and the full original `trace`.
- `*.case.json`: complete input for a new-process replay via `--case-input=<path>`.
- `*.identity.json`: complete input, actual per-file JSON/Lua hashes, and verification settings.
- `*.records.jsonl`: received existing event fields and original trace increments in receipt order. Missing damage/pairing details are never invented.

`compare` requires both `.case.json` input sidecars, checks complete case input, then every frame field and trace string. Missing input evidence or a mismatch returns exit code 2 and prints the first JSON path; success returns 0. It does not normalize away instance IDs or random ordering. Runtime or initialization errors return 1.

Additional switches:

- `--data-root=<directory containing Data>` verifies a different actual data copy.
- `--stop-tick=N` records only through that Tick without claiming normal combat completion.
- `--case-id=<id>` selects only a listed playable case.
- `--expected-fingerprint=deliberate-mismatch --expect-initialization-error` validates changed-data rejection before world initialization.
- `--expect-sink-error` deliberately throws from a client frame sink after one Tick and checks the original client error is retained, no false result is produced, and advancement stops.

The known core APIs cannot reset static IDs fully. Do not reinitialize a second battle in the same process or reflectively reset core fields. The session calls `WorldMgr.Initialize → Start → Assemble participants → Scene.Init → PreStart → Start`, subscribes before battle startup, pauses at Tick 0, and never calls `WorldMgr.Tick`.

For Godot capture, subscribe to `FrameCommitted` before `Initialize`, write `BattleJson.SerializeFrame(frame)` for every callback, and persist `session.Case.ToJson()` to the matching `.case.json`. This avoids missing intermediate Ticks when a display frame advances several logical steps. Callback failure pauses the observer after the completed core Tick and remains a client diagnostic.

UI upper limits remain optional initial-cache references. Even `Stats.Snapshot().Get()` is forbidden in projection because NCalc expressions are shared. The projection only copies already-present boxed float cache values and never runs stat, Lua, or random calculations.

## Presentation verification

See [PRESENTATION.md](PRESENTATION.md) for scope, baseline, current evidence and migration comparisons. Run each check in its own fresh process:

- `--check-presentation-core`: actual core event mutation points, ownership and delayed causes.
- `--verify-stat-projection`: readonly values and unchanged caches/formula parameters.
- `--check-presentation-player`: ordering, snapshots, deduplication, cancellation and bounded backlog.
- `--no-presentation`: disable the new observation buffer for equivalence runs.
- `compare-legacy before.jsonl after.jsonl`: migration-only comparison; ignores only the new current maxima/diagnostic fields and changed core assembly MVID. Regular comparisons must keep using strict `compare`.

Each normal run also writes `.presentation.jsonl`, one batch per logical Tick. Godot visual fixture output and real battle captures are kept separately.
