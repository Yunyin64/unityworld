# 初版验收记录

日期：2026-10-31。本变更完成地图生成、配置、运行时接入和诊断预览，尚未归档。

## 用户确认的实施修正

- 固定边界改为 `WorldBoundaryBuilder` 按代码中的确定规则生成。世界种子只改变腹地；四角归属规则明确。
- 主世界配置不展开边界格：`Data/WorldGenerationDefines.json` 为 38 行、2492 字节。
- 地图 Define 拆为独立小文件，简单属性合行、地区坐标按组阅读。

| 类型 | 文件数 | 最大行数 | 最大字节数 |
|---|---:|---:|---:|
| 地块 | 26 | 18 | 1117 |
| 地区 | 19 | 48 | 4032 |
| 地标 | 10 | 8 | 418 |

原三个地块元气修正定义的字节和值保持；旧地区/地标源文件保存在 `tools/map_legacy_source/` 供迁移核对。该目录不参与运行时加载。

## 初版历史预览结果（阶段调整前）

- 种子：`20261031`
- profile：`china-inspired-v1`
- 生成器版本：`region-puzzle-v1`
- 配置摘要：`45b5e1d5ea27a09282e3a7a2011ffff48b4bd30e61ff073f3119201b749fdce1`
- 结构摘要：`bbb6dea5990a62a807354f8bb63b0ddd024cad518f45200ace56ebf000fb0a05`
- 输出：外层 `UnityWorld/Docs/world-map-preview/seed-20261031-20261031-032716-fb569de3/map.html`
- 40000 格、169 个完整地区、3762 个固定边界格、717 个水系格；本次生成阶段合计约 1.10 秒，仅为本机观测值。

已在浏览器实际验证全图渲染、湿度图层、点选地块、地区下拉定位、填充候选权重展开；浏览器未记录脚本错误。页面可以本地打开，数据自包含。控制台导出和真实 `WorldMgr.Initialize` 在相同种子/配置下结构摘要一致，真实入口导出核验时世界 Tick 为零。

## 测试证据

`dotnet build Console/UnityWorld.Console.csproj --no-restore --nologo -v quiet -clp:ErrorsOnly` 成功，0 错误；现有依赖警告未作为通过证据隐藏。

`--map-generation-tests --data-root .`：

| 测试 | 断言 |
|---|---:|
| MAP_DATA_OK | 37 |
| MAP_GENERATOR_TESTS_OK | 12992 |
| MAP_RUNTIME_OK | 712 |
| MAP_DIAGNOSTICS_TESTS_OK | 12 |
| 合计 | 13753 |

覆盖定义严格校验与迁移、完整固定拼图、环境适配、空隙填充、失败回滚、流向/端口/等高环/内陆湖、河流载体白名单、关键地点通行、跨位面快照、速率/初值分离、同进程与独立进程复现、默认主世界入口和导出失败隔离。数据脚本 `--check` 检查 57 个文件通过；`--self-test` 8 断言通过；检查模式源文件哈希未变，重复迁移结果一致。

20 个种子的完整验收：1..20 全部成功、每张 40000 格、固定边界只有一个摘要。地区覆盖率实际范围约 32.63%～37.74%，低于 42% 软目标时保留警告，必需地区、完整性、水系和通路均通过。

原有回归分支全部通过：core-smoke（3 Tick、40000 格）、backpack-model（72）、backpack-combat（88）、backpack-timing（59）、effect-migration（85）、activation-migration（91）、template-backpack（92）。各次日志位于外层 `UnityWorld/Docs/world-map-preview/verification/`。

地图专项最终日志：`UnityWorld/Docs/world-map-preview/verification/map-final-tests.log`。
批量报告：`UnityWorld/Docs/world-map-preview/verification/batch-1-20-20261031-031825-9198ed4c71c74050b96068173ecec21a.json`。

## 复核修复

- 拒绝未注册或非当前目录实例的 profile，防止调用者绕过实际配置校验。
- 河流、湖泊、海洋载体同时检查类型能力与 profile 白名单；错误名单在加载时拒绝。
- `PlaneDefine.MainWorld()` 与 `ToConfig()` 统一 200×200 大陆模式，主位面不能通过 BasicFill 绕过契约。
- 填充格保留候选集合、基础权重、环境权重、邻接权重和前瞻淘汰标记。
- 修复九个受影响旧主规范的结构，使条文可被 OpenSpec 识别；正文未在归一化时丢失。当前 change 严格校验通过且无 issue。

## 仍可调优的内容参数

边界厚度、南部两种环境的具体分布、各地区形状、覆盖率、邻接偏好和地块权重均为开发样例。本版提供审阅与调参工具，没有把地区覆盖率不足伪报成达到目标。角色逐格移动执行、迷雾与探索事件属于后续功能。

迁移和回退步骤见 `tools/map-data-migration.md`；可执行命令与作者说明见 `Docs/世界/地图生成配置.md`。生成诊断放外层忽略目录，不加入运行时源数据。

## 2026-11-02 地标阶段调整验收

