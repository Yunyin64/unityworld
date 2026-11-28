# landmark-editor

## Purpose

地标编辑器为策划提供编辑、预览、校验和导出地标的入口。导出数据由地图目录直接加载，并完整表达首批生成规则、显示方式和观察可见性。

## Requirements

### Requirement: 首页可打开地标编辑器

测试首页 SHALL 提供与角色模板编辑器同级的地标编辑器，支持新建、复制、编辑、保存、图片预览和导出。编辑器 SHALL 将固定位置、数量与概率、间距、三类选址条件与预留文本归在 `InitialPlacement` 配置区，表现和隐藏属性独立编辑。切换或退出未保存的草稿前 SHALL 确认。

#### Scenario: 独立填写或组合首批生成参数

- **WHEN** 策划只填 `NeedCount`，或同时填 `PerTileChance`、`MinCount`、`MaxCount` 与 `NeedCount`
- **THEN** 编辑器保留所有已填值，空值与显式的 0 可以区分

#### Scenario: 编辑生成参数时独立保留显示设置

- **WHEN** 策划复制带图地标并更改首批选址条件
- **THEN** 草稿维持其图片、绘制方式和隐藏选项，来源定义保持原样

### Requirement: 导出新定义和图片

编辑器 SHALL 将验证通过的新地标写入地图定义目录，并将导入的 PNG 放入资源目录。导出 SHALL 使用当前严格地标结构，生成字段位于 `InitialPlacement`，直接拒绝重名、无效引用、非法取值，`ExtraCondition` 原样读写。发生失败时 SHALL 保留草稿和已有文件，清理本次部分输出。

导出与保存 SHALL 校验定义自身和引用。具体地图的落点容量及数量缺口 SHALL 由世界生成阶段处理。

#### Scenario: 导出可用的新格式

- **WHEN** 用户导出新 ID、合法的首批生成参数和有效图片
- **THEN** 生成器能载入该地标，导出的 JSON 包含对应的 `InitialPlacement` 值且无 `SchemaVersion`、`SpawnWeight`

#### Scenario: 无效的数量组合

- **WHEN** `NeedCount` 大于显式的 `MaxCount`
- **THEN** 编辑器指出相应字段，导出不产生文件

#### Scenario: 落点容量随地图而变

- **WHEN** 一个地标的保底和间距合法，实际可用落点数取决于地图
- **THEN** 编辑器可保存此定义，任一固定种子的生成结果都不参与保存判断

### Requirement: 保存已有地标配置

选中已有地标时，编辑器 SHALL 锁定其 ID，提供「保存修改」，并在校验后更新原 JSON 中的对应记录。保存 SHALL 保留同文件其他定义，并在提交前核对打开时的源文件内容。替换图片 SHALL 写入独立的资源地址，并随这次定义提交生效。

#### Scenario: 修改已有地标的创世数值

- **WHEN** 策划选中地标、更改合法数值并保存
- **THEN** 重新打开相同 ID 可读取新数值，原引用继续指向该地标

#### Scenario: 草稿和源文件发生冲突

- **WHEN** 源文件在打开后被另一编辑器修改，或草稿未通过校验
- **THEN** 编辑器报告原因，保留源文件和当前草稿
