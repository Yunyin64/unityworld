# landmark-system

## Purpose

本规范定义 landmark-system 能力对世界与调用方提供的行为、数据一致性和可验证场景。以下要求保留既有地图与元气系统约定，后续行为修改通过对应变更的增量规格进行维护。

## Requirements

### Requirement: LandMarkId 唯一标识

系统 SHALL 为每个有效运行时地标提供唯一实例标识，支持相等比较和字典索引，并与地标定义 ID 区分。实例标识 SHALL 保持现有整数表示的兼容性；稳定地图摘要使用定义及位置等结构信息，不依赖随机实例标识。

#### Scenario: LandMarkId 相等性
- **WHEN** 两个地标实例标识具有相同整数值
- **THEN** 相等比较成立且字典键定位一致，不要求新增包装类型

### Requirement: LandMarkDefine 静态配置

地标定义 SHALL 持有稳定 ID、名称、标签、自然／人工、全局唯一性、图片、修正引用、五行偏移、`DrawType` 与 `IsHide`。地标 SHALL 用通用 C# 类型 `InitialPlacement` 集中定义固定坐标、`PerTileChance`、`MinCount`、`MaxCount`、`NeedCount`、`MinSpacing` 和四类条件。地标格式 SHALL 移除 `SchemaVersion`、`SpawnWeight` 以及被四类条件替代的旧地形、标签及地区拓展筛选字段。五行偏移仍默认全零，按放置阶段生效。

#### Scenario: 自然奇观配置
- **WHEN** 自然地标配置了非零 `AuraOffset`
- **THEN** 地图生成时该偏移在自然原初快照前一次性计入地块初值
- **AND** 修正源的每秒速率不被直接加进初始快照

#### Scenario: 生成条件过滤
- **WHEN** 某地标的 `TerrainConditions` 只允许 `mountain`
- **THEN** 其普通独立候选只包含类型 ID 为 `mountain` 的格子

#### Scenario: 类型标签辅助散布
- **WHEN** 某地标 `TagConditions` 要求山类标签且未限制具体类型 ID
- **THEN** 所有携带该标签的同级类型均可参与其他条件的检查

#### Scenario: 旧修正值不隐式转初值
- **WHEN** 某自然地标仅引用一个火元气速率修正，未配置 `AuraOffset`
- **THEN** `AuraOffset` 仍为零，该修正依旧按每秒速率处理

#### Scenario: 严格加载新地标定义
- **WHEN** 加载仅含现行字段的定义，或地标定义含已删除的地标字段
- **THEN** 合法的现行结构可以加载，已删除字段由严格校验拒绝
- **AND** 地块、地区各自的版本字段和地区散布权重仍按其规范校验

### Requirement: LandMark 运行时实体

运行时地标 SHALL 记录整数实例标识、定义 ID、地块坐标、位面和有效状态。成功放置同时建立双向关联。区域模板可让同一 Tile 关联多个地标，并保持主地标的兼容访问。移除地标只解除该实例的关联，原主地标移除后依序显示下一项。

#### Scenario: LandMark 生成后关联地块
- **WHEN** 地标在某位面的某地块成功生成
- **THEN** 地标位置和位面与该地块一致，地块关联到该地标的实例标识

#### Scenario: 跨位面相同坐标不混淆
- **WHEN** 两个位面具有相同地块坐标，地标放在其中一个位面
- **THEN** 只有目标位面的地块持有此地标关联

### Requirement: LandMarkMgr 管理所有地标实例

系统 SHALL 支持放置、移除、按实例或位面查询及自然地标筛选。放置前校验定义、修正、占位和全局唯一性，失败不留副作用。移除只清理该实例的关联与修正。区域模板内成组地标随地区整体提交，普通创世补位和保底均只使用未占用的合法基础填充格。

#### Scenario: 生成地标时 Modifier 挂载
- **WHEN** 地标成功生成且引用了若干有效修正定义
- **THEN** 每项修正独立实例化并挂到目标地块，来源标识能追踪到该地标

#### Scenario: 移除地标时清理 Modifier
- **WHEN** 移除一个已生成地标
- **THEN** 该地块上由此地标挂载的修正源和实例关联全部移除，其他来源及同格地标保留

#### Scenario: Singleton 地标全图唯一
- **WHEN** 某定义声明全局唯一，已存在一个有效实例后再次尝试生成
- **THEN** 第二次放置失败，不创建实例或修正源；地区模板中的此失败使整个地区候选失败

#### Scenario: 已占地块拒绝第二地标
- **WHEN** 目标地块已有有效地标并尝试经普通运行时接口独立放置
- **THEN** 新地标放置失败，原关联、当前元气和修正源保持不变

#### Scenario: 缺失修正引用不能部分挂载
- **WHEN** 地标引用的多个修正定义中任意一个不存在
- **THEN** 放置失败，不先挂载其余修正，也不生成缺失修正的地标实例

#### Scenario: 固定地标忽略散布过滤
- **WHEN** 地区模板显式配置的地标不满足该定义的普通独立选址条件，但地区与地标的其他完整性校验全部通过
- **THEN** 该地标随地区在固定位置生成并计入首批总量

#### Scenario: 地区内无地标格不参与独立散布
- **WHEN** 地区中某格没有配置地标，环境恰好符合独立散布条件或某地标需要保底
- **THEN** 该格继续受已接受的作者地区布局保护

