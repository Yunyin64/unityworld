## 1. C# API 实现

- [x] 1.1 在 `CombatManaAction.cs` 中实现 `AddElementBuff` API（Domain, Element, IsDebuff, Count）
- [x] 1.2 在 `CombatManaAction.cs` 中实现 `RemoveElementBuff` API（Domain, Element, IsDebuff, Count）

## 2. JSON 数据定义

- [x] 2.1 在 `Element_Buff.json` 中写入 10 条 CombatNpcModifierDefine（ExpirePolicy=StackBased）

## 3. Lua 脚本实现

- [x] 3.1 创建 `Element_Buff_Jin.lua`（锐意：AddElementBuff 随机正面 n 次）
- [x] 3.2 创建 `Element_Debuff_Jin.lua`（出血：SelfDamage n）
- [x] 3.3 创建 `Element_Buff_Mu.lua`（再生：Heal n）
- [x] 3.4 创建 `Element_Debuff_Mu.lua`（中毒：AddElementBuff 随机负面 n 次）
- [x] 3.5 创建 `Element_Buff_Tu.lua`（载德：RemoveElementBuff 随机负面 n 次）
- [x] 3.6 创建 `Element_Debuff_Tu.lua`（石化：ManaConvert 扣 mp n）
- [x] 3.7 创建 `Element_Buff_Shui.lua`（浩瀚：ManaConvert 回 mp n）
- [x] 3.8 创建 `Element_Debuff_Shui.lua`（寒意：Slow Random n）
- [x] 3.9 创建 `Element_Buff_Huo.lua`（心火：Haste Random n）
- [x] 3.10 创建 `Element_Debuff_Huo.lua`（灼烧：RemoveElementBuff 随机正面 n 次）
