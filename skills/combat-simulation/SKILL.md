---
name: combat-simulation
description: 在 UnityWorld 模拟 NPC 对战。木桩和已有 NPC 都可作为基准对手，用于自定义 Tick 采样、统计单卡发动和生成批次汇报。普通对战适用于查胜负、换边轮赛和复现单局。用户说“战斗模拟”“跑几场”“换边试打”“看战报”时使用。
---

# 战斗模拟

基准模式和普通对战使用同一套 NPC 装配和战斗结算。按要看的数据选择入口。

| 用户要看什么 | 用哪个入口 | 读哪里 |
| --- | --- | --- |
| 指定 Tick 的资源、单卡发动次数的批次统计，或比较打木桩和打 NPC 的表现 | 基准模式。用 `-BenchmarkSubject` 指定待测方，`-BenchmarkOpponent` 可指定已加载的 NPC 模板，省略时对手是木桩 | `summary.md` |
| 单局胜负、双方整局用卡明细、换边或多模板轮赛 | 普通对战。用 `-Pair`、`-Templates` 或 `-Scope` | 普通战报 JSON 的 `battles` |

普通试打直接读取工作区配置。调卡、改代码和更新正式轮赛文档由当前任务另行决定。

## 运行

以下命令从仓库根执行，需要 Windows x64、.NET 8 SDK（或支持 net8.0 的更高版本）和 PowerShell 7.4+ x64。首次运行或后端源码有变化时先构建 Debug 程序集。每个批次用新的 `pwsh` 进程，批内保持数据文件不变。

```powershell
dotnet build UnityWorld/Console/UnityWorld.Console.csproj
$run = 'skills/combat-simulation/scripts/run.ps1'
pwsh -NoProfile -File $run -ListTemplates
```

### 基准模式（木桩或 NPC 对 NPC）

基准模式用 Python 3.10+ 标准库调度真实后端。每个种子独立初始化世界，固定待测方为 A，对手为 B。默认木桩开场为 20 HP、30 MP、SP 1/10，基础补灵为零。`-BenchmarkOpponent` 接受已有 NPC 的模板 ID 或唯一显示名，适用相同的测量清单和汇报格式。

```powershell
# 陆白打木桩
pwsh -NoProfile -File $run -BenchmarkSubject tca_trial_lu_bai
# 陆白打宁朔，同样输出基准汇报
pwsh -NoProfile -File $run -BenchmarkSubject tca_trial_lu_bai -BenchmarkOpponent tca_trial_ning_shuo
```

省略 `-Seeds` 默认跑 1–100。基准战斗推进到正常结算，默认最多 1000 Tick。默认在 50、100、200、300、400、500、750、1000 Tick 和终局读取待测方的剩余 HP、剩余 MP 和已完成的背包扫描轮数。同前缀的条目排在一起，定点按 Tick 递增。另记实际结束时点 `FinsihTick`。

默认表也包含第 50 至 500 Tick 每隔 50 Tick 的累计伤害、终局伤害、扣血和伤口事件、用卡、资源收支及对手终局状态。`mp_finish` 和 `rounds_finish` 是已有终局条目，分别与 `Mp_Last`、`rounds_Last` 读取同一数值。

自定义采样时，复制 `UnityWorld/tools/combat_benchmark/dummy-baseline.json`，追加条目后用 `-MeasurementPlan` 传入副本。下面这条读待测方第 150 Tick 的实际 MP 消耗。`side` 填 `subject` 或 `opponent`。定点的 `at` 填 Tick 数，终局填 `finish`，首次事件省略 `at`。

```json
{ "id": "mpSpent_150", "side": "subject", "metric": "mpSpent", "at": 150 }
```

要按卡统计发动次数，在 `cardUses` 测项加 `cardId`。下面这条统计待测方整场打出「打」的次数。报告描述取本批卡牌定义的 `DisplayName`。同一角色的同卡牌 ID 实例合计，一次提交发动计一次。整场没发动记 0。

```json
{ "id": "cardUses_card_form_da_Last", "side": "subject", "metric": "cardUses", "cardId": "card_form_da", "at": "finish" }
```

**用基准模式跑完后，先检查退出码，再打开命令打印的 `summary.md`。** 从这份汇报读取有效场数、结束原因，以及各条目的平均数、中位数和 null 个数，向用户汇报时链接这一个 Markdown。基准批次只生成一份汇报 Markdown。默认位置为 `UnityWorld/Reports/CombatBenchmarks`，可通过 `-BenchmarkOutputDirectory` 改存其他目录。

