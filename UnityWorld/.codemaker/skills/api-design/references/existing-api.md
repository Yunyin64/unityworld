# 现有 API 完整清单

> 最后更新：2025-05-20

## Contest 类（拼点）

| FuncName | 描述 | Scope | 参数签名 |
|----------|------|-------|----------|
| `Attack` | 造成伤害（攻击拼点） | CombatNpc | `Element:String, PhysicalType:String, AttackValue:Int` |
| `Shield` | 盾牌防御（赢了叠盾） | CombatNpc | `ShieldValue:Int` |
| `Block` | 格挡防御（赢了差值消失） | CombatNpc | `BlockValue:Int` |

**拼点参数说明：**
- `Element`：`"None"/"Jin"/"Mu"/"Shui"/"Huo"/"Tu"`
- `PhysicalType`：`"Zhan"/"Ci"/"Da"/"SheJi"`

---

## Action 类（效果）

### 治疗 / 自伤

| FuncName | 描述 | Scope | 参数签名 |
|----------|------|-------|----------|
| `Heal` | 恢复战斗中HP | CombatNpc | `HealValue:Int` |
| `SelfDamage` | 自伤 | CombatNpc | `DamageValue:Int` |

### 护盾 / 破甲

| FuncName | 描述 | Scope | 参数签名 |
|----------|------|-------|----------|
| `ArmorBreak` | 消除对方护盾值 | CombatNpc | `BreakValue:Int` |

### Buff / 属性

| FuncName | 描述 | Scope | 参数签名 |
|----------|------|-------|----------|
| `AddNpcBuff` | 给目标NPC添加Buff | Npc | `Target:CombatNpc, BuffId:String, Stacks:Int, [Duration:Float]` |
| `AddStatBuff` | 给施法者添加永久属性修正 | CombatNpc | `StatId:String, Value:Float, ?ModifierType:String, ?SourceId:String` |

### 灵元 / 资源

| FuncName | 描述 | Scope | 参数签名 |
|----------|------|-------|----------|
| `Convert` | 灵元转化回蓝条MP（1:1） | CombatNpc | `Element:String, MaxAmount:Int` |
| `Draw` | MP转化为灵元 | CombatNpc | `Amount:Int` |
| `ReduceMana` | 减少自身指定元素的灵元 | CombatNpc | `Element:String, Amount:Int` |

### CD / 速度

| FuncName | 描述 | Scope | 参数签名 |
|----------|------|-------|----------|
| `Charge` | 充能目标卡牌，减少CD | Card | `TargetCard:List<CombatCard>, ReduceTick:Int` |
| `Freeze` | 冻结目标卡牌 | Card | `TargetCard:CombatCard, FreezeTime:Float` |
| `Slow` | 减速目标卡牌 | Card | （空实现） |
| `Haste` | 加速目标卡牌 | — | （空实现） |

### 卡组操作

| FuncName | 描述 | Scope | 参数签名 |
|----------|------|-------|----------|
| `RemoveWound` | 移除目标某张伤势卡 | CombatNpc | `Target:CombatNpc, TargetCard:String` |
| `RemoveRandomWound` | 移除目标随机一张伤势卡 | CombatNpc | — |
| `Displace` | 位移目标卡牌 | CombatCard | `TargetCard:CombatCard, Position:String` |

---

## Condition 类（条件/选择器）

| FuncName | 描述 | Scope | 输入/输出 |
|----------|------|-------|-----------|
| `AllCard` | 获得目标所有卡牌 | CombatNpc | `Target:CombatNpc → Result:List<CombatCard>` |
| `RandomCardInCD` | 获得目标在CD中的随机一张卡 | CombatNpc | `Target:CombatNpc → Result:CombatCard` |
| `AdjacentCards` | 获得目标相邻卡牌 | CombatCard | `Target:CombatCard, Direction:string → Result:List<CombatCard>` |

---

## Score 参考（用于 ActionDefine.Score）

| 效果强度 | Score 范围 | 示例 |
|----------|-----------|------|
| 轻量辅助 | 1~2 | Charge(1), Draw(1) |
| 标准伤害/治疗 | 3~5 | Attack(3), Heal(3) |
| 强力效果 | 6~8 | Attack(8), 大量治疗 |
| 极端效果 | 9+ | 移除卡牌、大规模破甲 |

---

## 元素类型速查

```
None, Jin(金), Mu(木), Shui(水), Huo(火), Tu(土), Mix(混合)
```

## Scope 速查

```
Scope.Global    — 无特定归属
Scope.Npc       — NPC 相关（世界域）
Scope.CombatNpc — 战斗中的 NPC
Scope.CombatCard — 战斗中的卡牌
Scope.Card      — 通用卡牌
```
