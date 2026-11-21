## Purpose

规定策划可复用的战斗触发、选卡与动作模板，确保它们能经由发布的 Lua 接口按已记录的规则结算，并说明战斗广播与卡牌局部生命周期各自的用途。

## ADDED Requirements

### Requirement: 触发目录匹配战斗广播

触发目录 SHALL 对每个公开战斗广播以实际事件名登记一次，并描述真实发生时机。局部 Hook SHALL 在接口说明中记录各自的调用阶段。

#### Scenario: 查询公开广播

- **WHEN** 查询现行战斗触发配置
- **THEN** 收录下表中的广播。由生命周期直接调用的 `Contest`、`Tick`、`OnPassiveActivated`、`OnPassiveDeactivated`、`OnBeforePairedContest` 在接口说明中查阅

| 广播 | 发生时机 |
| --- | --- |
| OnUse | 满足末格资格且提交资源和行额度后 |
| OnApply | 卡牌的结算及对应词条调用后 |
| OnStraight | 轮末待发进入直击流程 |
| OnAttack | 非平局对拼的攻击侧及攻击直击 |
| OnDefend | 非平局对拼的防御侧 |
| OnContestOverflow | 非平局对拼产生胜负和差值后 |
| OnBaseManaDraw | 开场和本方轮末，基础放灵元前 |
| OnManaDraw | 成功放置至少一个灵元并扣 MP 后 |
| OnMPChanged | MP 实际发生变化后 |
| OnDamage | 护盾处理后实际扣 HP 时 |
| OnHeal | 实际增加 HP 后 |

#### Scenario: 需要判断胜负的监听

- **WHEN** 两张攻击卡以 4 和 5 点对拼，或攻击直接结算直击
- **THEN** 对拼的两个攻击侧和直击的攻击侧分别产生 `OnAttack`，仅非平局对拼的 `OnContestOverflow` 提供 Winner、Loser、WinnerType、LoserType、Overflow、WinnerCard、LoserCard

### Requirement: 可选模板能调用现行 Lua 接口

TCA 选择和动作模板 SHALL 使用已发布 Lua 入口的参数顺序和参数类型，描述执行阶段、目标和数量规则。选择模板 SHALL 使用现行 Domain。独立 CD、位移和候补部署及召回 SHALL 退出可选目录。旧 Lua 兼容接口遵循既有行为。

#### Scenario: 直接增减 MP 和格上操作

- **WHEN** 展开直接增减 MP、抽取或回收灵元模板
- **THEN** 直接增减 MP 使用 `RecoverMP(ctx, Domain, Amount)` 与 `ReduceMP(ctx, Domain, Amount)`，并受 MP 上下限约束。抽取只对成功放置的灵元扣 MP，回收按 MP 空缺与未分配灵元计算

#### Scenario: 结算量和模板描述一致

- **WHEN** 展开攻击拼点、冻结或移除伤势模板
- **THEN** 攻击参数生成拼点而非固定伤害，冻结生效到下次完整扫描，移除伤势按 `Exact=true` 等于体量或 `Exact=false` 至多该体量选择

### Requirement: 治疗按实际回复量结算

Heal SHALL 按角色的战斗 HP 上限结算。只有实际恢复正数 HP 时，系统 SHALL 派发 `OnHeal`，`ctx.Caster` 表示接受治疗的角色。

#### Scenario: 治疗超过缺少的 HP

- **WHEN** 角色当前 18 HP、上限 20 HP，收到 5 点治疗
- **THEN** 角色恢复 2 点至 20 HP，监听在事件中可读到 20 HP

#### Scenario: 无正向回复

- **WHEN** 对满 HP 角色治疗，或治疗量小于等于零
- **THEN** HP 保持当前值且没有 `OnHeal`
