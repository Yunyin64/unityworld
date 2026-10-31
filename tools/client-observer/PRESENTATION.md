# 战斗表现接入与验证

## 实施基线和边界

2026-10-31 开始 `enhance-battle-presentation` 时工作区无未提交改动。基线位于仓库外层 `.local-verification/enhance-battle-presentation/`：`initial-head.txt`、`initial-status.txt`、`protected-hashes.json`、`before.jsonl` 及输入/数据身份附属文件。原观察器与原控制台默认案例均实际得到 TeamB、AllDefeated、740 Tick，观察器保存了 741 帧。

本轮允许核心 Combat 事实记录、相关 C# 效果来源观察及 Stat 只读求值改动。JSON/内嵌 Lua、原控制台入口、工程依赖、原预览及美术资源受保护；不改扫描/资源/伤害公式。核心与 Godot 仍在同一进程，用值批次传递，无网络。

`console-before-run.log` 是原已构建控制台实际运行的基线；`console-before.log` 是并行开发中源文件尚未齐全时的一次未成功构建，不作为基线成功证据。

## 数据边界

`BattleObserverSession` 负责宿主和每次完整 Tick 提交；`BattleSnapshotProjector` 复制状态及安全上限；`BattleRecordCollector` 保留原始战报和玩法事件副本。新 `BattlePresentationFrame` 同时提供终态和有序事实，单次显示更新推进多个 Tick 时每个批次均交付。

`NpcSnapshot` 的 HpMaxReference/MpMaxReference/SpMaxReference 保留用于旧数据对照，当前界面使用 HpMax/MpMax/SpMax，失败用 StatReadError 明示。上限查询实现及测试见 STAT_PROJECTION.md；事件来源覆盖见 CORE_PRESENTATION.md。

## 已验证的适配层结果

- `--verify-stat-projection`：26 场景、11560 断言、2048 次重复读取通过。见 `stats.log`。
- `after.jsonl`（事件启用，每 Tick 重读 25 次）对 `before.jsonl` 的 `compare-legacy`：741 帧旧玩法字段及 Trace 一致。
- `disabled.jsonl`（两种观察均关闭）对 `after.jsonl` 的严格 `compare`：741 帧全部字段一致。
- `automatic.jsonl`（一显示帧可推进多 Tick）对 `after.jsonl`：741 帧全部字段一致；每个逻辑 Tick 都有完整表现批次。
- 各步当前上限均存在且无 StatReadError。实际零值与缺定义诊断分别由只读测试和投影入口处理。
- 原有 `--expect-sink-error` 仍准确停在已提交 Tick 1；`--expect-initialization-error` 保留原始版本错误且禁止推进。

`compare-legacy` 仅忽略已知新增 HpMax/MpMax/SpMax/StatReadError 展示字段和本轮必然改变的核心程序集 MVID，其余输入、旧字段及 Trace 严格比较。一般新版本间对照必须继续使用 `compare`，不能使用迁移模式掩盖字段差异。

## 最终验收（2026-10-31）

完整机器摘要：`.local-verification/enhance-battle-presentation/verification-summary.json`。

| 验证 | 实际结果与证据 |
| --- | --- |
| 原有核心回归 | 487 断言通过，另有 CORE_SMOKE_OK；同目录 backpack/effect/activation/template 日志 |
| 纯属性读取 | 26 场景、11560 断言、2048 次重复读取通过，`verify-stat-projection-final.log` |
| 新核心事实 | 67 断言通过，含真实新被动卡初始化生效先于入包的事实，`check-presentation-core-final.log` |
| 播放器 | 21 场景、100 断言通过；回调内取消、旧会话、积压、缺帧及新卡入包前置，`check-presentation-player-final.log` |
| 修改前后规则 | `before.jsonl` 对 `final.jsonl` 迁移投影 741 帧完全一致（含原始 Trace） |
| 最终观察开关 | `final-disabled.jsonl` 对 `final.jsonl` 全字段 741 帧完全一致 |
| 最终 Godot 自动播放 | `godot-final-auto/godot-auto-0.jsonl`，1600×900，741 帧与同版 CLI 严格一致 |
| 最终 Godot 单步及新进程重开 | `godot-final-step/godot-step-0.jsonl`、`godot-step-1.jsonl`，每份 741 帧与同版 CLI 严格一致 |
| 操作及末步 | controls 文本证实真实 viewport 命中播放/暂停/单步/战报；正常末步等待动画收尾才退出，新进程完成同输入战斗 |
| 两尺寸视觉夹具 | `fixture-final-1440/`、`fixture-final-1600/`，各 217 张帧图；half-blue/对拼及平局位移恢复的元数据审计通过，结束 idle=true/diagnostic=null |
| 保护范围 | 230 个数据、资源及原入口文件 SHA256 均未变；未生成美术图片，旧预览未改；diff 格式检查通过 |

