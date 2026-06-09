## 1. Phase 0：NpcMgr.Assemble 组装方法

- [x] 1.1 在 `NpcMgr.cs` 新增 `public Npc Assemble(NpcDefine define)` 方法
- [x] 1.2 Assemble 内部：new Npc → BioSystem.Register(npc, bioData) → Stats.SetBase → CardSystem.Register + GainCard → Add
- [x] 1.3 BioData 填充：NpcType=define.NpcType, NameData.Surname=define.DisplayName, IsAlive=true（走现有 Register，不加新方法）
- [x] 1.4 遍历 `define.InitStat` 调用 `npc.Stats.SetBase(key, value)` 写入八维
- [x] 1.5 CardSystem.Register(npc, new NpcCardData()) + 遍历 `define.InitCardDeck` 调用 `GainCard(cardDefineId)`
- [x] 1.6 末尾 `Add(npc.Id, npc)` + `npc.AssignAllToField()` + return npc

## 2. Phase 1：狼妖 NpcDefine 数据

- [x] 2.1 在 `Data/NpcDefines.json` 添加狼妖模板：ID=monster_wolf_0, DisplayName=灰狼, NpcType=Monster, InitAge=5, InitStat 八维全6, InitCardDeck=["card_monster_langya","card_monster_shoupi","card_monster_lizhao","card_form__strike","card_wolf_meat"]

## 3. Phase 2：卡牌数据补全

- [x] 3.1 修改 `Data/Card/Item_Monster.json` 中 card_monster_lizhao：Keywords 改为 ["FaBao"], 添加 ManaCost={"Jin":1}, Cooldown=50
- [x] 3.2 在 `Data/Card/FormBase.json` 末尾新增 card_form__strike（抓击）：ZhaoShi, Size=1, CD=40, Tags=["武器","攻击","刺","爪"]
- [x] 3.3 在 `Data/Card/Item_Monster.json` 末尾新增 card_wolf_meat（兽肉）：Passive+Item, Size=1, CD=0, Tags=["怪物素材","狼"]
- [x] 3.4 新建 `Data/Equip/Equip_Monster.json`：monster_claw 利爪，攻2防2速3, FormListBase=["card_form__strike"]

## 4. Phase 3：测试验证（暂停点）

- [ ] 4.1 在 CombatTestRunner 中用 `NpcDefineMgr.Instance.Get("monster_wolf_0")` 获取定义
- [ ] 4.2 调用 `NpcMgr.Instance.Assemble(wolfDefine)` 创建狼妖实例
- [ ] 4.3 验证狼妖的 Stats（八维=6, HpMax=6, SpMax=6, MpMax=18）和 CardData 有5张卡
- [ ] 4.4 `CombatMgr.Instance.RunCombat(playerNpc, wolfNpc)` 可跑通（卡牌效果为空但不崩）
- [ ] 4.5 **暂停** — 等用户一起配卡牌 Lua 效果和调试
