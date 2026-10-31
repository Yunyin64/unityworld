# Battle observer verification

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
