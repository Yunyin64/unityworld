# keyword-system Specification

## Purpose

规定卡牌关键词的注册与唯一 Main 判定，由 Main 决定卡牌的核心运转方式。法术、法宝与招式由背包扫描提供主动发动机会，被动 Main 与已激活的法宝按各自生命周期响应效果。

## Requirements

### Requirement: Keyword 注册与 Main 标记
Keyword SHALL 以注册名标识，注册时可声明 `Main`。系统 SHALL 在初始化时加载 Keyword 目录中的自注册脚本，并为卡牌定义校验和战斗提供同一份 Main 判定。法术 `FaShu`、法宝 `FaBao`、被动 `Passive`、招式 `ZhaoShi` SHALL 标为 Main，未声明 Main 的 Keyword 视为普通 Keyword。

#### Scenario: Main 标记来自 Keyword 定义
- **WHEN** 定义目录中的卡分别引用 `Passive` 和 `Wound`
- **THEN** 按注册定义识别 `Passive` 为 Main、`Wound` 为普通 Keyword，卡牌配置无需重复声明角色

#### Scenario: 单个 Keyword 脚本加载失败
- **WHEN** 一个自注册脚本加载失败
- **THEN** 报告该脚本，其余可正常注册的 Keyword 仍可加载

### Requirement: 每张卡恰好拥有一个 Main Keyword
每张 Card SHALL 恰好拥有一个 Main Keyword。该约束适用于配置卡、存档卡、程序生成卡和战斗中创建的卡。系统 SHALL 在创建或读入 Card 时校验，报告缺少 Main 或 Main 冲突的卡与相关 Keyword。已合法创建的卡 SHALL 始终维持此约束，卡库、装配与战斗 SHALL 对同一卡使用同一个 Main。

#### Scenario: 普通 Keyword 与 Main 共存
- **WHEN** 卡的 `Keywords` 为 `Passive` 和 `Wound`
- **THEN** 卡以 `Passive` 运转，同时保留 `Wound` 的伤口行为

#### Scenario: 缺少 Main
- **WHEN** 卡只配置了 `Item`、`Wound` 或丹药等普通 Keyword
- **THEN** 系统报告缺少 Main，须补全合法 Main 才能创建或载入该卡

#### Scenario: 两个 Main 冲突
- **WHEN** 卡同时引用 `FaShu` 和 `FaBao`
- **THEN** 系统报告两个 Main 冲突，调整 Keyword 顺序也不能通过校验

### Requirement: Main 决定核心运转方式
卡牌 SHALL 由其唯一 Main 决定核心运转方式。普通 Keyword 可用于分类、附加效果或修正 Main 的参数。`FaShu` 和 `ZhaoShi` 的成功发动进入各自的单次结算流程，`FaBao` 的成功发动激活或续期自身被动，`Passive` 在场期间常驻激活。三种主动 Main 的发动入口统一为背包扫描到本卡末格。

每次实际扫到本卡末格，核心 SHALL 只向该卡主关键词的 `Lifecycle.OnScanEnd(card, ctx, committed)` 同步提交一次结果。成功时灵元、次数和行额度已提交，主词在 `OnUse` 广播之前处理结果。失败时保留资源和额度，主词在本次扫描解冻之前处理结果。入场、阶段切换和 Apply 完成后的类型专属规则由主关键词 Lua 定义。核心对所有卡使用同一个 `Phase`。

#### Scenario: 附加效果保留原有发动入口
- **WHEN** 一张法术有普通 Keyword 扩展其伤害效果
- **THEN** 法术仍在完整扫描时尝试发动一次，附加效果按该卡定义处理

#### Scenario: 旧事件不会发动法宝
- **WHEN** 满灵法宝在扫到末格之前收到战斗事件
- **THEN** 卡保留蓄灵并等待完整扫描。若它已经激活，事件可触发符合条件的法宝被动

### Requirement: 法宝的激活持续到下次扫完该卡
`FaBao` SHALL 在首次成功发动时开启自身被动，并在下次完整扫描到该卡末格时处理到期。法宝处于激活期间 SHALL 继续蓄灵。末格检查通过时，法宝 SHALL 使用新一份满灵和本行发动额度连续续期，同一被动只保留一份激活状态。检查失败时旧激活 SHALL 当场结束，已蓄灵元保留。法宝被动仅在激活期间生效。

#### Scenario: 末格成功续期
- **WHEN** 两格法宝在第 2 轮第 3 行激活，并在第 3 轮第 3 行的末格前重新蓄满两个匹配灵元，其他发动条件满足
- **THEN** 到末格时清空这次蓄灵并占用该行额度，被动连续维持到下一次扫完本卡

#### Scenario: 续不上时关闭但保留资源
- **WHEN** 激活中的法宝再次被完整扫过，但尚未满灵、被冻结或本行额度已经使用
- **THEN** 到末格时关闭被动，已蓄灵元保留，等待下一次合法扫描

### Requirement: 招式按装备绑定并独立限制每轮次数
`ZhaoShi` SHALL 绑定一件当前有效且提供该招式的装备实例。招式 SHALL 按自身效果使用绑定装备提供的攻防基础进行对拼，装备自身的法宝被动激活状态与该绑定的有效性独立。每张招式卡的每轮成功发动上限 SHALL 默认是 1，可按卡牌规则修正。该次数按招式实例记录，以所属背包完整扫描一轮为周期，失败不记次数。

