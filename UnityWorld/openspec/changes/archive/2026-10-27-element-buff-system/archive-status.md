# 归档记录

- 日期：2026-10-27；schema：spec-driven；任务：13/13。
- 已同步 element-buff-api、element-buff-defines，并补齐新规格用途说明。
- 两条过长需求的定义表与效果表移入对应场景，保留原需求及原有场景；变更及两份主规格 strict 校验通过。
- 按现有 Lua 和已完成背包迁移修正历史效果名：石化 ReduceMP、浩瀚 RecoverMP；寒意 Slow(Self,n*10)、心火 Haste(Self,n*10)，均作用于本方整包，每层 10 个真实 Tick，同类加时。依据为 Data/Modifier/CombatModifierDefines/Lua 下对应脚本及背包迁移 evidence-effects.md。
- 再生仍调用现有 Heal，归档不代表修复或验收了 ApplyHeal；历史实施完成与后续已记录限制分别保留。
- 本次批量校验：15 份受影响主规格全部通过；全库另有 15 份无关旧主规格缺少 Purpose/Requirements 章节，未在本次修复。本次未重跑游戏运行测试。
