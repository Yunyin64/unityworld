# keyword-system Specification

## Purpose

规定卡牌关键词的注册加载、生命周期回调与状态访问方式，使同一卡牌能够组合被动效果、扫描机会和条件事件机会，并通过统一的资源、冻结与待发检查保证主动使用和事件分发的一致性。

## Requirements

### Requirement: Keyword 索引文件加载
LuaMgr 初始化阶段 SHALL 加载 `Data/LuaScripts/Keywords/Keyword.lua` 索引文件。该文件 SHALL 返回一个 table，key 为 keyword 名称（字符串），value 为对应 Lua 脚本的相对路径名（相对于 Keywords 目录）。LuaMgr SHALL 遍历该 table，加载每个 keyword 脚本并缓存为 `Dictionary<string, LuaTable>` 注册表。

#### Scenario: 正常加载 Keyword.lua
- **WHEN** LuaMgr.Init() 执行且 `Keywords/Keyword.lua` 存在并返回 `{ Passive = "Passive" }`
- **THEN** 注册表中包含 key "Passive"，对应的 LuaTable 为 `Passive.lua` 返回的 table

#### Scenario: Keyword.lua 不存在
- **WHEN** LuaMgr.Init() 执行且 `Keywords/Keyword.lua` 文件不存在
- **THEN** 输出警告日志，注册表为空，不影响其他功能

#### Scenario: keyword 脚本文件不存在
- **WHEN** `Keyword.lua` 中注册了 key "Foo" 指向 "Foo"，但 `Keywords/Foo.lua` 不存在
- **THEN** 输出错误日志，该 keyword 不加入注册表，其余 keyword 正常加载

### Requirement: CombatCard 生命周期遍历 keyword hooks
CombatCard 在 `PreStart`、`Start`、`Tick`、`End` 方法中 SHALL 遍历自身的 `Keywords` 列表，对每个 keyword 查询 LuaMgr 注册表获取对应 LuaTable，并调用 table 中对应的 hook 函数（`OnPreStart`、`OnStart`、`OnTick`、`OnEnd`）。Keyword hooks SHALL 在卡自身 Lua env hooks 之前调用。

#### Scenario: keyword 已注册且有对应 hook
- **WHEN** 一张卡的 Keywords 包含 "Passive"，注册表中有 "Passive"，且 Passive LuaTable 中定义了 `OnPreStart` 函数
- **THEN** CombatCard.PreStart() 中调用 Passive 的 `OnPreStart(card, ctx)`

#### Scenario: keyword 已注册但无对应 hook
- **WHEN** 一张卡的 Keywords 包含 "Passive"，注册表中有 "Passive"，但 Passive LuaTable 中未定义 `OnTick` 函数
- **THEN** CombatCard.Tick() 中静默跳过该 keyword 的 OnTick 调用，不报错

#### Scenario: keyword 未注册
- **WHEN** 一张卡的 Keywords 包含 "UnknownKW"，但注册表中无 "UnknownKW"
- **THEN** 输出错误日志 "Keyword 'UnknownKW' 未注册"

### Requirement: SetPhase API 暴露给 Lua
CombatCard SHALL 提供 `SetPhase(string phaseName)` 方法。该方法 SHALL 将字符串解析为 `CombatCardPhase` 枚举值并设置 `Phase` 字段。解析失败时 SHALL 输出错误日志且不改变当前 Phase。

#### Scenario: 有效的 phase 名称
- **WHEN** Lua 调用 `card:SetPhase("Passive")`
- **THEN** CombatCard.Phase 设为 CombatCardPhase.Passive

#### Scenario: 无效的 phase 名称
- **WHEN** Lua 调用 `card:SetPhase("InvalidPhase")`
- **THEN** 输出错误日志，Phase 保持原值不变

### Requirement: 多 Keyword 提供并列主动机会
同一 Card SHALL 可同时拥有法术与法宝 Keyword。法术提供完整扫描机会，法宝提供配置的事件条件机会，两者为或关系，共同尝试该 Card 的 Use 效果。法宝属性可供效果读取，但不得建立第二份卡实例、资源或独立 CD。

