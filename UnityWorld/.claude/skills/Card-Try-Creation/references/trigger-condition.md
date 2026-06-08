# Trigger / Condition 速查 + 选择器模式

## 已有 Trigger

| ID | 说明 | 类型 |
|----|------|------|
| `trigger_on_use` | 使用时（CD 到了就触发） | 主动 |
| `trigger_on_attack` | 攻击时 | 主动 |
| `trigger_on_burn` | 敌人燃烧触发 | 被动 |
| `trigger_on_death` | 敌人死亡触发 | 被动 |
| `trigger_on_hit_body` | 击中本体（伤害打到HP上） | 被动 |
| `trigger_on_contest_win` | 拼点胜利 | 被动 |
| `trigger_on_contest_lose` | 拼点失败 | 被动 |
| `trigger_on_dominate` | 压制（同物理类型通吃成功） | 被动 |
| `trigger_on_dominated` | 被压制（同物理类型被通吃） | 被动 |

## 已有 Condition — 判定型（纯 bool 判断）

| ID | 说明 |
|----|------|
| `cond_none` | 无条件（空字符串 `""` 也等同无条件） |
| `cond_target_burn` | 目标有燃烧 |
| `cond_target_low_hp` | 目标HP低于30% |

## 已有 Condition — 选择器型（判定 + 选择目标写入 context）

| ID | 写入 context 的 key | 说明 |
|----|---------------------|------|
| `cond_card_self` | `TargetCardId` = 本卡自身 | 选自身卡牌 |
| `cond_card_above` | `TargetCardId` = 上方卡 | 选卡组中上方的卡 |
| `cond_card_all_self` | `TargetCardIds` = 己方全部卡 | 选己方所有卡 |
| `cond_random_enemy_card_in_cd` | `TargetCardId` = 敌方随机CD中卡 | 随机选敌方一张正在CD的卡 |
| `cond_target_self` | `Targetint` = 自身NPC | 选使用者NPC自己 |
| `cond_target_enemy` | `Targetint` = 敌方NPC | 选敌方目标NPC |

## Condition 选择器模式（核心设计）

**所有需要"选目标"的 API，目标都由 Condition 选择器提供，不由 API 自己处理。**

工作原理：
1. Effect 的 `conditionId` 指定一个选择器型 Condition
2. Condition 判定是否满足，同时将选择结果写入共享 context
3. Action 从 context 中读取目标（如 `TargetCardId`、`Targetint`）
4. Action 的 `params` 中目标字段填 `""`（占位），运行时被 context 覆盖

**示例：**
```
冻结卡: trigger_on_use + cond_random_enemy_card_in_cd → Freeze("", 1)
爆燃（全卡充能）: trigger_on_use + cond_card_all_self → Charge("", 1)
自燃: trigger_on_use + cond_target_self → AddNpcBuff("", "Burn", 1)
```

如果需要新 Trigger 或 Condition，和用户确认后新增到对应 JSON。