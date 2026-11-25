---
name: combat-simulation
description: 在 UnityWorld 用真实战斗后端模拟 NPC 模板对战。适用于指定角色单挑、换边轮赛、配卡前后对照，以及复现 MP 耗尽、灵元堵盘或法宝触发问题。用户说“战斗模拟”“跑几场”“换边试打”“看战报”时使用。
---

# 战斗模拟

用项目的 NPC 装配流程和战斗场景实际跑完对局，交付胜负、结束原因和原始 JSON。普通试打直接读取工作区配置。调卡、改代码和更新正式轮赛文档由当前任务另行决定。

## 运行

以下命令从仓库根执行，需要 Windows x64、.NET 8 SDK（或支持 net8.0 的更高版本）和 PowerShell 7.4+ x64。首次运行或后端源码有变化时先构建 Debug 程序集。每个批次用新的 `pwsh` 进程，批内保持数据文件不变。

```powershell
dotnet build UnityWorld/Console/UnityWorld.Console.csproj
$run = 'skills/combat-simulation/scripts/run.ps1'
pwsh -NoProfile -File $run -ListTemplates
pwsh -NoProfile -File $run -Pair '曲泽,段萤' -WorldSeed 12345 -Seeds '1207,3169,5519'
```

`-Pair` 接受模板 ID 或唯一的显示名。上例对每个种子正反装配各跑一次，共 6 局。只要一场时用一个种子并加 `-Order Forward`。运行结束会打印 JSON 的绝对路径，默认每次生成独立文件。需要长期留档时传 `-OutputPath` 指定本批文件名。

| 用户想测什么 | 传给脚本的参数 |
| --- | --- |
| 指定几人互打 | `-Templates '曲泽,夏祁,段萤'`，名单内两两配对 |
| 一人对当前所有模板 | `-Scope All -Only '^roster_qu_ze$'`，`Only` 是匹配配对任一方模板 ID 的正则 |
| 全表或某组模板 | `-Scope All` 为当前已加载的全部模板。`Roster` 为 ID 以 `roster_` 开头的组，`Existing` 为其余组，`Baseline` 为两组交叉对战 |
| 查法宝是否在激活期响应 | 加 `-TraceArtifactPhases`，查看每局的 `artifactPhases`，按法宝、回调和发生阶段计数 |
| 查耗灵或堵盘 | 加 `-TraceManaPair '曲泽,段萤'`，记录该配对的空格、各色灵元、卡牌费用与蓄灵。只看各局 HP、MP、盾、SP 和扫描轮号时用 `-TraceResources` |

## 读战报

### 打木桩

在仓库根构建同一份 Debug 程序集后，基准模式用 Python 3.10+ 标准库调度真实后端。每个种子单独装配双方，固定待测者为 A，木桩为 B；木桩开场 20 HP、30 MP、SP 1/10，自身基础补灵为零。

```powershell
pwsh -NoProfile -File skills/combat-simulation/scripts/run.ps1 -BenchmarkSubject roster_qu_ze -Seeds 37
```

省略 `-Seeds` 默认跑 1–100。可用 `-BenchmarkOpponent` 指定对照组。基准战斗推进到正常结算，默认最多 1000 Tick。默认在 50、100、200、300、400、500、750、1000 Tick 和终局读取待测方的剩余 HP、剩余 MP 和已完成的背包扫描轮数。同前缀的条目排在一起，定点按 Tick 递增。另记实际结束时点 `FinsihTick`。

默认表也包含第 50 至 500 Tick 每隔 50 Tick 的累计伤害、终局伤害、扣血和伤口事件、用卡、资源收支及对手终局状态。`mp_finish` 和 `rounds_finish` 是已有终局条目，分别与 `Mp_Last`、`rounds_Last` 读取同一数值。

复制 `UnityWorld/tools/combat_benchmark/dummy-baseline.json`，用 `-MeasurementPlan` 传入副本。例如追加下面这个条目，便在原表中多一行第 150 Tick 的实际 MP 消耗。定点填整数，终局填字符串 `finish`，首次事件省略 `at`。

```json
{ "id": "mpSpent_150", "side": "subject", "metric": "mpSpent", "at": 150 }
```

从打印的批次目录打开 `seed-37.md`。每个启用的条目一行，真实零值写 0，未到达的节点或未发生的首次事件写 null。`FinsihTick` 和 `Finishreson` 沿用约定拼写。采样不进入普通日志。默认位置为 `UnityWorld/Reports/CombatBenchmarks`，可通过 `-BenchmarkOutputDirectory` 改存其他目录。

打开批次目录的 `summary.md`，表格包含「条目ID」「描述」「平均数」「中位数」「null个数」。失败场单列。成功局未到采样点或事件未发生时记 null，手动缩短 Tick 上限也适用。零值参与计算。偶数个读数取中间两项的平均值作为中位数。

`summary.json` 引用逐场文件，按条目给出覆盖局数和分布。首次事件同时给出发生局数与成功局数。`manifest.json` 保存输入、版本指纹、指标字典和错误诊断。可在进程关闭后用 `python UnityWorld/tools/combat_benchmark/benchmark.py --summarize <批次目录>/manifest.json` 重建两份汇总。

### 普通对战

- 先看进程退出码，再读 `battles`。逐局异常记在 `reason = Error` 的 `error` 中。启动或参数失败时看终端报错。核对实际对手、不同种子数和换边覆盖。两次装配顺序共享一个战斗种子。
- 按 `reason` 和 `winner` 一起统计。`AllDefeated` 且有胜者计胜负，双方均败退时单列。`TurnLimitReached` 单列触顶，末 Tick 即使带有胜者也保留该事实。汇报有效胜负的分母，附种子、Tick 上限和战报路径。
- 查看 `leftState` / `rightState` 中的 `uses`、`effects`、`wounds` 和 `pendingValues` 判断实际用牌情况。法宝的使用、激活光环和后续事件实效分别核对。长局用 `resourceTrace` 对齐两边 MP、盘上余灵和后续行动。

## 精确复现

战报含数据与后端程序集的 SHA-256、参与名单与顺序、筛选条件、世界种子、战斗种子、装配顺序和 Tick 上限。换边时同时对调装配顺序和阵营，`firstScanned` 记录双方同 Tick 到行时的优先者。

批内共享世界实例分配。复现轮赛里的一局，要保留前面所有对局的输入，只延长目标局。下面两条命令是同一批次的原跑和复跑。复跑保留前缀，目标局结束即停。核对前面各局结果，以及目标局在原上限时的资源状态。

```powershell
pwsh -NoProfile -File $run -Templates '曲泽,段萤,夏祁' -Seeds '1207,3169' -WorldSeed 42 -Limit 250 -TraceResources
pwsh -NoProfile -File $run -Templates '曲泽,段萤,夏祁' -Seeds '1207,3169' -WorldSeed 42 -Limit 250 -ExtendPair '曲泽,段萤' -ExtendOrder Swapped -ExtendSeed 3169 -ExtendedLimit 2500 -TraceResources
```

做改卡前后对照时，每个版本都用独立进程跑同一对手、一个种子和一个装配顺序，再换种子复核。复跑旧 24 人验收表时，使用当时的种子、筛选及历史名单顺序，并开启 `-HistoricalRoster`。旧轮赛的 SP 检查与复跑说明见[后端轮赛复跑](../../UnityWorld/Docs/战斗/TCA工作区/人物模板/后端轮赛复跑.md)。
