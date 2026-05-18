## 1. ILuaBindable 扩展

- [x] 1.1 `ILuaBindable` 新增 `Dictionary<string, LuaFunction> LuaHooks { get; set; }` 属性
- [x] 1.2 `ILuaBindable` 新增 `ScanLuaHooks()` 方法（默认接口实现或扩展方法）：遍历 env.Keys，缓存所有 string→LuaFunction 映射到 LuaHooks；env 为 null 时初始化空字典
- [x] 1.3 `ILuaBindable` 新增 `HasHook(string hookName)` 方法：`LuaHooks.ContainsKey`
- [x] 1.4 `ILuaBindable` 新增 `CallLuaHook<bool>(string hookName, params object[] args)` 方法：UseLuaHooksCache 为 true 时查 LuaHooks 字典，false 时走老路径 env[hookName]
- [x] 1.5 `ILuaBindable` 新增 `CallLuaHookWithReturn<T>(string hookName, params object[] args)` 方法：同上但返回 Lua 返回值
- [x] 1.6 新增静态配置 `LuaBindableConfig.UseLuaHooksCache = true`（全局开关）

## 2. LuaMgr 集成扫描

- [x] 2.1 `LuaMgr.LoadModifierScript` 返回 env 前，调用 `ScanLuaHooks(env)` 扫描并缓存 LuaHooks 到 env 对应的 ILuaBindable 实例（或返回扫描结果供调用方直接赋值）
- [x] 2.2 `LuaMgr.LoadCardScript` 同上
- [x] 2.3 `CombatNpcModifier`：删除自身 `CallLuaHook` 实现，LuaHooks 在 Load 时已填充，创建时不再需要手动扫描
- [x] 2.4 `CombatCard`：同上，LuaHooks 在 Load 时已填充
- [x] 2.5 `NpcModifier`：同上（如有 Lua env）

## 3. 基类/接口扩展

- [x] 3.1 `GameEntityBase` 新增 `virtual float GetStat(string statId)` 方法，默认实现 `return Stats.Get(statId)`
- [x] 3.2 `ICombatEntity` 新增 `CombatScene Scene { get; set; }` 属性
- [x] 3.3 `CombatScene.Init` 中为所有战斗实体赋值 `Scene` 引用
- [x] 3.4 `CombatScene` 新增 `CollectModifierStat(object caller, string statId)` 方法：遍历全场 Modifier，对 `HasHook("OnModifierStat" + statId)` 为 true 的调 Lua hook，累加返回值

## 4. 战斗实体 GetStat override

- [x] 4.1 `CombatNpc` override `GetStat`：`base.GetStat(statId)` + `Scene.CollectModifierStat(this, statId)`
- [x] 4.2 `CombatCard` override `GetStat`：`base.GetStat(statId)` + `Owner.Scene.CollectModifierStat(this, statId)`
- [x] 4.3 战斗内现有直接调 `Stats.Get()` 的位置梳理，需要 Modifier hook 生效的改为 `GetStat()`

## 5. Lua 接口暴露

- [x] 5.1 确保 Lua 中 `entity.Stats:Get(statId)` 可调用（裸值，供 hook 内使用）
- [x] 5.2 确保 Lua 中 `entity:GetStat(statId)` 可调用（含 hook 贡献）
