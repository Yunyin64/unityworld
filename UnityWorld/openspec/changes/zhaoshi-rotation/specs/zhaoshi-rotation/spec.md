## ADDED Requirements

### Requirement: 招式串行轮转
CombatNpc SHALL 维护 `CurrentZhaoShiCardId` 字段，标识当前唯一允许走CD的招式卡。同一时间只有该卡的 ZhaoShi Keyword Tick 逻辑执行 Phase 流转，其余招式卡 MUST 停留在 Waiting。

#### Scenario: 正常轮转
- **WHEN** 当前招式卡完成 Apply
- **THEN** CurrentZhaoShiCardId 更新为 Field 中下一张 ZhaoShi 卡（按物理顺序），下一张卡开始从 Waiting 进入 InCD

#### Scenario: 末尾回绕
- **WHEN** 当前招式是招式列表中最后一张且完成 Apply
- **THEN** CurrentZhaoShiCardId 回绕到招式列表第一张

#### Scenario: 非当前招式不走CD
- **WHEN** 某张招式卡不是 CurrentZhaoShiCardId 对应的卡
- **THEN** 该卡在 Tick 中 MUST 不执行 TryPayMana 和 CD 流转，保持 Waiting Phase

### Requirement: 轮转顺序由 Field 物理顺序派生
招式列表 SHALL 通过实时过滤 Field 中所有带 ZhaoShi keyword 的卡、按 Field 中的物理索引排序得到。不单独存储 index。

#### Scenario: 位移影响轮转顺序
- **WHEN** 一张尚未轮到的招式卡被位移到当前卡之后（更靠后的位置）
- **THEN** 该卡在本轮循环中将被 advance 到（插队效果）

#### Scenario: 位移导致跳过
- **WHEN** 一张尚未轮到的招式卡被位移到当前卡之前（更靠前的位置）
- **THEN** 该卡在本轮循环中被跳过，需等下一轮才能轮到

#### Scenario: 当前卡自身被位移
- **WHEN** 正在走CD的招式卡被位移到其他位置
- **THEN** 该卡继续走CD不中断，advance 时从新位置向后找下一张

### Requirement: 当前卡被移除时 Fallback
当 CurrentZhaoShiCardId 对应的卡从 Field 中移除时，系统 SHALL 自动选择新的当前卡。

#### Scenario: 当前卡被移除且列表非空
- **WHEN** CurrentZhaoShiCardId 对应的卡被移除，且 Field 中仍有其他招式卡
- **THEN** CurrentZhaoShiCardId 更新为招式列表第一张卡的 Id

#### Scenario: 所有招式卡被移除
- **WHEN** Field 中无任何 ZhaoShi 卡
- **THEN** CurrentZhaoShiCardId 设为 -1，无招式输出

### Requirement: 初始化
CombatNpc 在 PreStart 阶段 InitDeck 完成后 SHALL 扫描 Field 中第一张 ZhaoShi 卡设为 CurrentZhaoShiCardId。

#### Scenario: 有招式卡时初始化
- **WHEN** InitDeck 完成且 Field 中存在至少一张 ZhaoShi 卡
- **THEN** CurrentZhaoShiCardId = 第一张 ZhaoShi 卡的 Id

#### Scenario: 无招式卡时初始化
- **WHEN** InitDeck 完成但 Field 中无 ZhaoShi 卡
- **THEN** CurrentZhaoShiCardId = -1

### Requirement: 冻结锁链
冻结当前招式卡时 SHALL 阻止整条招式链的输出——因为后续卡依赖当前卡完成才能 advance。

#### Scenario: 当前招式被冻结
- **WHEN** 当前招式卡的 CDSpeed 被设为冻结状态（CD 不推进）
- **THEN** 该卡 CD 不走，不会到达 CDFull/Ready，后续所有招式卡持续停留在 Waiting
