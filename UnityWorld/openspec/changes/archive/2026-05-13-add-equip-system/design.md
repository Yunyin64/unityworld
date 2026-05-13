## Context

当前项目使用 `DefineBase` + `IDataMgrBase<T>` 模式管理静态配置数据。数据源有两种形态：
- **单文件模式**：如 `Traits.json`（TraitDefine）
- **文件夹模式**：如 `Data/Card/*.json`（CardDefine）、`Data/Stat/*.json`（StatDefine）

装备系统采用**文件夹模式**（`Data/Equip/*.json`），方便按装备类别分文件管理。

攻击/防御的物理类型、元素属性等具体效果属于招式卡层面的概念，由 FormList 引用的 CardDefine 自行决定，不在装备定义上声明。

EquipDefine 定位为**装备基本形态**的定义（如"剑"、"炉"、"刀"），只描述骨架数值，具体战斗效果走 Card 逻辑。

## Goals / Non-Goals

**Goals:**
- 建立 `EquipDefine` 静态数据定义，包含完整字段
- 建立 `EquipDefineMgr` 文件夹加载器，注册到 `GameDataMgr`
- 创建 `Data/Equip/` 目录及初始 JSON 模板
- 建立 `Equip` 运行时实例类（`Scripts/Game/Domain/Object/Equip/Equip.cs`），作为 Define 的实例化载体

**Non-Goals:**
- 穿戴系统（谁持有装备、装备槽管理）——后续变更
- 装备与战斗系统的集成——后续变更
- 装备掉落 / 生成 / 经济系统——后续变更

## Decisions

### D1: EquipDefine 字段设计

| C# 属性名 | JSON Key | 类型 | 默认值 | 说明 |
|---|---|---|---|---|
| ID | "ID" | string | "" | 继承自 DefineBase |
| DisplayName | "DisplayName" | string | "" | 继承自 DefineBase |
| Tags | "Tags" | List\<string\> | [] | 继承自 DefineBase |
| Size | "Size" | int | 1 | 对应卡牌 Size（短剑=1, 长剑=2...），表示此装备属性作用于哪个 Size 的卡牌 |
| AttackBase | "AttackBase" | int | 0 | 攻击基础值（非最终值，最终值由后续机制计算） |
| DefendBase | "DefendBase" | int | 0 | 防御基础值（非最终值，最终值由后续机制计算） |
| SpeedBase | "SpeedBase" | float | 0 | 速度基础值（非最终值） |
| AmountBase | "AmountBase" | int | 0 | 数量/耐久基础值（非最终值） |
| FormListBase | "FormListBase" | List\<string\> | [] | 附带招式卡 ID 列表基础值（引用 CardDefine，非最终值） |

**属性名统一 PascalCase 大写开头**，与 DefineBase 风格一致。

**Rationale:**
- EquipDefine 是"装备基本形态"定义，只存骨架数值；物理类型、元素等具体效果由 FormList 中的招式卡（CardDefine）承载
- Size 对应 CardDefine.Size：同一武器按 Size 拆分多条 EquipDefine（如 short_sword Size=1, long_sword Size=2, great_sword Size=3）
- AttackBase / DefendBase 是基础值而非最终值：最终伤害/防御由后续运行时机制计算，Define 只存基础数据
- SpeedBase / AmountBase / FormListBase 同理，均为基础值：最终速度/数量/招式列表由运行时机制决定，Define 只存基础数据
- AmountBase 默认 1：语义灵活，可表示耐久/数量/使用次数
- FormListBase 引用 CardDefine ID：复用现有卡牌体系，装备可携带招式卡

### D2: EquipDefineMgr 文件夹加载模式

复制 `CardDefineMgr` 的文件夹遍历模式：扫描 `Data/Equip/*.json`，每个文件是 `List<EquipDefine>`，合并加载、去重。

### D3: GameDataMgr 注册

在 `GameDataMgr` 构造函数中添加一行：
```csharp
_datamgrs.Add(new EquipDefineMgr(Path.Combine(dataDir, "Equip")));
```

### D4: Equip 运行时实例类

位于 `Scripts/Game/Domain/Object/Equip/Equip.cs`，实现 `IFormDefine<EquipDefine>`（参照 GongFa）。

**Equip 是 EquipDefine 的实例化载体**：Define 只是模板（Base 值），Equip 才是最终生效的运行时对象。

| C# 属性名 | 类型 | 初始值来源 | 说明 |
|---|---|---|---|
| DefineId | string | 构造时传入 | 实现 IFormDefine，关联 EquipDefine |
| DisplayName | string | 构造时传入 | 实现 IFormDefine |
| Size | int | EquipDefine.Size | 作用于哪个 Size 的卡牌 |
| Attack | int | EquipDefine.AttackBase | 攻击值（最终生效值，可被修改器叠加） |
| Defend | int | EquipDefine.DefendBase | 防御值（最终生效值） |
| Speed | float | EquipDefine.SpeedBase | 速度（最终生效值） |
| Amount | int | EquipDefine.AmountBase | 数量/耐久（最终生效值） |
| FormList | List\<string\> | EquipDefine.FormListBase | 招式卡列表（最终生效值） |

**Rationale:**
- Define 字段带 Base 后缀（模板值），Equip 实例字段去掉 Base（运行时最终值）
- 初始值从 Define 复制而来，后续可被 Modifier、Trait 等机制修改
- 参照 GongFa 模式实现 `IFormDefine<EquipDefine>`，保持 Object 层风格统一

## Risks / Trade-offs

- [Risk] JSON Key 大写开头（如 `"AttackBase"`）与 CardDefine 的小写风格（如 `"rarity"`）不完全统一 → 可接受，EquipDefine 保持与 DefineBase 自身字段风格一致，后续可通过 `JsonPropertyName` 灵活调整
- [Risk] FormList 引用的 CardDefine ID 可能不存在 → 加载时暂不校验，后续可在运行时穿戴时做校验

## Open Questions

- 是否需要装备类型/槽位字段（如 Weapon / Armor / Accessory）？——暂不加，后续按需扩展
- 是否需要稀有度字段？——暂不加
- 是否需要描述文本字段？——暂不加
