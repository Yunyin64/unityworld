## MODIFIED Requirements

### Requirement: 五行元素 Buff Lua 脚本
系统 SHALL 为每个 Buff 创建对应 Lua 脚本，通过 `OnBaseManaDraw(ctx)` hook 触发效果。每个脚本 SHALL 检查 `ctx.Caster == self.m_Owner`，只响应主人抽灵。

#### Scenario: 主人抽灵时分派对应效果
- **WHEN** 主人抽灵且状态当前为 n 层
- **THEN** 调用下表效果；心火与寒意作用于本方整包，每层持续 10 个真实 Tick，同类只加时，异类独立计时：

| Buff | Lua 效果 |
|------|----------|
| 锐意 | AddElementBuff(Self, None, false, n) |
| 出血 | SelfDamage(Self, n) |
| 再生 | Heal(Self, n) |
| 中毒 | AddElementBuff(Self, None, true, n) |
| 载德 | RemoveElementBuff(Self, None, true, n) |
| 石化 | ReduceMP(Self, n) |
| 浩瀚 | RecoverMP(Self, n) |
| 寒意 | Slow(Self, n * 10) |
| 心火 | Haste(Self, n * 10) |
| 灼烧 | RemoveElementBuff(Self, None, false, n) |


#### Scenario: 锐意触发扩散
- **WHEN** 拥有 3 层锐意的 NPC 触发 OnBaseManaDraw
- **THEN** 调用 AddElementBuff 循环 3 次，每次随机给自己加 1 层正面五行 Buff

#### Scenario: 出血造成伤害
- **WHEN** 拥有 5 层出血的 NPC 触发 OnBaseManaDraw
- **THEN** 调用 SelfDamage 对自己造成 5 点伤害

#### Scenario: 载德清除负面
- **WHEN** 拥有 2 层载德的 NPC 触发 OnBaseManaDraw，身上有出血(3)+寒意(1)
- **THEN** 调用 RemoveElementBuff 循环 2 次，从 [出血, 寒意] 中随机各减 1 层

#### Scenario: 浩瀚先补 MP 再放置灵元

- **WHEN** 主人有 2 层浩瀚、当前 MP 为 0，且基础补灵允许放置灵元
- **THEN** 先恢复至多 2 MP，再按可用 MP 与格位扣费放置灵元。其他角色的基础补灵事件不会使主人获得 MP
