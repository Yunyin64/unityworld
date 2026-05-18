## ADDED Requirements

### Requirement: ILuaBindable 扩展——LuaHooks、ScanLuaHooks、CallLuaHook
`ILuaBindable` 接口 SHALL 扩展为包含以下成员（通过默认接口实现或扩展方法均可，推荐改为带默认实现的接口或提取基类 `LuaBindableBase`）：
- `Dictionary<string, LuaFunction> LuaHooks`：缓存 env 中所有函数引用
- `void ScanLuaHooks()`：遍历 env.Keys，将所有 `string → LuaFunction` 映射写入 LuaHooks
- `void CallLuaHook<bool>(string hookName, params object[] args)`：通过 LuaHooks 查找并调用 hook
- `bool HasHook(string hookName)`：通过 `LuaHooks.ContainsKey` 判定

所有实现 `ILuaBindable` 的类型（`CombatNpcModifier`、`NpcModifier`、`CombatCard` 等）的 LuaHooks SHALL 在 `LuaMgr` 加载脚本时（`LoadModifierScript`、`LoadCardScript`）完成扫描填充，不在各实现者的构造/创建方法中手动调用。

#### Scenario: CombatNpcModifier 创建时缓存所有 Lua 函数
- **WHEN** CombatNpcModifier 的 Lua env 定义了 `OnApply`、`OnTick`、`OnModifierStatAtk` 三个函数
- **THEN** `LuaHooks` 包含这三个 key，对应的 LuaFunction 引用不为 null

#### Scenario: CombatCard 创建时缓存所有 Lua 函数
- **WHEN** CombatCard 的 Lua env 定义了 `OnPreStart`、`OnUse` 两个函数
- **THEN** `LuaHooks` 包含这两个 key

#### Scenario: env 为 null 时 LuaHooks 为空字典
- **WHEN** 对象无 Lua 脚本（env 为 null）
- **THEN** `LuaHooks` 为空 Dictionary，不为 null

#### Scenario: 非函数类型的 env 值不缓存
- **WHEN** Lua env 中有 `CurrentStack = 1`（number 类型）
- **THEN** `LuaHooks` 不包含 `CurrentStack`

### Requirement: HasHook 快速判定
`ILuaBindable` 实现者 SHALL 通过 `HasHook(string hookName)` 方法判定是否存在指定 hook，实现为 `LuaHooks.ContainsKey(hookName)`。

#### Scenario: 存在的 hook 返回 true
- **WHEN** LuaHooks 包含 `OnTick`
- **THEN** `HasHook("OnTick")` 返回 true

#### Scenario: 不存在的 hook 返回 false
- **WHEN** LuaHooks 不包含 `OnRemove`
- **THEN** `HasHook("OnRemove")` 返回 false

### Requirement: UseLuaHooksCache 全局开关
系统 SHALL 提供静态 bool 字段 `LuaBindableConfig.UseLuaHooksCache`，默认 true。
- true：`CallLuaHook` 通过 `LuaHooks.TryGetValue` 获取函数引用
- false：`CallLuaHook` 通过老路径 `env[hookName] as LuaFunction` 获取函数引用

该开关对所有 `ILuaBindable` 实现者生效。

#### Scenario: 开关为 true 时走缓存路径
- **WHEN** `UseLuaHooksCache = true`，调用 `CallLuaHook<bool>("OnTick", ...)`
- **THEN** 通过 `LuaHooks.TryGetValue("OnTick")` 获取函数，不访问 env

#### Scenario: 开关为 false 时走老路径
- **WHEN** `UseLuaHooksCache = false`，调用 `CallLuaHook<bool>("OnTick", ...)`
- **THEN** 通过 `env["OnTick"] as LuaFunction` 获取函数

#### Scenario: 开关可运行时切换
- **WHEN** 运行中将 `UseLuaHooksCache` 从 true 改为 false
- **THEN** 后续 `CallLuaHook` 切换到老路径，行为不变