正式案例仍为 TeamB、AllDefeated、740 Tick；741 批共 282 条事件（90 次分配、61 次使用、44 次待发变化、8 次对拼、14 次伤害、2 次入包、63 次生效）。真实战斗连续帧在 godot-final-auto，专用构造视觉案例明确标注非正式战斗，不能替代核心真实性验证。验收采用 Godot viewport 注入按钮输入、实际渲染连续帧、机器元数据对照和助手视觉检查，不冒称用户人工验收。

## 播放及扩展约定

`BattlePresentationPlayer` 用对象/Action/Contest/Effect 依赖排程，无关效果并行；显示时间与逻辑 Tick 分离。暂停和正常结束保留已提交短动画，退出/重开递增 generation 并取消旧表现。新被动卡 Start 的事实可先于 CardAdded；播放计划保留原始事实文件顺序，同时将该新卡入包设为它自身提示的前置，不移动生成者卡的先行效果。

默认时长集中在播放器：灵元 .18s、使用 .10s、对撞 .25s、入包 .40s、名称 .70s、伤害 .80s。积压 .8s 后压缩显示时间；2048 事件或帧上限触发明确跳过提示并恢复完整快照。原始事实文件不受动画跳过影响。

构造视觉夹具只在显式 `--observer-presentation-fixture` 开关下运行。实际客户端入口仍为原 battle_observer 场景，默认暂停在 Tick 0。原有 NCalc/编译及数据加载提示保留；本轮不借表现任务升级依赖或修正历史数据。

## 用户实测后的并行与卡面修正

用户发现扫描光线在播放表现时停顿，以及长剑图被内部格缝切断。首轮虽然同 Tick 内存在局部并行，整批播放屏障仍使新 Tick 状态迟到；仅比较玩法轨迹和单次动画截图没有覆盖此体验问题。

现改为跨 Tick 的同一显示时间线，新批次立即更新最新状态并安排其事件。只有同卡资源过渡、新卡入包前置及待发槽短位移持有必要依赖；名称和伤害淡出不占有整张卡、人物或整个对拼链。多条头顶文字同时显示，存活期间保持固定槽位。各批完成通知可以晚于新批，但完成回调不得写回旧快照。

扫描线及行进度独立读取 session 最新快照。卡图/底色按同卡四邻接并集连续绘制，取消内部格缝与穿图边框，仅保留真实外轮廓和非矩形空洞，不修改图像资源。

回归新增到 28 场景、133 断言，包括跨 Tick 同卡链、长文字不阻塞后续动作、并发双伤害/双名称，以及旧延迟飞行不能删除新 Tick 同格再生灵元。新证据单独保存于 `.local-verification/battle-presentation-parallel/`，真实战斗采样同时记录扫描位置、逻辑 Tick 和活动事件，以验证扫描与动画同时推进。

修正后验收通过：自动播放及单步各 741 帧与原版逐 Tick 全字段严格一致；名称存活样本跨 Tick 45—47，两侧扫描各有 12 个不同位置；伤害存活样本跨 Tick 65—68，各有 18 个不同位置；对撞存活样本跨 Tick 61—63，各有 9 个不同位置，全部样本的显示 Tick 与逻辑 Tick 相同。两种尺寸的长剑连贯卡面和 L 形空洞检查通过；新增同人物双名称/双伤害并发画面，数字两列分开，避免连成一个数。241 个核心 C# 文件与本次修正前一致，原保护范围 230 文件仍未变。机器摘要为该目录 `verification-summary.json`。
