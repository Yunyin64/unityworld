## 1. LuaMgr：Modifier 脚本加载

- [ ] 1.1 在 `LuaMgr` 中新增 `LoadModifierScript(string defineId)` 方法：按路径 `Data/LuaScripts/CombatModifiers/{defineId}.lua` 执行 DoFile，返回独立 LuaTable；文件不存在时返回 null 不报错
- [ ] 1.2 在 `LuaMgr` 构造函数中新增 `_luaCombatModifiersDir` 字段，指向 `Data/LuaScripts/CombatModifiers`

## 2. CombatNpcModifier：Lua Hook 调用

- [ ] 2.1 在 `CombatNpcModifier` 中新增 `CallLuaHook(string hookName, CombatNpc npc)` 方法：从 env 取 LuaFunction 调用 `(mod, npc)`，env 为 null 或 hook 不存在时静默跳过，异常时 catch 并输出错误日志

## 3. CombatNpcBuffFunc.cs：Buff 管理 partial

- [ ] 3.1 新建 `Scripts/Game/Domain/Combat/CombatNpc/CombatNpcBuffFunc.cs` 文件，声明为 `partial class CombatNpc`
- [ ] 3.2 实现增强版 `AddBuff(string defineId, int stacks = 1)`：查重叠层逻辑（CurrentStack/MaxStack/RefreshOnStack）+ LuaMgr.LoadModifierScript 加载 env + 调用 OnApply 或 OnStack hook
- [ ] 3.3 实现 `BuffTick()` 方法：遍历 Buffs 调用 OnTick，衰减有限时 Buff 的 RemainingTime，用 toRemove 临时列表收集过期 Buff，遍历结束后批量调用 OnRemove 并移除
- [ ] 3.4 实现 `RemoveBuff(string defineId)` 方法：查找匹配 Buff，调用 OnRemove 后移除，未找到时静默跳过

## 4. CombatNpc：接入 BuffTick

- [ ] 4.1 在 `CombatNpc.Tick()` 中加入 `BuffTick()` 调用

## 5. Lua 脚本目录

- [ ] 5.1 创建 `Data/LuaScripts/CombatModifiers/` 目录
