## 1. LuaMgr：Modifier 脚本加载

- [x] 1.1 在 `LuaMgr` 中新增 `_luaCombatModifiersDir` 字段，指向 `Data/LuaScripts/CombatModifiers`
- [x] 1.2 在 `LuaMgr` 中新增 `LoadModifierScript(string defineId)` 方法：按路径 `{_luaCombatModifiersDir}/{defineId}.lua` 执行 DoFile，返回独立 LuaTable；文件不存在时返回 null 不报错

## 2. CombatNpcModifier：Lua Hook 调用

- [x] 2.1 在 `CombatNpcModifier` 中新增 `CallLuaHook<bool>(string hookName, CombatNpc npc)` 方法：从 env 取 LuaFunction 调用 `(mod, npc)`，env 为 null 或 hook 不存在时静默跳过，异常时 catch 并输出错误日志

## 3. CombatNpcModifierFunc.cs：Modifier 管理 partial

- [x] 3.1 新建 `Scripts/Game/Domain/Combat/CombatNpc/CombatNpcModifierFunc.cs` 文件，声明为 `partial class CombatNpc`
- [x] 3.2 实现 `AddModifier(string defineId, int stacks = 1)`：查重叠层逻辑（CurrentStack/MaxStack/RefreshOnStack）+ LuaMgr.LoadModifierScript 加载 env + 调用 OnApply 或 OnStack hook
- [x] 3.3 实现 `ModifierTick()` 方法：遍历 Modifiers 调用 OnTick，衰减有限时 Modifier 的 RemainingTime，用 toRemove 临时列表收集过期 Modifier，遍历结束后批量调用 OnRemove 并移除
- [x] 3.4 实现 `RemoveModifier(string defineId)` 方法：查找匹配 Modifier，调用 OnRemove 后移除，未找到时静默跳过

## 4. CombatNpc：重命名与接入

- [x] 4.1 将 `CombatNpc.Buffs` 属性重命名为 `Modifiers`，同步更新所有引用
- [x] 4.2 在 `CombatNpc.Tick()` 中加入 `ModifierTick()` 调用
- [x] 4.3 移除 `CombatNpcFunc.cs` 中旧的 `AddBuff` 方法（逻辑已迁移到 CombatNpcModifierFunc）

## 5. Lua 脚本目录

- [x] 5.1 创建 `Data/LuaScripts/CombatModifiers/` 目录
