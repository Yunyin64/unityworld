## MODIFIED Requirements

### Requirement: ZhaoShi Keyword Tick Hook
ZhaoShi Keyword 的 Tick hook SHALL 在执行 Phase 流转前检查当前卡是否为 owner 的 CurrentZhaoShiCardId。若不是，MUST 跳过所有流转逻辑，保持 Waiting。

#### Scenario: 当前卡正常走 Tick
- **WHEN** card 的 Id == owner:GetCurrentZhaoShiCardId()
- **THEN** 执行原有逻辑：Waiting→InCD（无 mana 消耗）→CDFull→SetReady

#### Scenario: 非当前卡 Tick 静默
- **WHEN** card 的 Id != owner:GetCurrentZhaoShiCardId()
- **THEN** Tick hook 立即返回，不执行任何 Phase 变更

### Requirement: ZhaoShi Keyword Apply Hook
ZhaoShi Keyword 的 Apply hook SHALL 在标记 Finished 后调用 owner 的 AdvanceZhaoShi() 推进轮转。

#### Scenario: Apply 后触发轮转
- **WHEN** 招式卡 Apply 完成
- **THEN** 调用 owner:AdvanceZhaoShi()，CurrentZhaoShiCardId 切换到下一张招式卡
