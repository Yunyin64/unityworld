## ADDED Requirements

### Requirement: 五行元素 Buff JSON 定义
系统 SHALL 在 `Element_Buff.json` 中定义 10 条 CombatNpcModifierDefine，ID 与 `ElementType.BaseElementBuff` 字典一致：

| ID | DisplayName | Duration | MaxStack | ExpirePolicy |
|----|-------------|----------|----------|--------------|
| Element_Buff_Jin | 锐意 | -1 | 99 | StackBased |
| Element_Debuff_Jin | 出血 | -1 | 99 | StackBased |
| Element_Buff_Mu | 再生 | -1 | 99 | StackBased |
| Element_Debuff_Mu | 中毒 | -1 | 99 | StackBased |
| Element_Buff_Tu | 载德 | -1 | 99 | StackBased |
| Element_Debuff_Tu | 石化 | -1 | 99 | StackBased |
| Element_Buff_Shui | 浩瀚 | -1 | 99 | StackBased |
| Element_Debuff_Shui | 寒意 | -1 | 99 | StackBased |
| Element_Buff_Huo | 心火 | -1 | 99 | StackBased |
| Element_Debuff_Huo | 灼烧 | -1 | 99 | StackBased |

所有条目 RefreshOnStack=false，StatModifiers=[]。

#### Scenario: Define 加载成功
- **WHEN** GameDataMgr 加载 CombatModifierDefines 目录
- **THEN** CombatNpcModifierDefineMgr 包含以上 10 条 Define，可通过 Get(id) 获取

### Requirement: 五行元素 Buff Lua 脚本
系统 SHALL 为每个 Buff 创建对应 Lua 脚本，通过 `OnBaseManaDraw(ctx)` hook 触发效果。

| Buff | Lua 效果 |
|------|----------|
| 锐意 | AddElementBuff(Self, None, false, n) |
| 出血 | SelfDamage(Self, n) |
| 再生 | Heal(Self, n) |
| 中毒 | AddElementBuff(Self, None, true, n) |
| 载德 | RemoveElementBuff(Self, None, true, n) |
| 石化 | ManaConvert 扣 mp n |
| 浩瀚 | ManaConvert 回 mp n |
| 寒意 | Slow(Random, n) |
| 心火 | Haste(Random, n) |
| 灼烧 | RemoveElementBuff(Self, None, false, n) |

每个脚本 SHALL 检查 `ctx.Caster == self.m_Owner` 确保只响应自己的 ManaDraw。

#### Scenario: 锐意触发扩散
- **WHEN** 拥有 3 层锐意的 NPC 触发 OnBaseManaDraw
- **THEN** 调用 AddElementBuff 循环 3 次，每次随机给自己加 1 层正面五行 Buff

#### Scenario: 出血造成伤害
- **WHEN** 拥有 5 层出血的 NPC 触发 OnBaseManaDraw
- **THEN** 调用 SelfDamage 对自己造成 5 点伤害

#### Scenario: 载德清除负面
- **WHEN** 拥有 2 层载德的 NPC 触发 OnBaseManaDraw，身上有出血(3)+寒意(1)
- **THEN** 调用 RemoveElementBuff 循环 2 次，从 [出血, 寒意] 中随机各减 1 层
