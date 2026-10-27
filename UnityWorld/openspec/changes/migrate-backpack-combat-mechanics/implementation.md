# 背包旧机制迁移实施记录

## 基线与状态

- 实施起点：2026-10-25，Git HEAD `456cd4b`，开始时工作区干净。
- 根目录：`UnityWorld`；核心目标框架 .NET 8，当前 SDK 9.0.201。
- 修改前实际运行 `dotnet run --project Console/UnityWorld.Console.csproj -- --backpack-tests`：模型 72、战斗 87 项断言通过。此结果仅作回归基线，不作为本次新增功能验收。
- 本次 41 项实现和验收已完成；同步、归档尚未执行。最终结果与矩阵如下。

## 已采用的实现约定

- 公开位置使用从 1 开始的坐标；内部 `BackpackCell` 仍从 0 开始，通过统一转换边界连接。
- 扫描进度采用 `decimal` 正常速度 Tick 单位，精确保留半速和跨行余量。
- 真实 Tick 开始先增加自然进度并消耗既有速度时长，随后回调和扫描新加的速度从下一 Tick 生效；充能只增加进度。
- 多行推进按批处理：所有同批角色各扫完整一行，再对拼、各自轮末、伤口/败退、换行/补灵，然后继续下一批。
- 每 Tick 超过 10000 批扫描属于异常诊断，明确终止该战斗并输出剩余进度及最近日志，不截断后继续。验收包含 1000 行合法连续推进；该上限不是正常玩法次数或冷却限制。
- 条件事件先收集卡自身、Keyword、EventMgr 两通道的请求，再按角色 ID、公开位置 Y/X、卡实例 ID 排序提交。`TryUseFromEvent` 返回机会已接收；最终提交重新检查资源和槽，事件结束丢弃失败请求。相同事件不得复用点亮，过期事件不可重试。
- 行边界快照双方待发槽，每个角色同批最多参与一次配对。对拼 Apply 派生的新槽等待后续边界；随后本方轮末的独立强制结算仍按既定顺序执行。

## 验证命令

### 时间与调度实测

- `dotnet run --project Console/UnityWorld.Console.csproj -- --backpack-timing-tests`：最终退出 0，`BACKPACK_TIMING_TESTS_OK assertions=59`。
- T1/T2：正常/双倍/半速进度、半 Tick 精度、同类延时、异类各自到期、行内新增 1 Tick 加速的边界均通过。
- T3：四种倍率下 4/10 加 10 进度均完成一行剩 4/10；充能不扣加减速剩余时间。
- T4：同 Tick 六行跨轮、行内新增两行、双方同时轮末配对一次、败退后保留未消费进度，以及 1000 行压力验证均通过。
- 最终运行 `--backpack-tests`：模型 72、战斗 88 项通过（含动态伤口位置与索引一致）。新测试没有取代既有逐格顺序、跨行形状和伤口回归。

以下命令在 `UnityWorld` 目录运行。统一构建后可加 `--no-build` 批量执行独立测试，避免重复编译；修改 JSON 后先构建刷新输出目录中的数据副本。

```powershell
dotnet build UnityWorld.csproj
dotnet run --project Console/UnityWorld.Console.csproj -- --backpack-tests
dotnet run --project Console/UnityWorld.Console.csproj -- --backpack-timing-tests
dotnet run --project Console/UnityWorld.Console.csproj -- --activation-migration-tests
dotnet run --project Console/UnityWorld.Console.csproj -- --effect-tests
dotnet run --project Console/UnityWorld.Console.csproj -- --template-tests
dotnet run --project Console/UnityWorld.Console.csproj -- --core-smoke-test
dotnet run --project Console/UnityWorld.Console.csproj
python tools/audit_backpack_content.py
openspec.cmd validate migrate-backpack-combat-mechanics --strict --no-interactive
git diff --check
```

## 最终结果与验收矩阵

所有下列运行退出码均为 0；测试总计 **487 项断言**。最终测试日志按 `backpack-regression/timing/activation/effects/templates/smoke/template-battle.log` 保存在项目工作目录，日志为本地运行产物，不作为新增源码提交。

