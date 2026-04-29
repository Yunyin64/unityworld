## 1. Day1 — 战斗基本框架重构

- [x] 1.1 新增枚举：CardType（ZhaoShi/FaShu/FaBao/DanYao/ZhenFa/ShenTong）、PhysicalType（Zhan/Ci/Da/SheJi）、CombatEndReason新增SpaceOverflow
- [x] 1.2 新建 CombatCardState 类：currentCdTick、isManaFulfilled、cardData引用、拼点数值提取方法
- [x] 1.3 重构 CombatNpc：移除 DeckSequence/CurrentDeckIndex/CycleCount，改为 List<CombatCardState> 卡组、SP属性、待发槽(PendingSlot)、灵元池(ManaPool)
- [x] 1.4 重构 CombatScene：将 NextTurn() 改为 Tick()，实现每Tick推进所有卡计时器的主循环
- [x] 1.5 实现待发槽机制：入槽、溢出直击、双方有卡触发对拼
- [x] 1.6 实现对拼结算：数值比较、赢方效果（攻击→伤害/盾→加血/防→消失）、赢家通吃规则
- [x] 1.7 实现 SP 溢出判负检查（每Tick检查）
- [x] 1.8 实现 HP清零→伤势卡生成→塞入卡组→HP恢复50% 流程
- [x] 1.9 更新 CombatResult/CombatantResult：新增伤势卡列表(InjuryCards)字段

## 2. Day2 — 卡牌数据战斗适配重构

- [x] 2.1 扩展 ActionDefine：新增可选字段 atkValue/shieldValue/defendValue/element/physicalType，更新 JSON 序列化
- [x] 2.2 扩展 CardDefine：新增 cardType、manaCost 字段，更新 JSON 序列化
- [x] 2.3 扩展 CardData：同步新增 CardType、ManaCost 字段
- [x] 2.4 扩展 EffectData：新增从 ActionDefine 汇总战斗数值的逻辑（判断是否为攻防Effect）
- [x] 2.5 实现 CardData 层面的"是否攻防卡"判定方法（遍历 Effects 检查是否含攻防数值）
- [x] 2.6 实现伤势卡生成逻辑：根据伤害数值映射为不同严重度的伤势CardData
- [x] 2.7 实现基础 Mana 系统框架：灵元池数据结构、转化逻辑（扣MP→产灵元）、消耗逻辑
- [x] 2.8 在 CombatScene.Tick 中接入 Mana 定期转化检查
- [x] 2.9 更新现有 ActionDefines.json 数据，为已有Action补充战斗数值字段
- [x] 2.10 回填：CombatCardState.GetContestValue/GetContestType/GetPhysicalType/GetElement 改为从 ActionDefine 汇总
- [x] 2.11 回填：移除 CardData 临时字段（ContestValue/ContestType/PhysicalType），CardType 保留为正式字段
- [x] 2.12 回填：CombatNpc.Mp/ManaPool 接入 Mana 系统
- [x] 2.13 回填：ResolveEffectCard 接入完整 Effect 执行逻辑
- [x] 2.14 回填：CombatScene Tick 中效果卡结算替换占位实现

## 3. Day3 — 基础30张卡牌设计

- [x] 3.1 设计攻击卡（招式类，无Mana需求）：约8张，覆盖斩/刺/打/射击四种物理类型，不同Cost/CD/攻击值组合
- [x] 3.2 设计攻击卡（法术类，有Mana需求）：约6张，覆盖五行属性，数值高于同级招式卡
- [x] 3.3 设计盾卡：约4张，不同Cost/CD/盾值组合
- [x] 3.4 设计防卡：约4张，高数值短CD
- [x] 3.5 设计效果卡（丹药/阵法等）：约4张，治疗/增益等直接结算效果
- [x] 3.6 设计伤势卡模板：约4张，出血/骨折/内伤/重创，不同Cost和负面效果
- [x] 3.7 将30张卡写入 CardDefines.json，对应的 Action/Effect/Trigger/Condition 写入各自 JSON
- [x] 3.8 验证所有 JSON 数据可被 DefineMgr 正确加载
- [x] 3.9 回填：CreateInjuryCard 从灵药卡模板 Define 查询替换硬编码
- [x] 3.10 回填：伤势严重度映射从 Define 规则替代硬编码阈值
- [x] 3.11 🔄回填(Day2)：CombatSpilloverHandler.CreateInjuryCard 适配新数据结构——移除对已删除的 ContestType/ContestValue 字段赋值，伤势卡自伤效果改为包含 SelfDamage(N) 的 ActionData（通过 EffectData.Actions）
- [x] 3.12 🔄回填(Day2)：设计伤势卡 Define 时使用 funcName+params 格式，如 `{"funcName":"SelfDamage","params":[1]}`
- [x] 3.13 🔄回填(Day2)：30张卡的 ActionDefines.json 全部使用 funcName+params 新格式
- [x] 3.14 🔄回填(Day2)：30张卡的 CardDefines.json 全部填写 cardType + manaCost 字段

