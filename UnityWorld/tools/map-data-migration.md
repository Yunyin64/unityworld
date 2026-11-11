# 地图数据作者与迁移说明

当前地图配置均为开发样例。200×200 和四方边界主题沿用已确认方向；边界厚度、地名、地区覆盖率、温湿参数、权重和移动消耗尚可调整。

## 文件组织

- `Data/TileDefines/<ID>.json`：26 种同级地块，每文件一个单元素数组。
- `Data/RegionDefines/<ID>.json`：25 个地区模板（含六份固定锚点模板）；每格显式配置，相对坐标是 Axial `(dq,dr)`。未列坐标始终是孔洞，不受旧 Width/Height 占位。
- `Data/LandMarkDefines/<ID>.json`：10 种地标；统一 AuraOffset，按创世自然／其他放置阶段应用。
- 世界生成参数改为 `Scripts/Game/World/WorldGenerationOptions.cs` 和 `WorldGenerationPresets.cs`，无独立 DefineMgr 或 JSON。
- `Data/TileModifierDefines.json`：保留原三个修正数值，`AuraData` 仍是每秒速率。

`MapCatalog.Load` 同时支持同名聚合 JSON 与对应递归目录；两者出现重复 ID 会失败。文件名和文件绝对路径不参与配置摘要。兼容旧 DefineMgr 的内容目录目前为单层；递归分类请以 MapCatalog 查询为准。

六份 `fixed_*.json` 用 50 或 100 格小模板加 11..20 个固定锚点组成完整边界，运行时才展开 6700 格。每个 ForcedPositions 坐标是完整地区的绝对 Offset 锚点，布局仍为相对 Axial；无方向专用算法。FixedEnvironment 提供环境锚点，great_wall 地块类型阻断所有移动能力。地标同样可用 ForcedPositions 预留绝对位置，实际实体仍按自然／人工分阶段创建。

布局较长时，每行约五个坐标；简单参数合行，保持可阅读。`tools/build_world_map_samples.py` 是批量制作样例配置的作者工具，不是运行时地图随机生成器；再次运行会重写该工具负责的样例，先保存手工内容调整。

## 严格审计

在 UnityWorld 根执行：

```powershell
python tools/audit_world_map.py --check
python tools/audit_world_map.py --self-test
dotnet run --project Console/UnityWorld.Console.csproj -- --map-data-tests --data-root .
```

检查模式只读源文件。Python 批量检查 ID、引用、占地、版本、边界参数及基本数值；C# 严格入口还检查未知字段、原始重复字段、相互水文端口、区间、预算等。任何错误都阻止大陆生成。Python 输出的文件路径和 ID 用于定位问题。

## 旧数据转换

原始三个文件完整保存在 `tools/map_legacy_source/`，不在运行时 Data 目录内。数字映射固定为：

| 旧值 | 新 ID |
| --- | --- |
| 0 | plain |
| 1 | hill |
| 2 | mountain |
| 3 | river |
| 4 | ocean |
| 5 | desert |
| 6 | forest |

`IsGuaranteed` 转换为 `MinCount` 的 0 或 1；布局只转换原有坐标，不补满矩形。`PlacementTerrains` 转为 `PlacementTypeIds`，保留原类型条件；地区生成按该类型的环境条件评估锚点，并在迁移报告中列出条件。未知数字、混用新旧字段、坏坐标或地标落在 footprint 外都会拒绝转换。

地标增加的 `AuraOffset` 默认全零，旧 modifier 数值不移入初值。新样例中的自然地标偏移在创世时立即生效，古观星台的偏移在快照后按秒生效。运行后新增自然地标也按秒生效。`ExtraTerrainIds` 与 `PlacementExtraTerrains` 原样保留，不因没有对应子系统而删除。

预览转换不写文件：

```powershell
python tools/audit_world_map.py --source tools/map_legacy_source --migrate
```

生成可核对输出及源文件副本：

```powershell
python tools/audit_world_map.py --source tools/map_legacy_source --migrate --output Docs/world-map-migration
```

输出包含转换后的文件、`source/` 原始副本及 `migration-report.json`。输出位置必须在源目录之外；原始副本不同则拒绝覆盖。转换前一次性检查所有输入，失败不会先写出部分转换。SchemaVersion=2 的输入保持内容不变，再次转换不会重复更换类型或增加初值。

回退时把原始三个文件与对应代码版本作为同一边界恢复；不要把新字符串布局喂给旧枚举加载器。已生成地图通过 manifest 的生成器版本、配置摘要和种子定位；本期不声明旧存档格式兼容。

## v3 河流规范名称

当前唯一ID为 `river`，与独立的 `lake`、`ocean` 并列。运行 `python tools/migrate_water_types.py` 批量转换类型、布局、放置过滤和邻接键值；先预检冲突／未知引用，再写入。重复运行变更数为0，初始元气不变。`ocean` 的水域Fill资格和正权重由作者脚本配置，水域类别仍是硬过滤。

`tools/map_water_v2_backup/` 保存本次迁移前六份原始文件（来自v2基线）；原有 `map_legacy_source/` 不变，备份均不参加运行加载。`test_water_migration.py` 在临时副本验证幂等、元气、备份、旧引用清除、冲突和未知引用拒绝。`build_world_map_samples.py` 作者脚本输出规范名称与三份大湖，JSON格式器独立在 `map_json_format.py`，导入迁移工具不会重写开发数据。
