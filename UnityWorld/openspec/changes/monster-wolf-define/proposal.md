## Why

战斗系统需要非人类敌人用于测试和遭遇。妖兽是"无因果"的模板实体，不走 Birth 流程（那是有因果的人类 NPC 用的），而是直接从 NpcDefine 组装出完整可战斗的 Npc 实例。当前 NpcDefines.json 为空，且 Birth 流程中未消费 InitCardDeck/InitStat 字段。

第一个妖兽模板：**狼妖**（凡人级），用于验证"Define → 组装 → 战斗"的完整链路。

## What Changes

- NpcMgr 新增 `Assemble(NpcDefine)` 方法：从 Define 直接组装 Npc，不走 Birth/因果
- NpcSystemBio 支持 Monster 类型直接注册（名字取 Define.DisplayName）
- NpcDefines.json 配置狼妖模板（八维全6，InitCardDeck 引用妖兽专属战斗卡）
- 新建 `Data/Card/Monster_Wolf.json`：狼妖战斗卡组（狼牙/兽皮/利爪/抓击/兽肉）
- 新建 `Data/Equip/Equip_Monster.json`：利爪装备定义（攻2防2速3）
- CombatTestRunner 中可用 Assemble 创建狼妖进行战斗测试

## Capabilities

### New Capabilities
- `npc-assemble`: NpcMgr.Assemble(NpcDefine) — 无因果组装 NPC 工厂方法
- `monster-wolf-cards`: 狼妖专属战斗卡牌数据（5张：狼牙/兽皮/利爪/抓击/兽肉）

### Modified Capabilities
- `npc-define`: NpcDefines.json 首次填充妖兽数据，InitCardDeck/InitStat 被 Assemble 消费

## Impact

- `Scripts/Game/Domain/Object/Npc/NpcMgr.cs` — 新增 Assemble 方法
- `Scripts/Game/Domain/Object/Npc/Systems/NpcSystemBio.cs` — 支持 Monster 注册
- `Scripts/Game/Domain/Object/Npc/Systems/NpcSystemCard.cs` — Assemble 时消费 InitCardDeck
- `Data/NpcDefines.json` — 狼妖模板数据
- `Data/Card/Monster_Wolf.json` — 狼妖战斗卡（新文件）
- `Data/Equip/Equip_Monster.json` — 利爪装备定义（新文件）
- `Scripts/Tests/CombatTestRunner.cs` — 新增妖兽战斗测试
