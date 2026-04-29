## Why

当前系统用 `SocialRole.Cultivator` 枚举值来区分修士与凡人，这是一个静态的、不可变的标签，无法表达"凡人觉醒成修士"或"修士废修为变凡人"等动态变化。修为等级应当是一个可在游戏过程中变化的核心属性，而不是出生时固化的身份标签。

## What Changes

- **新增 `StatId.CultivationLevel`**：修为等级，整数值，`0` = 凡人，`> 0` = 修士，作为判断是否为修士的唯一依据
- **`NpcDefine` 新增初始修为范围字段** `InitCultivationLevelMin` / `InitCultivationLevelMax`，凡人模板默认均为 `0`，修士模板给定初始范围（如 1-5）
- **`NpcSystemName` 判断道号逻辑**改为读取 `CultivationLevel`，替换原来 `SocialRole == Cultivator` 的硬判断
- **移除 `NpcDefine` 中名字池相关字段**（`MaleNamePool` / `FemaleNamePool`，已由 `NpcSystemName` 接管，本次一并清理干净）
- `SocialRole.Cultivator` 枚举值**保留**，但不再作为修士判断依据（记录技术债：未来 system 化）

## Capabilities

### New Capabilities
- `cultivation-level`：修为等级属性，作为 NPC 的核心修炼状态，影响道号显示、寿元上限等下游逻辑

### Modified Capabilities
- `npc-define`：`NpcDefine` 新增初始修为范围字段，移除名字池字段，模板工厂方法同步更新
- `npc-name`：`NpcSystemName` 道号判断逻辑改为依赖 `CultivationLevel > 0`

## Impact

- `Scripts/Game/Domain/Stat/StatId.cs`：新增 `CultivationLevel`
- `Scripts/Game/Domain/Npc/NpcDefine.cs`：新增初始修为范围，移除名字池
- `Scripts/Game/Domain/Npc/NpcMgr.cs`：`Create()` 中初始化 `CultivationLevel`，修士判断逻辑更新
- `Scripts/Game/Domain/Npc/Systems/NpcSystemName.cs`：道号判断改为读 Stat
- `Program.cs`：无 breaking 变化，NPC 创建方式不变
