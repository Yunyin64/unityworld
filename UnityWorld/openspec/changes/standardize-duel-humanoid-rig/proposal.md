# Proposal

## Why

人形动作需要一套易于辨认和复制的骨架。现在的男女骨架使用相同的老命名，缺少胸、颈、锁骨和脚部控制。换装时衣身有重复袖片，前臂又总用默认图，导致肩肘错色。

## What Changes

- 用现有两个 Spine 导出作基础，采用 `Root`、`Hips`、`UpperArm_L` 等人形命名并补齐关节，保留现有动作、皮肤、插槽以及衣摆、披风、头发、武器骨骼。
- 整批重画可独立使用的衣身、大小臂、手、衣摆和腿脚衣物，按肩肘腕的接缝与前后遮挡装配。大臂与小臂各有自己的插槽。
- 提供含原有动画、皮肤和分件图片的 Spine 导入副本与骨骼对照。

## Capabilities

### New Capabilities

- `duel-humanoid-rig` — 人形骨架约定、动画复用与独立袖片装配。

### Modified Capabilities

## Impact

涉及 Spine 输出、模板预览、NPC 外观候选及 Mod 制作说明。现有 NPC 配置和战斗玩法输入继续可读。
