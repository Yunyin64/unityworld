## MODIFIED Requirements

### Requirement: CardDefine 效果描述结构
CardDefine SHALL 持有 `ActionPipelines`（List\<ActionPipeline\>）和 `AuraPipelines`（List\<AuraPipeline\>）字段，替代原有通过 EffectDefine 引用效果的方式。原有字段保留但标记废弃。

#### Scenario: 新格式 CardDefine JSON
- **WHEN** CardDefine JSON 包含 `"actionPipelines": [...]` 和 `"auraPipelines": [...]`
- **THEN** 反序列化后 CardDefine.ActionPipelines 和 CardDefine.AuraPipelines 正确填充

#### Scenario: 旧格式兼容
- **WHEN** CardDefine JSON 不包含管线字段（仅有 keywords 等旧字段）
- **THEN** ActionPipelines 和 AuraPipelines 为空列表，卡牌仍通过旧 Keyword/Lua 机制工作

#### Scenario: 新旧并存
- **WHEN** CardDefine 同时有 keywords 和 actionPipelines
- **THEN** 两者均生效，keyword hooks 在管线之外独立运行
