## MODIFIED Requirements

### Requirement: Passive 卡效果表达
Passive 卡的被动持续效果 SHALL 可通过 AuraPipeline 描述。Passive keyword 仍负责将卡设为 Passive phase（不参与 CD 循环），但效果本身由 AuraPipeline 驱动。

#### Scenario: Passive 卡配置 AuraPipeline
- **WHEN** 一张 Keywords 包含 "Passive" 的 CardDefine 配置了 AuraPipelines
- **THEN** 卡牌进入 Passive phase 后，AuraPipeline 持续生效

#### Scenario: Passive 卡无 AuraPipeline（纯 Lua）
- **WHEN** 一张 Passive 卡未配置 AuraPipelines（仅有 Lua keyword hook）
- **THEN** 行为与改动前完全一致，keyword hook 的 OnPassiveTick 正常运行

#### Scenario: Passive 卡同时有 ActionPipeline
- **WHEN** 一张 Passive 卡配置了 ActionPipelines
- **THEN** ActionPipeline 仍可被事件触发（Passive 不阻止事件响应，只阻止 CD 循环）
