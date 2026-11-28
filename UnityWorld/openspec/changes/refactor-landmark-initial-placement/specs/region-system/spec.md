## MODIFIED Requirements

### Requirement: RegionDefine 静态配置

地区定义 SHALL 包含稳定 ID、固定相对 Axial 坐标到地块类型 ID 的布局、固定地标布局、`AllowInitialLandMarks`、环境适配条件、`MinCount`、`MaxCount`、选择权重、间距及水系接口。布局中配置的坐标 SHALL 是唯一占地集合；`Width`/`Height` 只保留为迁移元信息，不产生额外占位。地区 SHALL 保持作者朝向，不旋转、镜像、裁切或逐格随机。定义 SHALL 保留拓展语义标签、叙事标签和区域初始元气偏移；原锚点地形限制迁移为明确的环境适配或类型条件，不能无提示丢弃。

#### Scenario: 必然生成区域优先放置
- **WHEN** 某地区定义的 `MinCount > 0`
- **THEN** 满足其最少数量的实例在可选随机地区前安排
- **AND** 旧 `IsGuaranteed=true` 默认迁移为 `MinCount=1`，`false` 默认迁移为 `0`

#### Scenario: 长方形超出地图边界
- **WHEN** 候选锚点使任意实际配置格子越出地图边界
- **THEN** 整个候选被拒绝，不生成部分地区
- **AND** 仅 `Width`/`Height` 对应的旧外框越界、实际配置格全部合法时，不因旧外框单独拒绝候选

#### Scenario: 区域地形布局覆盖
- **WHEN** 地区布局配置 `(0,0) → mountain` 且候选通过全部检查
- **THEN** 锚点地块的最终类型为 `mountain`，其他配置格按各自类型原样落地

#### Scenario: 区域地标固定生成
- **WHEN** 地标布局配置 `(0,0) → volcanic_pillar` 且地区成功落地
- **THEN** 该坐标生成配置地标，地区提交时给首批独立补位提供位置和实例数
- **AND** 定义、修正引用、占位或唯一性校验失败时，整个地区候选失败

#### Scenario: 地区允许或关闭首批独立生成
- **WHEN** 已占地格为空且地区的 `AllowInitialLandMarks` 为默认 `true`
- **THEN** 此格可以参加独立落点选择，固定锚点地区使用相同规则
- **AND** 配置 `false` 时，普通生成和保底均跳过该地区，预置地标与固定坐标继续生效

#### Scenario: 不规则布局保留空洞
- **WHEN** 地区外框内某坐标没有配置地块
- **THEN** 该坐标不归此地区占有，可以由其他地区或基础填充使用
