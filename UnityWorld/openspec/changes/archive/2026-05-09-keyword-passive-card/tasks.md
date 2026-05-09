## 1. 数据层：枚举与字段

- [x] 1.1 在 `Enum_Combat.cs` 的 `CombatCardPhase` 枚举中新增 `Passive` 值
- [x] 1.2 在 `CardDefine.cs` 中新增 `List<string> Keywords` 字段（JsonPropertyName "keywords"，默认空列表）
- [x] 1.3 在 `CardBaseData.cs` 中新增 `List<string> Keywords` 字段，并在 `Clone()` 方法中深拷贝该列表

## 2. LuaMgr：Keyword 注册表

- [x] 2.1 在 `LuaMgr` 中新增 `Dictionary<string, LuaTable> _keywordRegistry` 字段
- [x] 2.2 在 `LuaMgr` 中新增 `LoadKeywords()` 方法：加载 `Data/LuaScripts/Keywords/Keyword.lua` 索引文件，遍历返回的 table，逐个加载 keyword Lua 脚本并缓存到注册表
- [x] 2.3 在 `LuaMgr.Init()` 末尾调用 `LoadKeywords()`
- [x] 2.4 在 `LuaMgr` 中新增 `GetKeyword(string name)` 公开方法：查询注册表返回 LuaTable，未找到返回 null
- [x] 2.5 在 `LuaMgr.End()` 中清理 `_keywordRegistry`

## 3. CombatCard：Keyword Hook 遍历与 SetPhase

- [x] 3.1 在 `CombatCard` 中新增 `SetPhase(string phaseName)` 方法：解析字符串为 CombatCardPhase 枚举并设置 Phase，解析失败时输出错误日志
- [x] 3.2 在 `CombatCard` 中新增 `RunKeywordHooks(string hookName)` 私有方法：遍历 Keywords 列表，查 LuaMgr 注册表，调用对应 hook 函数（hook 不存在静默跳过，keyword 未注册报错）
- [x] 3.3 在 `CombatCard.PreStart()` 中 InitializeLuaCards 之后调用 `RunKeywordHooks("OnPreStart")`
- [x] 3.4 在 `CombatCard.Start()` 中调用 `RunKeywordHooks("OnStart")`
- [x] 3.5 修改 `CombatCard.Tick()`：开头调用 `RunKeywordHooks("OnTick")`，然后判断 `Phase == Passive` 时调用 `CallLuaHook("OnPassiveTick", ctx)` 并 return，跳过 CD 循环
- [x] 3.6 在 `CombatCard.End()` 中调用 `RunKeywordHooks("OnEnd")`

## 4. Lua 脚本

- [x] 4.1 创建 `Data/LuaScripts/Keywords/Keyword.lua` 索引文件，内容 `return { Passive = "Passive" }`
- [x] 4.2 创建 `Data/LuaScripts/Keywords/Passive.lua`，返回 table 包含 `OnPreStart` 函数，调用 `card:SetPhase("Passive")`
