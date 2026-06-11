## Context

当前 NPC 创建只有一条路径：`NpcMgr.Birth(BirthContext)` — 走 GlyphMgr 命名 → 各 System.OnEntityBorn → 注册到世界。这适合有因果的人类 NPC。

妖兽是无因果的模板实体：
- 不需要 GlyphMgr 随机命名（名字=Define.DisplayName）
- 不需要性格/修行/行为/社交系统
- 只需要 Bio + Stat + Card 三件事即可战斗
- 用完即弃（战斗结束后可直接 Remove）

现有未消费字段：`NpcDefine.InitCardDeck`、`NpcDefine.InitStat` 正好为此设计。

已有可复用的卡牌数据：
- `card_monster_langya`（狼牙）— Item_Monster.json
- `card_monster_shoupi`（兽皮）— Item_Monster.json
- `card_monster_lizhao`（利爪）— Item_Monster.json（需改为 FaBao）

## Goals / Non-Goals

**Goals:**
- 从 NpcDefine 模板直接组装出可参与战斗的 Npc 实例
- 狼妖模板 ID=`monster_wolf_0`：八维全6，携带5张战斗卡
- 利爪是 FaBao 类型卡牌（装备），装备模板 ID=`monster_claw`
- Assemble 的 Npc 注册到 NpcMgr._allEntities（可被 CombatMgr 使用）
- Monster 也是 Npc，走同一套注册，不搞 RegisterMonster 分支
- InitialTraits 暂不消费（Trait 系统未就绪）

**Non-Goals:**
- 不改动 Birth 流程（两条路径并存）
- 不实现卡牌的具体 Lua 效果（第3步等用户一起做）
- 不实现掉落/遭遇系统（只配数据+组装方法）
- 不处理 InitialTraits

## Decisions

### D1：Assemble 方法签名

```csharp
public Npc Assemble(NpcDefine define)
```

不传 BirthContext。直接从 define 读取所有需要的数据。返回注册完毕的 Npc。

### D2：Assemble 内部流程

```
1. var npc = new Npc(Soul.NewId())
2. BioSystem 注册（走现有 Register，NpcBioData 直接从 define 填充）
3. Stats 写入 InitStat 的 base values
4. CardSystem 注册 + 遍历 InitCardDeck → GainCard
5. Add(npc.Id, npc) 注册到 _allEntities
6. return npc
```

不搞新的 RegisterMonster 方法。Bio 注册和人类一样走 `Register(npc, data)`, 只是 data 的内容不同（NpcType=Monster, Name 来自 define.DisplayName）。

### D3：狼妖属性配置

ID=`monster_wolf_0`。八维全6 → HpMax=6, SpMax=6, MpMax=18（QiGan×3）。五行亲和默认。

### D4：狼妖卡组设计

| ID | DisplayName | 类型 | 来源 | 说明 |
|---|---|---|---|---|
| card_monster_langya | 狼牙 | Item | 已有 Item_Monster.json | 刺击强化1 |
| card_monster_shoupi | 兽皮 | Item | 已有 Item_Monster.json | 斩击伤害抗性1 |
| card_monster_lizhao | 利爪 | FaBao | 改造 Item_Monster.json → FaBao | 攻2防2速3，金1，拼点时攻击+1 |
| card_form__strike | 抓击 | ZhaoShi | 新增 FormBase.json | CD=40, 刺击3 |
| card_wolf_meat | 兽肉 | Item | 新增 Item_Monster.json | 气血+1 |

### D5：利爪装备定义

装备模板 `Data/Equip/Equip_Monster.json`：
- ID=`monster_claw`, DisplayName=利爪
- 攻2, 防2, 速3
- FormListBase=[`card_form__strike`]

`card_monster_lizhao` 在 Item_Monster.json 中改 Keywords 为 `["FaBao"]`, ManaCost 加 `{"Jin":1}`。

### D6：NpcDefine.InitStat 消费方式

```csharp
foreach (var kv in define.InitStat)
    npc.Stats.SetBase(kv.Key, kv.Value);
```

直接写入 StatBlock 的 base 层。

## Risks / Trade-offs

- **[卡牌效果为空]** 本次只配 JSON 框架，Lua 效果不实现 → 卡牌在战斗中会触发但无实际效果，等第3步补
- **[五行亲和=0]** 因为是组装不走 CultivationSystem，ManaPool 可能为空 → 可通过 InitStat 写入 AffinityXxx 解决
- **[Trait 未消费]** InitialTraits 被跳过 → 后续 Trait 系统就绪后补上
