## 1. APIMgr 签名修复

- [x] 1.1 修改 APIMgr.ScanHandlers() 签名校验：支持返回 APIContext 和参数类型 APIContext/ContextBase

## 2. LuaMgr 改造

- [x] 2.1 LuaMgr.Init() 中加载 Data/LuaScripts/Init.lua
- [x] 2.2 LuaMgr.LoadCardScript() 去掉缓存，每次执行脚本并捕获 return 值返回
- [x] 2.3 移除 _cardEnvironments 字典及相关方法（GetCardEnvironment、UnloadCardScript 等可简化）

## 3. Init.lua 编写

- [x] 3.1 创建 Data/LuaScripts/Init.lua，定义 CardBase 元表
- [x] 3.2 实现 Attack/Shield/Block/Heal/SelfDamage 包装函数（ctx:Set + APIMgr:Execute）

## 4. CombatCard Lua 调用

- [x] 4.1 CombatCardFunc.InitializeLuaCards() 简化：加载脚本 → env = return 值
- [x] 4.2 CombatCard 添加通用 CallLuaHook 辅助方法（取函数 + Call(env, ctx) + try-catch）
- [x] 4.3 CombatCard.OnContest() 构造 APIContext 并调用 Lua
- [x] 4.4 CombatCard.OnApply() 构造 APIContext 并调用 Lua

## 5. 卡牌脚本补全

- [x] 5.1 更新 Data/LuaCards/card_form_quan_da.lua 的 OnContest 填入 Attack(ctx, "Wu", "Da", 2)
- [x] 5.2 新建 Data/LuaCards/card_form_ci_quan.lua — 刺拳（Ci 类型攻击）
- [x] 5.3 新建 Data/LuaCards/card_form_jian_qi.lua — 剑气（SheJi 类型攻击，3点武器射伤）