## 4. Day4 — 战斗流程跑通

- [x] 4.1 编写战斗测试入口：构造2个CombatNpc，各装3-5张卡，发起一场完整战斗
- [x] 4.2 验证 Tick 循环正确推进所有卡计时器
- [x] 4.3 验证待发槽入槽/溢出直击/对拼触发流程
- [x] 4.4 验证对拼结算（攻vs攻、攻vs盾、攻vs防、赢家通吃）
- [x] 4.5 验证 HP清零→伤势→SP溢出判负 完整链路
- [x] 4.6 验证 Mana 转化→灵元消耗→卡启动CD 流程
- [x] 4.7 验证效果卡（非攻防）直接结算流程
- [x] 4.8 调整明显不合理的数值（CD/攻击/盾/防/HP/SP），确保战斗不会一瞬秒杀也不会无限拖延
- [x] 4.9 🔄回填(Day2)：实现 Action 执行器（ActionResolver）——至少支持 Heal(N)→恢复HP、SelfDamage(N)→自伤，使效果卡和伤势卡真正生效
- [x] 4.10 🔄回填(Day2)：CombatCardFlowHandler.ResolveEffectCard 接入 Action 执行器——效果卡 CD 满时遍历 EffectData.Actions 调用执行器，替换纯日志占位
- [x] 4.11 🔄回填(Day2)：实现对拼后续结算——拼完后执行 OnUse Effect 中非拼点的 Action（如 AddPoison），检查 OnContestWin/OnContestLose Trigger 的 Effect
- [x] 4.12 🔄回填(Day2)：DamageInfo.CreateInjurySelfDamage 接入 ActionData——从伤势卡的 ActionData 中读取 SelfDamage 值，替换 ⏳ 占位
- [x] 4.13 实现 Condition 选择器模式——Condition 运行时不仅返回 bool，还可以向 ActionContext 写入选择结果（如 TargetCardId）。需要在 Condition 判定流程中支持：根据 conditionId 执行对应硬编码逻辑，选择结果写入共享 context 供后续 Action 读取
- [x] 4.14 实现 `cond_random_enemy_card_in_cd` 判定逻辑——从敌方 CombatNpc 的 CombatCardState 列表中筛选正在 CD 中的卡（currentCdTick < Cooldown），随机选一张，将其 CardId 写入 context 的 `TargetCardId` key。若无可选卡牌则返回 false，整个 Effect 不触发
- [x] 4.15 实现 Freeze Handler——标记 `[APIFunc("Freeze")]` 的静态方法，从 ActionContext 读取 `TargetCardId` 和 `FreezeTick`，在目标 CombatCardState 上暂停 CD 计时器指定 tick 数（如设置 frozen 标记，Tick 推进时跳过该卡的 CD 递增）

## 5. Day5 — 战斗Log与NPC接通

- [ ] 5.1 实现完整的战斗过程 Log：每Tick事件、对拼详情、伤势生成、判负原因
- [ ] 5.2 实现战斗结果摘要 Log：胜负方、总Tick、每NPC最终状态
- [ ] 5.3 CombatScene.PreStart 接入大世界NPC：从Npc读取体魄→HP、神识→SP、蓝条→MP
- [ ] 5.4 CombatScene.PreStart 接入NPC卡组：从Npc的CardData列表构建CombatCardState列表
- [ ] 5.5 实现战斗结算回写接口：CombatResult中的HpLost和InjuryCards回写到大世界Npc
- [ ] 5.6 在 WorldMgr 或合适位置添加发起战斗的便捷方法，串联完整流程
- [ ] 5.7 端到端测试：从大世界NPC发起战斗→自动推演→回写结果→验证NPC状态变化
- [ ] 5.8 回填：CombatScene.PreStart 从真实 Npc 读取 HP/SP/MP/CardStates
- [ ] 5.9 回填：移除 SetupTestCombatNpc 硬编码占位
- [ ] 5.10 🔄回填(Day2)：CombatScene.PreStart 中 Mp 从真实 NPC Stat 读取，替换硬编码 100f
- [ ] 5.11 🔄回填(Day2)：CombatScene.PreStart 中 ManaPool 初始化规则——根据 NPC 道途/功法决定初始灵元类型和数量
- [ ] 5.12 🔄回填(Day2)：移除 SetupTestCombatNpc 中 ManaPool 硬编码初始化（随 5.9 一并清理）