#### Scenario: 法宝未激活时仍可出招
- **WHEN** 长剑已经装备但自身被动尚未激活，它带出的剑斩满灵并满足扫描发动条件
- **THEN** 剑斩从长剑读取攻防并进入对拼流程，长剑自身的被动仍按法宝激活状态处理

#### Scenario: 同轮再次扫到已经发动的招式
- **WHEN** 默认上限的招式在本轮成功发动一次，因移位再次满灵扫到末格
- **THEN** 招式保留蓄灵及本行额度，下一轮重新获得一次机会

#### Scenario: 同招式绑定到不同装备
- **WHEN** 长剑和短剑都提供同一个剑斩定义，其中长剑对应实例已经用完本轮次数
- **THEN** 短剑对应实例仍可按自己的发动资格使用，读取短剑的攻防并记录自己的次数

### Requirement: CombatCard 生命周期遍历 keyword hooks
CombatCard 的入场前、入场及 Tick SHALL 同步交给唯一主关键词的 `Lifecycle`。主词按自身规则选择卡脚本和普通关键词的回调。`CallLua` 先执行本卡脚本，再按关键词列表派发同一上下文。离场时核心保留待发拼点，其余阶段回到 `Waiting`，主词按实际阶段变化处理失活。生命周期入口独立于同名的全场事件。

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
- **WHEN** Lua 调用 `card:SetPhase("Active")`
- **THEN** CombatCard.Phase 设为 CombatCardPhase.Active

#### Scenario: 无效的 phase 名称
- **WHEN** Lua 调用 `card:SetPhase("InvalidPhase")`
- **THEN** 输出错误日志，Phase 保持原值不变

### Requirement: 条件判断保留原始事件上下文
卡牌被动判断战斗事件时 SHALL 使用实际事件的施放者、来源卡、目标与事件数据。结算被动效果时 SHALL 区分被动所属卡与触发事件来源。

#### Scenario: 只响应主人抽灵
- **WHEN** 卡牌被动的条件是主人抽灵，实际收到对手抽灵事件
- **THEN** 按对手是原始施放者判定条件不满足

#### Scenario: 效果来源与触发来源不同
- **WHEN** 激活中的长剑监听到符合条件的斩击对拼胜利，并给予下次招式对拼点数加成
- **THEN** 条件读取原斩击及对拼结果，加成的来源仍是长剑

### Requirement: 次数规则由本卡的普通关键词参与

Mod 使用的弹药、消耗、灵活 SHALL 作为普通 Lua 关键词与原有 Main 组合。主关键词处理末格结果并控制卡的阶段；弹药和消耗关键词 SHALL 决定何时调用卡的资源操作，灵活关键词 SHALL 决定是否及何时请求离场。脚本继续可读卡的容量和当前存量，并可通过现有 C# 方法提交实际扣减；同一资源变化的所有调用者可观察到一致的结果与资源事件。

#### Scenario: 带弹药的法术完成原有待发

- **WHEN** 带弹药和灵活的法术使用了最后一发，本次攻击进入待发槽
- **THEN** 卡保留法术的待发与后续结算时序；额度耗尽只影响后续使用。

#### Scenario: 脚本复用现有扣份操作

- **WHEN** Mod 对还剩 1 份的消耗卡调用既有 `TryConsume(2)`
- **THEN** 实际只扣 1 份，返回 1，库存变为 0。同一次操作各产生一次“消耗时”和“消耗耗尽”；是否因灵活离场由该关键词在适当时机判断。

### Requirement: 灵活可跟随弹药或物品耗尽离场

灵活 SHALL 对弹药卡和消耗卡都生效。耗尽且最后一次效果仍在处理时，卡应保留其完成该效果所需的在场和待发状态。处理完毕或因战斗结束取消待发后，仍然耗尽的灵活卡 SHALL 经通用离场流程离开一次。实际离场释放所占格和对应 SP，并向监听者发出一次离开事件。

#### Scenario: 两格攻击法术从耗尽到腾出 SP

- **WHEN** 占 2 格的灵活法术使用最后一发进入待发槽，期间没有得到补弹
- **THEN** 待发期间仍占原格和 2 SP。该次攻击完成并因灵活离场后，两格可用，已占 SP 减少 2。监听者此时才能收到“触发灵活时”，通用离开事件仍只发生一次。

#### Scenario: 留场和离场分别用于不同构筑

- **WHEN** 两张同样的法术都用尽弹药，其中仅有一张配置灵活，最后一击均已处理完毕
- **THEN** 带灵活的法术离场；另一张法术继续占位，可以成为在场补弹效果的目标。

#### Scenario: 灵活法宝的最后一个激活期

- **WHEN** 灵活法宝成功用最后一发续期，此后直到下次扫到末格均未补弹
- **THEN** 本次续期完整生效，期间继续占格、响应卡面效果并可蓄灵。下次末格因空弹续期失败，法宝先失活，再按灵活规则实际离场一次，释放所占 SP。

#### Scenario: 到期前补弹可继续续期

- **WHEN** 上述法宝在下次末格之前补回一发，到末格时也满足满灵、未冻结和本行额度等原有条件
- **THEN** 法宝留场并正常续期，补回的一发用于这次续期。本次从 1 发再降到 0 发形成一次新的弹药耗尽。
