# 归档记录

- 日期：2026-10-27；schema：spec-driven；任务：41/41。
- 已同步全部八份增量：combat-backpack-scan、combat-backpack、combat-contest-scheduling、combat-mana-manipulation、combat-status-effects、keyword-system、npc-template-backpack、passive-card。
- keyword-system 的历史增量头修正为标准主规格结构，原三条需求及场景保留，再合入五条新需求。其他既有主规格保留标题、Purpose 和增量未涉及的需求/场景。
- 变更及八份主规格 strict 校验通过；逐项比较确认增量描述与场景均已同步。implementation.md、tasks.md 中“尚未同步/归档”是 2026-10-25 实施阶段的历史状态，以本记录补充其后续完成状态。
- 本次批量校验：15 份受影响主规格全部通过；全库另有 15 份无关旧主规格缺少 Purpose/Requirements 章节，未在本次修复。本次未重跑游戏运行测试，487 项断言等运行结果仍为原实施记录中的历史验证。
- 神修操作、玩家排包、生成 NPC 组牌、距离算法、旋转、ApplyHeal、告警清理和 Godot 验收的原有限制继续保留；zhaoshi-rotation 仍待重设计。