失败场单列。成功局未到采样点或事件未发生时记 null，手动缩短 Tick 上限也适用。零值参与计算。偶数个读数取中间两项的平均值作为中位数。

需要查某场或更细的分布时，从 `summary.json` 的 `samples[].rows` 和 `metrics` 读取。`manifest.json` 保存输入、版本指纹、指标字典和错误诊断。用 `python UnityWorld/tools/combat_benchmark/benchmark.py --summarize <批次目录>/manifest.json` 可以离线重建汇总。

### 普通对战

```powershell
pwsh -NoProfile -File $run -Pair '陆白,宁朔' -WorldSeed 12345 -Seeds '1207,3169,5519'
```

`-Pair` 接受模板 ID 或唯一显示名。上例对每个种子正反装配各跑一次，共 6 局。只跑一场时用一个种子并加 `-Order Forward`。命令打印 JSON 的绝对路径，可用 `-OutputPath` 指定存放位置。

| 用户想测什么 | 传给脚本的参数 |
| --- | --- |
| 指定几人互打 | `-Templates '陆白,宁朔,裴轻'`，名单内两两配对 |
| 一人对当前所有模板 | `-Scope All -Only '^tca_trial_lu_bai$'`，`Only` 匹配配对任一方的模板 ID |
| 全表或某组模板 | `-Scope All` 为当前已加载的全部模板。`Roster` 为 ID 以 `roster_` 开头的组，`Existing` 为其余组，`Baseline` 为两组交叉对战 |
| 查法宝是否在激活期响应 | 加 `-TraceArtifactPhases`，查看每局的 `artifactPhases`，按法宝、回调和发生阶段计数 |
| 查耗灵或堵盘 | 加 `-TraceManaPair '陆白,宁朔'`，记录该配对在资源采样点的空格、各色灵元、卡牌费用与蓄灵 |

- 先看进程退出码，再读 `battles`。逐局异常记在 `reason = Error` 的 `error` 中。启动或参数失败时看终端报错。核对实际对手、不同种子数和换边覆盖。两次装配顺序共享一个战斗种子。
- 按 `reason` 和 `winner` 一起统计。`AllDefeated` 且有胜者计胜负，双方均败退时单列。`TurnLimitReached` 单列触顶，末 Tick 即使带有胜者也保留该事实。汇报有效胜负的分母，附种子、Tick 上限和战报路径。
- `leftState.uses` 和 `rightState.uses` 读取整场 `CardUsed` 事件，按卡牌定义 ID 累计发动次数。法宝激活和后续效果分别查 `artifactPhases` 与 `effects`。伤口和待发拼点读 `wounds` 与 `pendingValues`。
- `-TraceResources` 在开场、每隔 250 Tick 和终局读取双方 HP、MP、盾、SP 和扫描轮号，保存到 `resourceTrace`。需要自定义采样时点时，用基准模式的 `-MeasurementPlan`。

## 精确复现

战报含数据与后端程序集的 SHA-256、参与名单与顺序、筛选条件、世界种子、战斗种子、装配顺序和 Tick 上限。换边时同时对调装配顺序和阵营，`firstScanned` 记录双方同 Tick 到行时的优先者。

普通轮赛在批内共享世界实例分配。复现其中一局，要保留前面所有对局的输入，只延长目标局。复跑时核对前面各局结果，以及目标局在原上限时的资源状态。基准模式中每局独立初始化，复跑某局可用同一份数据和测量清单并传入该局种子。

```powershell
pwsh -NoProfile -File $run -Templates '陆白,宁朔,裴轻' -Seeds '1207,3169' -WorldSeed 42 -Limit 250 -TraceResources
pwsh -NoProfile -File $run -Templates '陆白,宁朔,裴轻' -Seeds '1207,3169' -WorldSeed 42 -Limit 250 -ExtendPair '陆白,宁朔' -ExtendOrder Swapped -ExtendSeed 3169 -ExtendedLimit 2500 -TraceResources
```

做改卡前后对照时，每个版本都用独立进程跑同一对手、一个种子和一个装配顺序，再换种子复核。复跑旧 24 人验收表时，使用当时的种子、筛选及历史名单顺序，并开启 `-HistoricalRoster`。旧轮赛的 SP 检查与复跑说明见[后端轮赛复跑](../../UnityWorld/Docs/战斗/TCA工作区/人物模板/后端轮赛复跑.md)。
