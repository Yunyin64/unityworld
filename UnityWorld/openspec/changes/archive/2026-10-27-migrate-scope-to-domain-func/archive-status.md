# 归档记录

- 日期：2026-10-27；schema：spec-driven；任务：25/25。
- 已将 card-target-domain 同步到主规格，并通过变更及主规格 strict 校验。
- 归档前按任务清单和当前 APIDomainFunc 修正文档中的 Left/Right 旧命名为 Above/Below。按已完成的背包迁移及当前实现，将 Random 更新为稳定排序的场上卡候选，AboveAll 更新为二维完整上方，Charge 更新为角色整包基准进度。其余线性选择器明确保留兼容语义。
- 依据：Scripts/Game/Domain/!Global/API/APIDomainFunc.cs、Combat/Action/CombatCDAction.cs，以及同批归档的 migrate-backpack-combat-mechanics/evidence-effects.md。没有修改游戏代码或新增玩法。
- 历史 tasks.md 保留当时的实施记录，旧接口调用示例以本次修正后的规格为准。
- 本次批量校验：15 份受影响主规格全部通过；全库另有 15 份无关旧主规格缺少 Purpose/Requirements 章节，未在本次修复。本次未重跑游戏运行测试。
