## ADDED Requirements

### Requirement: 目录保留待实现的设计

TCA 配置 SHALL 保留待实现条目的 ID、参数、原始设计说明及评分数据，并标记 `Pending` 和说明当前缺口。已经接入的条目标记 `Ready`，旧数据省略状态时视为 `Ready`。完整目录 SHALL 能查到两种状态。可执行模板筛选和 Lua 触发映射只使用 `Ready`。

#### Scenario: 查阅待实现设计

- **WHEN** 按原 ID 查阅候补部署、召回、位移、旧对象或 CD 选择器，或尚未派发的触发
- **THEN** 目录返回 `Pending` 条目及原设计资料。Lua 触发映射和可执行模板列表不纳入这些条目

#### Scenario: 旧数据兼容

- **WHEN** 加载已接入但配置未声明状态的模板
- **THEN** 该条目按 `Ready` 使用，仍可按已对齐的 Lua 签名执行

## MODIFIED Requirements

### Requirement: 可选模板能调用现行 Lua 接口

状态为 `Ready` 的 TCA 选择和动作模板 SHALL 使用已发布 Lua 入口的参数顺序和类型，描述执行阶段、目标和数量规则。已接入的选择模板 SHALL 使用现行 Domain。独立 CD、位移和候补部署及召回保留为 `Pending`。旧 Lua 兼容接口遵循既有行为。

#### Scenario: 直接增减 MP 和格上操作

- **WHEN** 展开直接增减 MP、抽取或回收灵元模板
- **THEN** 直接增减 MP 使用 `RecoverMP(ctx, Domain, Amount)` 与 `ReduceMP(ctx, Domain, Amount)`，并受 MP 上下限约束。抽取只对成功放置的灵元扣 MP，回收按 MP 空缺与未分配灵元计算

#### Scenario: 结算量和模板描述一致

- **WHEN** 展开攻击拼点、冻结或移除伤势模板
- **THEN** 攻击参数生成拼点而非固定伤害，冻结生效到下次完整扫描，移除伤势按 `Exact=true` 等于体量或 `Exact=false` 至多该体量选择
