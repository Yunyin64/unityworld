## 1. C# 基础设施

- [ ] 1.1 CombatNpc 新增 `CurrentZhaoShiCardId` 属性（int, 默认 -1）
- [ ] 1.2 CombatNpc 新增 `GetZhaoShiList()` 方法：过滤 Field 中带 ZhaoShi keyword 的卡，按 Field 顺序返回
- [ ] 1.3 CombatNpc 新增 `AdvanceZhaoShi()` 方法：从当前卡位置 +1 mod N 取下一张，更新 CurrentZhaoShiCardId；列表为空时设 -1
- [ ] 1.4 CombatNpc 新增 `InitZhaoShiRotation()` 方法：取招式列表首张卡 Id 设为 CurrentZhaoShiCardId，在 PreStart 的 InitDeck 之后调用
- [ ] 1.5 CombatNpc 新增 Fallback 逻辑：在 DealFieldChange 处理完队列后，如果 CurrentZhaoShiCardId 对应卡不在 Field 中，重置为列表首张或 -1

## 2. Lua Keyword 重写

- [ ] 2.1 重写 `Data/LuaScripts/Keywords/ZhaoShi.lua` 的 Tick hook：检查 card.Id == owner:GetCurrentZhaoShiCardId()，不匹配则 return；匹配则走 Waiting→InCD→CDFull→SetReady（无 mana 消耗）
- [ ] 2.2 重写 `Data/LuaScripts/Keywords/ZhaoShi.lua` 的 Apply hook：SetReady(false)、SetPhase(Finished)、调用 owner:AdvanceZhaoShi()

## 3. 验证

- [ ] 3.1 运行 CombatTestRunner，确认招式卡串行出牌（日志中同一时刻只有一张招式走CD/Use）
- [ ] 3.2 测试位移场景：位移一张招式卡到当前卡之前，确认本轮被跳过
- [ ] 3.3 测试移除场景：移除当前正在走CD的招式卡，确认 fallback 到列表首张
