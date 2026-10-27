# 主动机会、事件和冻结验收记录

2026-10-25；本次变更实际执行。运行目录 `C:\Unity\Game\unityworld\UnityWorld`。

## 命令与实际结果

```powershell
dotnet build Console\UnityWorld.Console.csproj --no-restore -v quiet
dotnet run --project Console\UnityWorld.Console.csproj --no-build -- --activation-migration-tests
dotnet run --project Console\UnityWorld.Console.csproj --no-build -- --backpack-timing-tests
```

构建成功，0 错误。完整重编译显示 62 个既有/当前工作区告警（NU1900、NU1902、CS8632、CS0108、CS0114、CS0414）；增量编译告警数会不同。输出：

```text
ACTIVATION_EVENT_PRESSURE finite=5000 independent=100 budget=10000 diagnostics=64
ACTIVATION_MIGRATION_TESTS_OK assertions=91
BACKPACK_TIMING_TESTS_OK assertions=59
```

本地 `activation-run.log` 中搜索 `ERR|异常|失败|nil` 无匹配；故意构造的循环异常由测试捕获并检查场景 Finished 和诊断内容，未吞掉后续合法事件。上述日志文件是运行产物，不作为必需代码资产。

## 任务对应

| 任务 | 实现及实测证据 |
| --- | --- |
| 3.1 / K1 | `HasScanOpportunity`、`HasEventOpportunity`、`HasActiveAbility` 分离；FaShu/ZhaoShi/FaBao Lua 不再推进 CD。真实 `card_fabao_jian` 纯法宝扫过不发动、主人抽灵发动；同一真实卡加 FaShu 后扫描与条件均发动，CD 保持 0。 |
| 3.2 / K1-K2 | `TryCommitUse` 原子检查存活、活动战斗、点亮、冻结、执行中/待发及拼点槽；提交前锁定，清点亮一次，Use/Apply 计数各符合预期。缺灵、被冻结、待发、槽满不消耗。重复 Apply 无效果。 |
| 3.3 / K2-K4 | 明确 `ProducesContest`；false 时跳过旧/空 Contest，不因他卡占槽被阻挡；true 必须空槽。null 仅用于旧专用测试兼容推断。 |
| 3.4 / K2-R1 | 扫描局部行额度仅由成功扫描 Use 设置；同一行左卡 Apply 事件可让右法宝合法发动。失败事件在本次分发结束后失效，补满不能持旧事件补发；纯法宝等待新事件。原迟到扫描规则由既有背包回归覆盖。 |
| 3.5 / K4 | Passive 不按 Cooldown 自动派生，纯被动保留开场 Apply；主动混合 Passive 无免费 Apply。Start/OnStart 与 keyword OnTick/Tick 二选一，去重 Keyword 名；混合冻结卡自身 Tick、OnPassiveTick、keyword Tick 各 3 次；纯 Passive 开场仅 Apply 1 次。 |
| 4.1 / K4 | 同一次事件的 `APIContext` 原对象经过自身 hook、Keyword、EventMgr；`Caster/SourceCard` 不改成监听卡。新效果上下文属于发动卡并通过 `TriggerEvent` 保留原事件。真实长剑忽略敌方抽灵；受击反例区分受击者和敌方来源卡。 |
| 4.2 / K3 | 每事件请求及成功卡实例集合；多 Keyword、重复 Keyword、EventMgr 双通道只成功 1 次，即使 Apply 立即重新点满。两通道先收集，再统一按 Npc Id、位置 Y/X、实例 Id 提交；倒置 Field、逆序创建、Lua 右卡先请求/C# 左卡后请求、两个 C# listener 反序注册均由左卡争得槽，右卡保留资源且槽释放不补发。独立新事件可再次成功。 |
| 4.3 / K5 | Scene 队列在当前效果结束安全点排空，不递归分发。自身 Use 派生事件拒绝重入；缺灵 A→B→A 正常结束；有资源互相触发明确抛异常并置 Finished。5000 事件合法链完整处理，100 独立事件不共用旧预算；10001 条同链事件停止并保留最多 64 条来源诊断。 |
| 4.4 / K1-K4 | `CardBase:UseFabao(ctx)` 使用收到的真实事件；长剑仍读取已绑定 long_sword，真实 JSON/Lua 算出 Attack=4 待发，轮末 Apply 1 次。其 Lua 中资源文档同步为 Jin=2。 |
| 5.1 / F1-F3 | 战斗卡独立 `IsFrozen` 与非叠加 `Freeze()`；所有主动机会统一拒绝冻结；混合被动 Tick 及蓄灵持续；条件失败保留点亮。 |
| 5.2 / F1-F3 | 扫描末格先消费冻结，再判断能力、资源及行额度。满灵左冻右用、缺灵/额度已用仍解冻、多格非末格保持且末格解除；原待发被冻结仍结算并保留冻结，下次完整扫描跳过不补发。 |

## 最终调用约定

- 卡自身 Lua：`function card:Hook(ctx)`，实际传 `(env, ctx)`；Keyword：`function Keyword.Hook(card, ctx)`，实际传 `(CombatCard, ctx)`。
- 生命周期 `PreStart`/`Start` 优先无 On 前缀，缺少时兼容 `OnPreStart`/`OnStart`；Keyword Tick 优先 `OnTick`，缺少时兼容 `Tick`。同一回调不调用两次。主动卡自身保留 `Tick`；纯被动不执行旧自身 Tick，执行 `OnPassiveTick`；主动+Passive 也执行 `OnPassiveTick`。
- `Apply` 是执行效果生命周期；`OnApply` 是之后广播的实际事件，两个名称职责不同。`Contest` 只在显式拼点或兼容推断拼点时调用。
- 条件事件 Hook 原样传入 `ctx`；`ctx.EventId` 标识事件，`ctx.ChainId` 标识派生链，`ctx.EventName` 标识类型。新发动效果的 `ctx.SourceCard/ctx.Caster` 是发动卡及主人，`ctx.TriggerEvent` 是原触发事件。例如长剑使用 `if ctx.Caster ~= self.m_Owner then return end; self:UseFabao(ctx)`。
- `TryUseFromEvent` / `UseFabao(ctx)` 返回“该卡在本事件中的机会已接收”，不是最终 Use 成功。仅接受当前事件收集阶段的请求；缺资源等立即拒绝，重复请求返回 false。两通道收集后按稳定位置重新验证并提交，槽争夺失败的请求随本事件结束清除，不跨事件缓存。过期事件不能重试。自身事件由 `SourceCard` 识别，事件归属使用原 `Caster`，不要重建监听卡上下文。
- 合法 5000 事件链已实测；10000 上限是留出至少一倍压力裕量的诊断保护，超过后显式中止整个异常战斗，并不保留半截战斗继续运行。不使用事件名称作为永久去重键，不添加玩法冷却。

## 集成交界补充

两通道排序已统一，不依赖自定义 C# listener 的注册顺序。根代理新增行边界双方槽快照，每角色在同批只参与一次对拼；`DerivedPendingWaitsForNextBoundary` 验证对拼 Apply 派生的新槽等待后续边界。此补充后实际重跑上述两个分支，分别 91 / 59 断言通过；`activation-run.log`、`activation-timing.log` 无 Lua 异常。