#### Scenario: 单一法术
- **WHEN** 卡只有法术扫描机会且满足本行扫描条件
- **THEN** 可尝试 Use，不因为不具有法宝条件而受阻

#### Scenario: 单一法宝
- **WHEN** 卡只有法宝条件机会且已经满灵，条件事件发生
- **THEN** 可尝试 Use，无需等完整扫描；扫描经过本身不会提供主动机会

#### Scenario: 法术与法宝共存
- **WHEN** 同一张卡同时具有法术与法宝 Keyword
- **THEN** 完整扫描和法宝事件均能独立提供机会，不要求两个条件同时成立

### Requirement: 所有主动机会共用使用资格
扫描与条件机会 SHALL 共用点亮、冻结、待发和使用状态检查；仅存活角色上的满灵、未冻结、没有正在待发的卡可成功使用。产生拼点还必须有空待发槽；成功才结束本次点亮，无拼点效果立即结算，有拼点效果按槽调度结算一次。

#### Scenario: 两条路径不重复消耗或重复使用
- **WHEN** 条件机会先成功使用，随后扫描机会到达，但卡没有重新点满
- **THEN** 第二次不使用，不能因另一个 Keyword 存在而复用之前的点亮

#### Scenario: 缺灵冻结和占槽均不消耗
- **WHEN** 条件事件发生但卡缺灵、被冻结、正在待发，或需要拼点但本方槽已占用
- **THEN** 本次机会失败，已有点亮与槽内数据保持不变，不积攒触发次数

#### Scenario: 无拼点不受其他卡占槽阻挡
- **WHEN** 满灵且未冻结的无拼点卡遇到条件，而另一张卡占着本方待发槽
- **THEN** 无拼点卡可成功使用并结算一次，不覆盖槽内数据

### Requirement: 条件机会不消耗扫描额度
条件事件触发 SHALL 不要求完整扫描、不占用或检查每行扫描发动额度；扫描额度仍只由成功的扫描 Use 消耗。事件失败不排队等待，也不在后来满灵或槽空时自动补触发。

#### Scenario: 行额度已用仍可条件发动
- **WHEN** 本行扫描额度已用，另一张满灵法宝遇到合法事件且其他使用条件满足
- **THEN** 法宝可使用，本行扫描额度仍保持已用

#### Scenario: 迟到点亮不补旧事件
- **WHEN** 法宝条件发生时缺灵，稍后才点满
- **THEN** 不补发旧事件；纯法宝等待新条件，混合法术卡仍可等待后续合法扫描机会

### Requirement: 条件判断保留原始事件上下文
Keyword 条件判断 SHALL 使用实际事件的施放者、来源卡、目标与事件数据，不得替换为监听卡自身上下文。触发 Use 时 SHALL 同时区分发动卡与触发事件来源，确保归属、目标和事件去重可追踪。

#### Scenario: 只响应主人抽灵
- **WHEN** 法宝配置响应主人抽灵，但收到对手抽灵事件
- **THEN** 条件不满足，不能把监听卡主人填入事件施放者而误发动

#### Scenario: 效果来源与触发来源不同
- **WHEN** 对手攻击事件触发本方法宝
- **THEN** 法宝效果来源是法宝自身，同时条件仍能读取对手攻击的原始来源

### Requirement: 单次事件按卡去重并防止递归重入
同一个实际事件经多个 Keyword 或分发通道到达同一卡时，SHALL 最多形成一次成功条件 Use。一次 Use 的资格验证与状态提交 SHALL 不可被其派生事件重入；事件链必须可追踪并拒绝无限自身触发，不能将后续独立事件一概当作重复。

#### Scenario: 同事件双 Keyword
- **WHEN** 同一事件符合一张卡两个条件 Keyword
- **THEN** 该事件最多成功使用该卡一次，不执行两次效果

#### Scenario: 同卡派生事件重入
- **WHEN** 卡的 Use 发出事件，该事件又试图在本次 Use 未完成时使用同一卡
- **THEN** 拒绝重入，不递归清空点亮或重复入槽

#### Scenario: 后续独立事件仍有效
- **WHEN** 上一次使用完成且卡重新满灵，之后发生新的独立合法事件
- **THEN** 卡可重新尝试 Use，不被旧事件去重记录永久锁死
