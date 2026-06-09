## Why

CombatNpcModifier 系统（Define + Lua）适合有名字、有行为、有时限的重量级 Buff，但对于"HP上限+1"、"Def+2"这类纯数值、战斗内永久的简单属性修正来说太重了——每个属性修正都要新建一条 Define 配置不现实（属性几十种，排列组合无穷）。需要一个轻量级 API 让卡牌效果/Lua 脚本能直接对 CombatNpc 的 StatBlock 施加/撤销修正，无需经过 Define 体系。

## What Changes

- 在 `CombatNpc` 上新增 `AddStatBuff(statId, type, value, sourceId?)` 方法，直接向战斗 StatBlock 写入修正条目（永久存活，随战斗 Snapshot 结束自动清理）。
- 在 `CombatNpc` 上新增 `RemoveStatBuff(sourceId)` 方法，按来源标识精准移除所有相关修正。
- 不引入新的 Define、不引入新的 JSON 配置文件、不引入计时器——纯粹是 StatBlock 已有能力的薄封装。

## Capabilities

### New Capabilities
- `combat-stat-buff`: CombatNpc 轻量级属性修正 API——允许在战斗中不经 Define 直接施加/移除永久属性修正。

### Modified Capabilities
（无已有 spec 需要修改）

## Impact

- **代码**：`Scripts/Game/Domain/Combat/CombatNpc/` 下新增或修改 partial class 文件
- **Lua 绑定**：需确保 `AddStatBuff` / `RemoveStatBuff` 对 Lua 可见（CombatNpc 已实现 ILuaBindable 相关暴露）
- **依赖**：仅依赖已有的 `StatBlock.AddModifier` / `RemoveModifiersBySource`，无新外部依赖
