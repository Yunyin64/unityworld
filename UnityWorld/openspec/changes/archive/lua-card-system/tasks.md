## 1. TCA 底层设计重构（ActionDefine/ConditionDefine 参数化）

- [x] 1.1 设计新的 ActionDefine JSON 格式（参数化 Params），编写 2-3 个样例（action_atk, action_charge, action_heal）
- [x] 1.2 设计新的 ConditionDefine JSON 格式（参数化 Params），编写 2-3 个样例
- [x] 1.3 修改 ActionDefine.cs 数据结构（Params 从 object[] 改为 List<ParamDef>）
- [x] 1.4 修改 ConditionDefine.cs 数据结构（同上）
- [x] 1.5 修改 ActionDefineMgr / ConditionDefineMgr 的加载逻辑适配新格式
- [ ] 1.6 重写 Data/Action/ 目录下所有 ActionDefine JSON 为参数化格式
- [ ] 1.7 重写 Data/ConditionDefines.json 为参数化格式

## 2. LuaMgr 基础设施

- [x] 2.1 创建 LuaMgr.cs（IDomainMgrBase，单例，NLua.Lua State 生命周期管理）
- [x] 2.2 实现 C# API 自动注册（扫描 [APIFunc] 方法 → 注册为 Lua 全局函数）
- [x] 2.3 实现 LoadCardScript(cardId) — 加载 .lua 到独立环境
- [x] 2.4 实现 Hook 函数发现（扫描环境中 OnXxx 函数名，返回列表）
- [x] 2.5 在 WorldMgr._mgrs 中注册 LuaMgr（确定初始化顺序：须在 APIMgr 后）

## 3. LuaEventListener 实现

- [x] 3.1 修改 LuaEventListener.cs — 实现 OnEvent 真实桥接（调用 Lua 函数，传 ctx）
- [x] 3.2 实现 ActionContext → Lua table 的适配（Caster/Target/SelfCardId/事件数据）
- [x] 3.3 异常处理：try-catch 包裹 Lua 调用，LogMgr.Instance.Err 记录

## 4. CombatScene 集成

- [x] 4.1 修改 CombatScene.PreStart() — 加载卡牌时检查 .lua 是否存在，存在则走 Lua 初始化
- [x] 4.2 Lua 卡初始化流程：读 CardData/Keywords → 覆盖 CombatCard 属性
- [x] 4.3 Lua 卡 Hook 注册：扫描函数名 → 非 OnUse 的自动注册到 EventMgr
- [x] 4.4 修改 CombatCardFlowHandler — OnUse 路径：Lua 卡调用 Lua OnUse 而非遍历 Effects
- [x] 4.5 确保 Cleanup 时注销所有 Lua 注册的事件监听

## 5. 单卡验证（card_jin_whirlwind）

- [x] 5.1 编写 Data/LuaCards/card_jin_whirlwind.lua（OnUse + OnAttack）
- [x] 5.2 从 CardDefine JSON 中移除 EffectIds 字段（仅该卡，验证 Lua 路径）
- [x] 5.3 运行 CombatTestRunner，确认 Lua 卡牌产出与原 C# 路径一致（Attack 命中 + Charge 充能）
- [ ] 5.4 验证错误处理：故意写错 Lua → 确认不崩溃、有日志

## 6. 批量转化工具

- [ ] 6.1 编写 tools/convert_cards_to_lua.py — 读取现有 CardDefine + EffectDefine + ActionDefine → 生成 .lua 文件
- [ ] 6.2 转化所有现有卡牌，生成 Data/LuaCards/ 下对应文件
- [ ] 6.3 清理 CardDefine JSON 中的 EffectIds 字段
- [ ] 6.4 全量 CombatTestRunner 验证（所有卡走 Lua 路径）