### Requirement: LandMarkDefineMgr 数据加载
系统 SHALL 提供 `LandMarkDefineMgr`（实现 `IDataMgrBase<LandMarkDefine>`），从 JSON 加载，在 `GameDataMgr` 中注册。

#### Scenario: 按 ID 查询地标定义
- **WHEN** 调用 `LandMarkDefineMgr.Instance?.Get("volcanic_pillar")`
- **THEN** 返回对应 Define，不存在返回 null

### Requirement: 统一偏移按放置阶段生效

地标 SHALL 只有一个 AuraOffset 字段。仅创世自然阶段成功放置的自然地标 SHALL 立即应用一次并计入原初快照；人工地标 SHALL 在快照之后才创建，并将偏移作为每秒速率挂载。运行时 Place 不论 IsNatural 取值，均只挂载动态速率，不立即改变浓度、不重拍快照。移除地标 SHALL 停止其来源速率，不撤销历史变化。额外 ModifierDefineIds 维持原有每秒语义。

#### Scenario: 创世自然偏移计入原初
- **WHEN** 地块基础火浓度 10，创世自然地标 AuraOffset 火 20
- **THEN** 原初与当前火浓度均为 30，未来 Tick 不自动重复这个偏移

#### Scenario: 人工地标在快照后生成并缓慢变化
- **WHEN** 地块基础火浓度 10，初始人工地标 AuraOffset 火 4
- **THEN** 快照和刚生成的当前值均为 10，运行半秒后当前值为 12，原初仍为 10

#### Scenario: 晚生自然地标也动态变化
- **WHEN** 运行后在当前火浓度 10 的格子放置 IsNatural=true、AuraOffset 火 4 的地标
- **THEN** 放置时仍为 10，运行半秒后为 12，已有原初值保持不变

#### Scenario: 重试与移除不产生额外变化
- **WHEN** 放置失败或移除已放置的动态地标
- **THEN** 失败不留下偏移或速率；移除停止该来源的未来速率且保留历史浓度

### Requirement: 为独立地标刷新提供候选与放置边界

系统 SHALL 提供运行时候选查询和 `Place` 接口。运行时入口负责位面、可用地块、占位、合法定义、修正及全局唯一性检查。后续事件可声明自己的筛选规则。创世的数量、概率、条件和保底 SHALL 仅在创世生成或对应预览中执行。

#### Scenario: 不在地区模板中的尸体可以独立刷新
- **WHEN** 上层事件选择一个符合自身规则的空闲 plain 格，并请求放置合法的尸体地标
- **THEN** 地标可经运行时放置入口创建，元气只在后续 Tick 改变

### Requirement: 地标可由固定世界坐标预留

地标 SHALL 支持 `InitialPlacement.ForcedPositions` 绝对 Offset 坐标，在创世前段预留。坐标预留按显式作者布局生效，校验越界、引用、占位和全局唯一性。固定位置计入首批总数，可由同定义的独立规则继续补位。预留阶段 SHALL 保持原初元气，实际创建沿用自然、原初快照、人工的顺序。

#### Scenario: 固定地标不满足散布条件
- **WHEN** 固定位置本身合法，目标地形不满足该定义的普通独立选址条件
- **THEN** 固定地标仍按作者布局创建，并作为已规划实例计数

#### Scenario: 固定地标冲突
- **WHEN** 固定位置已有地区地标、另一固定地标或对应 singleton 已占用
- **THEN** 候选生成失败，不覆盖原内容或留下公开副作用

### Requirement: 首批独立选址与额外条件占位

`TerrainConditions` SHALL 读取最终 Tile 类型，`LandMarkConditions` SHALL 查询已经规划的地标，`TagConditions` SHALL 检查该格的地形标签，`ExtraCondition` SHALL 保存原始字符串，创世与预览均跳过它。三个生效条件同时满足才进入普通候选，空值跳过。简单条件支持正向值和 `No:` 排除，引用使用稳定 ID 或正式标签值。地形的多个允许类型取任一，地标和标签的多个条件须逐条满足。

#### Scenario: 海上且没有水府
- **WHEN** 地形条件为 `ocean`，地标条件为 `No:river_water_mansion`
- **THEN** 普通候选必须为海格，且格内不存在该定义的主地标或附加地标

#### Scenario: 依赖独立生成的地标
- **WHEN** 某定义配置 `Near(3):<地标ID>`、`No:Near(3):<地标ID>`，或声明 `After:<地标ID>`
- **THEN** 引用对象先完成规划，近邻判断读取六边形格距内的地区、绝对坐标与已生成计划
- **AND** 纯顺序声明只调整先后，自引用与循环依赖报告配置错误

#### Scenario: 预留的文本保持原样
- **WHEN** 定义填写任意 `ExtraCondition` 字符串
- **THEN** 字符串原样加载和导出，并保持同样的选址结果

### Requirement: 隐藏地标从玩家和 NPC 观察中排除

`IsHide` SHALL 默认为关闭。启用时，玩家与 NPC 的地标观察、名称、数量和可交互候选 SHALL 排除该实例。地标本身仍参与创世计数、占位、依赖查询和世界效果。绘制方式与可观察性分别配置。

#### Scenario: 既有隐藏主地标又有可见附加地标
- **WHEN** 同格主地标隐藏，附加地标可见
- **THEN** 玩家与 NPC 的观察只包含可见附加地标，主地标仍可被创世内部查询
