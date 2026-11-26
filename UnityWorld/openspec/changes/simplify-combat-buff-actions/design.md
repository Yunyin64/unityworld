# Design

## Context

配卡时，Action 负责描述可选效果，Buff 持有战斗中的状态，Lua 在对应结算点读取状态。治疗、灵盾和 MP 回复已有完整的数值修正钩子。法宝供能也使用 Modifier 存层数和轮期限，但目前由 `BeginBackpackRound` 在公开轮首事件结束后直接调用供能方法。两处变更分别把参数选择收回目录、把供能规则收回 Buff。 [提案](proposal.md) · [目录增量规格](specs/combat-tca-catalog/spec.md)

## Goals / Non-Goals

让策划从三个数值修正入口配出已有组合，让 Mod 能沿用状态回调实现轮首效果。效果等价性以现有 Buff 的结算阶段、层数和到期规则为准。法宝供能以现有候选规则、场景随机和充入事件为准。本次范围限定于这十二条数值修正模板和法宝供能。

## Decisions

### 每种结算提供一个 Action

三条模板使用同一套用途、点数与有效期。用途同时决定目标和正负层，治疗额外通过用途选择出疗或受疗阶段。一个有效期选项表达完整生命周期，使「下一次」和正数 Tick 期限无法同时选中。

| Action | 强化自身 | 削弱对手 |
| --- | --- | --- |
| 治疗量调整 | `Self`，正层，`All_HealOut_Changed` | `Target`，负层，`All_HealIn_Changed` |
| 灵盾获得量调整 | `Self`，正层，`All_ShieldGain_Changed` | `Target`，负层，`All_ShieldGain_Changed` |
| MP 回复量调整 | `Self`，正层，`All_MpRecovery_Changed` | `Target`，负层，`All_MpRecovery_Changed` |

共用一个 Lua 参数转换入口，根据上表调用 `AddNpcBuff`。选「下一次」时用对应的 `_Once`，传 `Duration = -1` 等待首次匹配。选「本场」时用持续版和 `-1`。选 30、60、90 时用持续版和对应 Tick 值。同 ID 的后续施加遵循现有叠层和首次期限规则。例如自己获得灵盾的基础量为 3，持续版正 2 层有效时获得 5 点，到期后获得 3 点。 [治疗 Buff](../../../Data/Modifier/CombatModifierDefines/Heal_Changed.json) · [资源 Buff](../../../Data/Modifier/CombatModifierDefines/Resource_Changed.json)

独立开放目标、正负和 Buff ID 会让目录出现额外组合，例如给自己挂受疗削弱。直接复用原十二个选择也能避免这类组合，但需要策划在目录先决定效果的完整变体。用途联动保留原选择范围，查找入口时只需先选结算类型。

### 法宝供能由持有者的 Buff 执行

继续使用现有 `ArtifactPower_None` 与五行定义，每个定义加载同一套供能 Lua 逻辑。公开 Lua 包装将 `Element` 映射到定义，以 `Amount` 为层数，经通用 Buff 施加入口挂到 `Domain`。通用入口按现有 `AddRoundModifier` 路径接收可选的轮期限，未传轮数的调用仍按原来的 `Duration` 处理 Tick。相同定义且同一到期轮可叠层，期限不同分别到期。旧的底层派发名仅兼容转发到 Lua 包装。

```text
公开 OnRoundStart 派发完毕
        ↓
调用持有者有效 Modifier 的通用轮首回调
        ↓
法宝供能 Buff Lua 按层对合格法宝蓄灵
        ↓
OnBaseManaDraw → 基础补灵
```

复用 `ModifyHook` 的分发方式，在现有 `PowerArtifacts` 的位置调用通用 Modifier 回调。直接订阅公开 `OnRoundStart` 会把供能移入战斗事件队列，从而改变同事件施加 Buff、充入事件及后续补灵的先后顺序。保留当前的结算位置，轮首回调施加的状态仍能赶上当次供能。持有者施加当轮计入正数期限，`Rounds = 0` 为本场。例如第 2 轮中段施加 2 轮供能，能在第 3 轮首结算，第 3 轮末到期。 [轮首顺序](../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcRoundFunc.cs) · [轮期限](../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcData.cs)

Lua 负责筛选场上主关键词为法宝的卡、区分指定色和通用缺口、按层循环。先用场景随机选同优先级的卡，再选该卡可接收的元素，保持卡和元素的稳定排序以及每枚灵元后的缺口重算。指定五行供能只提供该色。任意供能遇到纯通用缺口时选择真实五行。没有候选就结束。候选资格沿用卡牌蓄灵检查，成功提交复用统一的蓄灵写入、表现和 `OnManaCharged` 广播。若 Lua 缺少必要入口，补齐通用蓄灵或查询能力。由 C# 接收候选列表并完成全部供能会继续把法宝供能规则锁在引擎层。 [当前候选与选择](../../../Scripts/Game/Domain/Combat/CombatNpc/CombatNpcManaFunc.cs) · [蓄灵提交](../../../Scripts/Game/Domain/Combat/CombatCard/CombatCardManaFunc.cs)

## Risks / Trade-offs

- 合并会改变目录入口 ID。旧引用经确定的迁移表映射，保留原点数与首次有效期。旧卡 Lua 已直接调用 `AddNpcBuff` 或 `AddArtifactPower` 时可继续运行。
- 合并会改变用等概率选单个有效期时的抽样分布。每类继承原四条的总权重 4。选择有效期时先在单次和持续之间等权选择，落到持续再从四个期限选一项。原单次没有期限评分，持续期限和点数沿用原评分。 [抽样器](../../../tools/sample_tca.py)
- 轮数与 Tick 的单位不同。用持有者轮序验证供能的施加、到期和移除，并用混合供能、不同轮次和固定种子验证随机及事件顺序。

## Migration Plan

先让三条新模板、旧数据映射和预览能够产出等价 Lua，再切换可选目录。旧 ID 映射见增量规格，源配置或草稿可按需迁移。供能在同一次切换中接入 Buff 回调并撤下轮首专用结算，避免同一轮重复充入。发布前分别走公开 Lua 包装和旧派发名验证，两条路径应产生相同状态。
