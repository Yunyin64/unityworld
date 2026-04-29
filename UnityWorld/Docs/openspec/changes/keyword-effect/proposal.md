## Why

战斗系统的 Effect 体系目前只支持 TCA（Trigger-Condition-Action）数据驱动的效果描述。但有一类常见的卡牌机制——如「初始」「消耗」「武器」——无法用 TCA 组合表达，它们改变的是卡牌在战斗系统中的**存在方式和生命周期**，需要引擎硬编码支持。

当前这些机制散落在"机制原子清单"中，缺乏统一的设计身份和数据结构。需要将它们正式定义为 **Keyword Effect**——一种特殊的 Effect，与普通 TCA Effect 共存于 Effects 列表中，共享 Score/Tag 体系，但走专用代码路径。

## What Changes

- 在 Effect 层引入 `IsKeyword` 概念，区分 TCA Effect 和 Keyword Effect 两种模式
- Keyword Effect 拥有 KeywordId + KeywordParams，由引擎硬编码识别和执行
- Keyword Effect 保留 Score 和 Tags，参与卡牌随机生成的分数预算系统
- 定义首批 Keyword 清单：
  - **初始 (Initial)**：开战 CD=0，立刻尝试触发
  - **消耗X (Consume)**：用 X 次后移除，释放 Size
  - **弹药X (Ammo)**：用 X 次后停转，可被装填恢复
  - **武器 (Weapon)**：结算前从卡组上方的武器卡读取属性，补全 Action Context 中的空位
  - **锁位 (Anchored)**：不能被位移效果移动位置
  - **速攻 (Rush)**：CD 到了不进待发槽，直接作为直击结算
  - **坚守 (Fortify)**：待发槽中不会被挤出
  - **迟缓 (Sluggish)**：首次 CD 翻倍
  - **超载 (Overcharge)**：可额外消耗灵元提升拼点数值
- 更新机制原子清单文档，将已有的「初始」「消耗」「弹药」重新归类为 Keyword Effect
- 卡面 UI 层：IsKeyword=true 的 Effect 用关键词格式渲染（标签词 + 简短说明）

## Capabilities

### New Capabilities
- `keyword-effect`: Keyword Effect 的设计定义——数据结构、与 TCA Effect 的关系、在结算管线中的介入时机、首批 Keyword 清单及各自的行为规则

### Modified Capabilities

（无已有 spec 需要修改）

## Impact

- 战斗/战斗_TCA体系.txt — 卡牌模型章节需补充 Effect 的 IsKeyword 分支
- 战斗/战斗_机制原子清单.txt — 「初始」「消耗」「弹药」从独立机制原子重新归类为 Keyword Effect
- 战斗/战斗_卡牌生命周期.txt — 生命周期流程需补充 Keyword 介入节点
- 卡牌随机生成系统 — Keyword Effect 作为 Effect 池的一部分参与分数预算