| 验收 | 实际证据 |
| --- | --- |
| F1、F2、F3 | 主动事件 91 断言覆盖满灵/缺灵/额度已用/多格末格解冻、条件冻结拒绝、原待发继续结算并保留冻结 |
| F4 | 效果 85 断言覆盖冻结优先未冻、全冻不叠加；主动回归验证被动与接收灵元继续 |
| T1、T2 | 时间 59 断言精确半速、倍率相乘及各自到期；真实元素效果日志 10/20 Tick → 次 Tick 9/19、进度 1 |
| T3、T4 | 4/10 加十保留余量、六行跨轮、1000 行压力、行内再充能、双方同轮末、新待发延后、败退停止 |
| K1、K2 | 真正长剑 JSON/Lua、纯法宝和混合 Keyword、缺资源/槽满/冻结/待发、空 Contest 声明、失败不消费 |
| K3、K4 | 两通道同事件去重与统一位置排序、反序 C# listener、事件原来源、被动单次、无开场免费主动 Use |
| K5 | 有限 A→B→A 缺灵反例不误杀；真实循环明确中止；5000 同链及 100 独立事件通过，10000 超限诊断 |
| S1 | 实际 AboveAll 包含异列/主功法/伤口，排除跨来源行，打乱 Field 后一致 |
| M1、M2、M3 | 格实体不放回且同种子可重放、元素过滤、数量不足部分成功、MP 空缺限制、回收只发一次 MP 事件、卡上资源保护；真实 Lua 删除右格、腾格再抽取且不倒退扫描 |
| D1、D2 | 所有原 Size 0 明确迁移；正式横条、默认 5×5/中心 (3,3)、公开边界、越界和测试几何配置 |
| N1、N2、N3 | 模板 92 断言含重复 key 独立实例/装备、主卡一次、无自动子卡/FormList、非模板自动发牌保留、双快照隔离 |
| N4 | 主功法缺失/歧义、缺位置、重叠、越界、SP、旧数据混用均拒绝；NPC/Card 及关联子系统注册数回滚 |
| C1 | 误标 Migrated 的 Min/Max/ManaAdj 仍拒绝；动态运行守卫、待定操作失败后非空待发/格灵元/位置/SP 全部不变 |
| R1 | 原模型 72、战斗 88 断言通过，保留逐格/迟到补满/跨轮点亮/几何/伤口/拼点/快照基线 |
| R2 | 世界烟测 seed=12345、3 Tick、40000 格；三个正式模板可直接创建战斗快照；正式 A/B 第 740 Tick，AllDefeated、TeamB 获胜 |

构建成功、零错误。编译仍有既有 CS8632/CS0414 等警告；恢复依赖时仍提示 NCalcSync 5.12.0 的 NU1902。未在本变更升级依赖或清理全库警告。OpenSpec strict 校验通过，`git diff --check` 通过。

## 数据迁移与配置

| 正式 Card 状态 | 数量 | 处理 |
| --- | ---: | --- |
| Migrated | 26 | 实际旧 ID 加载和 Use/Apply 验证；纯被动不强制收灵，长剑使用真实条件事件 |
| Unmigrated | 52 | 缺效果、空费用/元素方案未定、次数/空间依赖等保留并拒绝装配 |
| Deprecated | 1 | 金针依赖 ManaAdj，保留定义与脚本、禁用 |
| Pending | 1 | 旧水系位移，保留为后续神修操作 |

11 个 Card JSON 通过批量校验，17 个原 Size 0 改为 1；25 个正式功法尺寸字段已批量补齐。完整逐卡清单、零格 ID 和调用审计见 [效果与数据证据](evidence-effects.md)。此处统计排除已有 `bp_*` 回归定义。

模板位置条目保持列表，不转成字典；例如同定义双卡：

```json
{
  "MainGongFaId": "jin_fa",
  "InitCardPlacements": [
    { "Key": "jin_fa", "Value": { "X": 3, "Y": 3 } },
    { "Key": "card_jin_slash", "Value": { "X": 1, "Y": 1 } },
    { "Key": "card_jin_slash", "Value": { "X": 1, "Y": 2 } }
  ]
}
```

重复功法/装备绑定使用从 1 开始的 `PlacementIndex`，招式父装备引用用 `ParentPlacementIndex`；真实完整模板及绑定示例见 [模板证据](evidence-template.md)。`NpcMgr.Assemble` 是模板入口；`Birth` 和显式 `AssembleGenerated` 保留玩家/生成来源策略，不用是否配置某字段推断来源。

正式 A 使用原金法、斩击、长剑、木灵、爆燃、冻结、土缚，初始 10/11 SP；B 使用火法、双射击、冻结、土缚，6/10 SP。灰狼模板使用原狼牙/兽皮与明确主功法和两张斩击，5/8 SP。A 的神识由初次演示的 12 调整为 11，第二个完整伤口触发败退；未修改伤口/SP 规则。

## 日志读法与实际演示

`Tick` 是真实战斗时间，`Round`/`Row` 属于该 NPC，Row/格坐标均从 1 开始；`余量` 是正常速度 Tick 进度。条件日志包含 Event/Chain/Trigger 和原 Caster/Source；`点亮=n/Size`、`入槽`、`行切换对拼`、`轮末强制` 对应不同阶段。

固定 seed=12345 的正式演示中，Tick 40 出现冻结，70 加速，120 减速，130/215 长剑由 OnBaseManaDraw 条件发动，400 爆燃充能，740 A 伤口使 SP 超限、B 获胜。参数与事件回调约定详见 [主动事件证据](evidence-activation.md)。

## 交付边界

神修操作、玩家排包 UI、生成 NPC 组牌、距离算法定案、旋转、功法等级成长、ApplyHeal 修复、既有告警清理和 Godot 验收均不在本次范围。旧代码和数据保留，通过兼容状态明确区分已迁移、废弃、待定与未迁移。

旧 Wound.json 十张伤势的持续自伤/元素干扰没有对应旧 Lua，仍属未迁移；当前轻重伤完整占格沿用之前已实现的 `bp_wound_small/bp_wound_wide`。真实模板的初始角色、主功法、武器与控制卡全部来自原正式内容；本次不声称已实现这些旧伤势的持续效果，也没有新增替代正式内容的 BackpackTest 卡。

同步或归档需另外执行；现有 `keyword-system` 主规格仍有历史 delta 头结构问题，本次 strict 检查会给出提示。它不等同本次实现失败，也不能把本次规划校验通过当成已归档。