- MapCatalog.cs 已移动至 Scripts/Game/World，与 WorldMgr.cs 相邻；独立类和命名空间保持。
- 删除 InitialAuraOffset 字段，统一使用 AuraOffset；所有 10 份地标配置和作者／迁移脚本同步更名。
- 地区和独立地标的位置计划按 IsNatural 分组：先实际创建自然地标并应用一次偏移，准备原初快照，再实际创建人工地标及动态修正。
- 创世人工地标与运行后新增的任何地标（含自然地标）均不立即改变浓度，AuraOffset 作为每秒速率累积，移除停止，快照不变；额外 ModifierDefineIds 保留。
- Region.AuraProfile 保留兼容应用功能，移除专门配置校验和数值验收；正式内容默认零。
- 独立刷新扩展位置为 LandMarkMgr.GetIndependentSpawnCandidates + Place，注释明确后续数量区间、随机选取、周期及不足数量策略；未实现自动刷新调度。
- 最新配置摘要：d72e10171ce564bb6a25600351e1d28589945a22172363ec2ce6c30b50d37a86。上面的初版摘要和预览为历史证据，不代表调整后的配置。
- 核心／Console 构建通过（0 错误；现有警告仍在）。完整地图测试通过：数据 37、生成器 12992、运行时 720、诊断 12，共 13761 个断言。包含地区附带和独立来源的两类地标分阶段、人工慢变、晚生自然慢变、移除停止及快照隔离。
- core-smoke：3 ticks、40000 tiles；backpack-tests 模型与战斗回归通过，战斗 88 断言。
- 数据审计 57 文件通过，脚本自测 8 断言通过；OpenSpec 严格校验及 git diff --check 通过。
- 日志：外层 UnityWorld/Docs/world-map-preview/aura-phase-tests.log、aura-phase-core-smoke-test.log、aura-phase-backpack-tests.log。

## 2026-11-03 固定锚点地区复用验收

本节为最新版本；此前边界预设、世界 Define JSON、v1 摘要及统计均为历史记录。

- WorldGenerationDefine／WorldGenerationDefineMgr 的职责迁移为 World/WorldGenerationOptions.cs 与 WorldGenerationPresets.cs，取消 Data/WorldGenerationDefines.json；PlaneConfig.GenerationOptions 支持调用方参数，摘要包含有效参数。
- WorldBoundaryBuilder 原文件迁为 WorldMapFixedPlacements.cs，不再按方向专门生成边界。地区统一经 RegionPlan／CommitRegion 提交，ForcedPositions 每点完整复用一份相对 Axial 小拼图。
- 六个 fixed_*.json，最大 36 行、2964 字节；单模板 50 或 100 格，合计 92 个锚点、6700 个固定格。东海 10×10 模板在 20 个坐标复用。固定地区跳过普通权重、环境、间距和数量筛选，退出随机池；普通覆盖率排除固定面积。
- 固定地标在最前预留位置，支持固定地区空格和普通 Fill，仍按自然→快照→人工阶段实际创建；引用、占位、越界、singleton 不被绕过。
- 新增 5×5 奇偶行锚点、非法普通条件强制放置、冲突／越界失败、固定内容保护、固定地标过滤绕过、singleton 冲突、固定地标运行时元气及参数摘要测试。
- 构建 0 错误；完整地图测试通过：数据 36、生成器 18099、运行时 1082、诊断 12，共 19229 个断言。数据审计 62 文件通过，脚本自测 8 断言通过。
- v2 下 1..20 种子全部成功、每张 40000 格，固定摘要仅 1 个；普通地区覆盖率 32.09%..37.96%，低于 42% 的软目标仍如实警告。
- core-smoke：3 ticks／40000 tiles；backpack-tests 模型及战斗通过（战斗 88 断言）。纯生成与独立真实世界入口在种子 20261031 的结构摘要一致，真实入口 ticks=0。
- 最新配置摘要：2b32a909c65e0da19131b240965347df2214a757c6cca8b626a9e12d1f10769e；版本 region-puzzle-v2；种子 20261031 结构摘要 7971946df9ff0a1c6c3b5045c95430ef4fc2038264769da3f15ebbbd2ea47655。
- 最新预览：外层 UnityWorld/Docs/world-map-preview/fixed-region-verification/seed-20261031-20261102-161035-c0d871db/map.html；浏览器实看地形和生成来源图层，固定地区显示正常，页面错误日志为空。
- 批量报告：外层 UnityWorld/Docs/world-map-preview/fixed-region-verification/batch-1-20-20261102-161052-86693a672a9f476285abbb8b972555ed.json。文件夹名时间来自执行宿主 UTC，验收日期使用用户当前本地日期。
- 日志：外层 UnityWorld/Docs/world-map-preview/fixed-region-tests.log、fixed-region-batch.log、fixed-region-core-smoke-test.log、fixed-region-backpack-tests.log、fixed-region-runtime-hash.log。
- OpenSpec 严格校验和 git diff --check 通过；尚未提交或归档。
