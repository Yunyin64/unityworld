# 实例位置与模板装配实施证据

对应任务 1.1—1.3、2.1—2.5、9.2；共享能力/兼容字段支持任务 1.4、3.1、3.3、8.1。

## 最终接口与来源边界

- `CardBaseData.Position` / `Card.Position` 为 `BackpackPosition?`，公开 X/Y 从 1 起始；`BackpackCoordinates.ToCell/ToPosition` 是唯一转换边界。`BackpackCell` 与 `Shape` 相对偏移保持内部 0 起始。
- `PlaceBackpackCard(id,x,y)` 接收公开坐标。专用旧几何测试通过明确命名的 `PlaceBackpackCardInternal` 输入 0 起始坐标，保持既有多行几何回归；正式模板不调用这个入口。
- `NpcCardData.Clone` 深复制卡的 BaseData 与 Stats。摆放先操作独立候选实例，按实例位置重建 `Placements`，完整校验后同时提交实例位置与空间索引；失败不会提前回写世界卡。索引与实例位置不符会拒绝。
- `CardDefine.GetShape()` 从正数 Size 自动派生横条。非横条手配 Shape 在正式内容中拒绝；仅 `UseCustomShapeForTests=true` 保留已有 `bp_charge` 竖条夹具。Size 0 不作运行时回退，必须迁移定义。
- 所有现有正式功法 JSON 批量增加 `BackpackWidth=5` / `BackpackHeight=5`。无显式等级布局时等级 1 使用中心公开 `(3,3)`；原 `BackpackLevels` 的不同尺寸、等级与几何测试不被覆盖。
- `NpcMgr.Assemble` 为模板入口。`BirthContext.AssemblySource` 显式区分 Template、Generated、Player，`Birth` 拒绝模板来源。`AssembleGenerated` 及原 `Birth` 保留生成角色/玩家的自动功法发牌；非模板原 `GainEquip` + `AssignAllToField` 附带招式路径保留。
- 模板只按 `InitCardPlacements` 每条创建一张 Card。`InitGongFa`、`InitEquip` 绑定这批已创建实例，不调用自动发牌入口。`PlacementIndex` 为可选的 **1 起始列表序号**，同定义多实例绑定必须指定；显式招式可用 `ParentPlacementIndex` 指向已有装备条目。
- 主功法由 `MainGongFaId` 唯一定位，不猜选修炼顺序。模板禁止混用非空 `InitCardDeck`。失败会注销全部新建 Card、GongFa、Equip、Card/Npc 子系统数据与 Stats，不能进入战斗。

最终配置示例（完整字段见 `Data/Npc/Test.json`）：

```json
{
  "MainGongFaId": "jin_fa",
  "InitCardPlacements": [
    { "Key": "jin_fa", "Value": { "X": 3, "Y": 3 } },
    { "Key": "card_jin_slash", "Value": { "X": 1, "Y": 1 } },
    { "Key": "card_fabao_jian", "Value": { "X": 3, "Y": 1 } }
  ],
  "InitGongFa": [ { "DefineId": "jin_fa", "CurrentPoint": 100, "PlacementIndex": 1 } ],
  "InitEquip": [ { "CardID": "card_fabao_jian", "EquipID": "long_sword", "PlacementIndex": 3 } ]
}
```

## 正式模板迁移

| 现有模板 | 显式上场内容 | 初始占格 / SP 上限 | 未采用的旧内容 |
| --- | --- | --- | --- |
| test_npc_a | jin_fa、斩击、绑定 long_sword 的长剑、木灵、爆燃、冻结、土缚 | 10 / 11 | 无自动生成长剑 FormList；充能使用现有爆燃，未猜测空费用金系充能的元素 |
| test_npc_b | huo_fa、两个独立 card_huo_shot、冻结、土缚 | 6 / 10 | 原灵炎心诀、霜心诀组合改为明确的现有火法主功法；不自动生成其解锁卡 |
| monster_wolf_0 | jin_fa、两个独立斩击、原狼牙/兽皮被动 | 5 / 8 | 狼肉与利爪行为未迁移，不进入验收模板；原定义与脚本保留 |

三个模板均为 5×5、中心单格主卡，留有完整横向两格伤口空间。已用脚本批量解析全 Card JSON 与 Npc JSON，确认模板所有引用存在，所有占格与 SP 数值合法。功法宽高更新同样以脚本批量处理原 JSON，没有创建替代正式内容体系。

## 验证入口

- `dotnet run --project Console/UnityWorld.Console.csproj -- --template-tests`：新增 `TemplateBackpackTests`，覆盖 1 起始边界、Size 3 在 `(2,4)`、越界 `(4,2)`、失败回滚、不同实例/同源战斗位置与点亮隔离、JSON 重复 key、装备绑定、非模板发牌、无隐式 FormList、错误模板清理、所有正式模板无需辅助布置创建战斗快照。
- `dotnet run --project Console/UnityWorld.Console.csproj -- --backpack-tests`：保留原不同尺寸/等级、几何、扫描、快照回归。`BackpackModelTests` 改为明确非模板夹具，不继承正式模板上场卡。
- 最终统一构建后 `--template-tests` **92 断言通过**，包含全部子系统注册数回滚及废弃需求防线。
- 正式演示第一次记录为 Tick 5000 平局，不作为成功证据：A 在 Tick 315、740 获得两个伤口后 SP=12/12，随后剩余资源不足以继续击败。模板 A 的神识由 12 调整到 11，使既有第二伤口满足败退条件；不修改伤口/SP 规则。刷新输出目录数据后实际验证：Tick 740，AllDefeated，TeamB 获胜。

静态核验：所属代码/JSON 的 git diff --check 通过（仅 Git 的 CRLF→LF 提示）；脚本成功解析 3 个 NPC 和 25 个正式功法，25 个功法的默认宽高均为 5。另补 2 条动态误标 Migrated 的废弃 Keyword/Min 费用拒绝测试，最终预期共 92 条模板断言，以最终复跑为准。
