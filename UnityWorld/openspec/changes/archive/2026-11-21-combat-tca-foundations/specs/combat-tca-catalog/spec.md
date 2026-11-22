## MODIFIED Requirements

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
| OnContest | 双方待发正式配对后、读取最终比较数值前 |
| OnContestOverflow | 非平局对拼产生胜负和差值后 |
| OnRoundStart | 参战者本轮开始，供能和基础补灵前 |
| OnRoundEnd | 参战者本轮扫描、拼点和伤害结算结束后 |
| OnBaseManaDraw | 开场和本方轮末，基础放灵元前 |
| OnManaDraw | 成功放置至少一个灵元并扣 MP 后 |
| OnMPChanged | MP 实际发生变化后 |
| OnDamage | 护盾处理后实际扣 HP 时 |
| OnHeal | 实际增加 HP 后 |
| OnExhausted | 消耗次数从正数扣至零后 |
| OnCardLeave | 卡从场地或候补离开后 |
| OnCardDisplaced | 四向位移原子提交且位置实际改变后 |

#### Scenario: 需要判断胜负的监听

- **WHEN** 两张攻击卡以 4 和 5 点对拼，或攻击直接结算直击
- **THEN** 配对时先收到一次 `OnContest`。对拼的两个攻击侧和直击的攻击侧分别产生 `OnAttack`，仅非平局对拼的 `OnContestOverflow` 提供 Winner、Loser、WinnerType、LoserType、Overflow、WinnerCard、LoserCard

### Requirement: 可选模板能调用现行 Lua 接口

状态为 `Ready` 的 TCA 选择和动作模板 SHALL 使用已发布 Lua 入口的参数顺序和类型，描述执行阶段、目标和数量规则。已接入的选择模板 SHALL 使用现行 Domain 或已发布的二维选择。独立 CD、旧线性位移和候补部署及召回保留为 `Pending`。旧 Lua 兼容接口遵循既有行为。

#### Scenario: 直接增减 MP 和格上操作

- **WHEN** 展开直接增减 MP、抽取或回收灵元模板
- **THEN** 直接增减 MP 使用 `RecoverMP(ctx, Domain, Amount)` 与 `ReduceMP(ctx, Domain, Amount)`，并受 MP 上下限约束。抽取只对成功放置的灵元扣 MP，回收按 MP 空缺与未分配灵元计算

#### Scenario: 结算量和模板描述一致

- **WHEN** 展开攻击拼点、冻结或移除伤势模板
- **THEN** 攻击参数生成拼点而非固定伤害，冻结生效到下次完整扫描，移除伤势按 `Exact=true` 等于体量或 `Exact=false` 至多该体量选择

## ADDED Requirements

### Requirement: 新基础机制提供可复用的 TCA 节点

配置和接口索引 SHALL 提供改当前拼点、下次拼点、装备属性、亲和、轮期限、延迟收益、卸力、护甲、坚甲、灵盾、法宝供能、四向滑移、位置存在与计数、消耗比较的可用节点。胜负、邻卡使用、被位移和全场位移 SHALL 能通过公开事件及对应条件表达。

#### Scenario: 组合邻卡使用与延迟加盾

- **WHEN** 策划配置右侧火法成功使用时，为本方在下一轮开始获得 2 灵盾
- **THEN** 可组合使用发布的触发、位置条件和延迟效果来执行，无需重写扫描、轮时钟或护盾扣减
