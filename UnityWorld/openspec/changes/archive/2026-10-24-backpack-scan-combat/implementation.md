# 实施与验收记录（2026-10-24）

## 交付范围

已接通背包扫描战斗核心：主功法固定布局、位置灵元与逐格点亮、每行一次成功使用、单一待发槽、双方独立轮末、完整形状伤口和 SP 判负。世界侧布局与战斗状态隔离。核心可由控制台直接运行；本次不包含 Godot 界面接线或旧卡全量重设计。

## 本次采用的边界

- 成功使用结束本次点亮，进度复位为 0/n；未使用的进度和格上灵元跨轮保留。没有资源池，也没有使用时再次支付灵元。
- 入待发期间不接收下一次点亮资源；结算后重新参与分配。
- 待发槽已占用时，新拼点卡保留满亮等待下一次完整扫描，不占本行机会；右侧无拼点卡仍能使用。最小卡通过自身 `Contest` hook 声明拼点能力。
- 用户已确认由设计保证伤口有完整位置。先找纯空格完整位置，再允许灵元让位；各阶段按从上到下、从左到右的顺序找首个位置。非法碎片布局明确报数据错误，插入不拆格、不挪动其他卡、不改为背包满判负。
- 多格距离算法按要求保留独立接口。当前可运行默认值为“到最近占格的曼哈顿距离”，等距下方优先、左方次之；这是实现占位算法，不代表最终玩法算法定案。

前两项和待发占位行为是本次公开的实现选择，验收时可重点审阅；没有重新改写用户已经确定的逐格顺序、跨轮、单槽、独立轮末规则。

## 数据字段与装配入口

| 字段 / 入口 | 约束 |
| --- | --- |
| `CardDefine.Shape` | 相对左上角的 `(X,Y)` 集合，坐标从 0 起，X 向右、Y 向下。最小 X/Y 为 0，格子不重复，数量等于正数 Size；当前支持平移，不新增旋转 |
| `CardDefine.ScanActivatable` | 显式声明扫描主动卡；开启时 `ManaCost` 各元素的正数数量总和必须等于 Size；JSON 为大小/形状/需求权威来源，Lua 负责效果 |
| `CultivationDefine.BackpackLevels` | 每级 `Level/Width/Height/ValidCells/MainCardCells`；有效格为空表示完整矩形；主卡固定占格也必须符合其 Shape/Size |
| `NpcCardData` | `MainGongFaCardId/MainGongFaLevel/Placements` 保存唯一主功法及卡实例坐标；Field/Reserve 与摆放一致，Clone 深拷贝布局 |
| `Npc.SelectMainGongFa` | 通过现有 Card.Id 与 GongFaMgr 绑定选主功法，固定主卡；不猜选、不自动补无功法兜底 |
| `Npc.PlaceBackpackCard` | 校验形状、越界和重叠，移入 Field；修改失败保留原布局，主卡不可被移位/覆盖/候补/移除 |
| `CombatNpc.CreateCombatNpc` | 要求完整合法装配且 SpMax 不超过有效格数；立即复制布局、卡基础数据和属性 |
| `CombatBackpack` | 战斗私有格上灵元、每卡点亮资源、轮/行/行 Tick；对外占格和资源集合只读 |
| `BackpackManaTargetSelector.Distance` | 可注入距离函数；先筛需要且能接收的卡，再排序；无候选留在原格 |

新定义在 `Data/Card/BackpackTest.json`、`Data/Practice/GongFa/BackpackTest.json`，效果在 `Data/Card/Lua/bp_*.lua`。两张主功法也可作为副功法按普通卡摆放；另含攻击、三格蓄灵、防御、无拼点护盾和两种完整形状伤口。

## 调度与日志

`CombatScene.Tick` 在行边界先扫描所有存活参战者，再尝试双方对拼，然后只强制轮末所属者的剩余待发；同 Tick 的攻击先全部进入伤害队列，再处理伤口与 SP 败退，最后各自换行/补灵。`design.md` 已列 Tick 10/20/30/40/50/60/150 的三行对五行时间表。

`TraceLog` 包含 Tick、Npc、Round、Row、格坐标、分配目标与点亮数量、留格/跳过原因、待发所属者、实际生成数和 MP 成本、伤口替代格。日志轮号从 1 起，行列从 0 起。初始化与调度按稳定 NPC ID 排序；同种子同布局交换注册顺序得到相同轨迹。

## 验证命令及结果

在 `C:\Unity\Game\unityworld\UnityWorld` 运行：

```powershell
dotnet build UnityWorld.csproj --nologo -v quiet
dotnet run --project Console/UnityWorld.Console.csproj -- --backpack-tests
dotnet run --project Console/UnityWorld.Console.csproj -- --core-smoke-test
dotnet run --project Console/UnityWorld.Console.csproj
openspec.cmd validate backpack-scan-combat --strict
```

本次实际结果：

- 核心构建 0 错误；最后增量构建有 2 条既有 NCalcSync NU1902 提示，未调整依赖版本。
- `BACKPACK_MODEL_TESTS_OK assertions=72`。
- `BACKPACK_COMBAT_TESTS_OK assertions=87`，合计 159 条断言。测试运行未发现 Lua 异常或 ERR 日志。
- `CORE_SMOKE_OK seed=12345 ticks=3 tiles=40000`。
- 默认固定布局演示运行到 Tick 150，返回 `TurnLimitReached / WinnerTeam=None`，用于观察双节奏；完整胜负场景由集成测试另行覆盖，不能把演示平局说成击败验收。
- OpenSpec strict 检查结果为 valid；`git diff --check` 通过。

| 任务 | 断言或演示证据 |
| --- | --- |
| 1.1、3.3 | `RowOrder/DrawAndCarry`：每个灵元进入只点亮一次；2/3跨轮保留；迟到补满不追补；下一轮完整扫描使用；仅成功使用复位 |
| 1.2、5.2 | `SlotsAndTimeline`：逐节点检查3行/5行时钟；A轮末不清B槽；Tick150同时末行配对只伤害一次、双方Apply各一次，没有重复强制直击 |
| 1.3、6.2 | `Wounds`：满背包单格/双格伤口；纯空位优先；一空格加一灵元完整放置；碎片数据拒绝且原子；替代灵元不可再用；HP归零恢复，SP超限败退 |
| 2.1 | `Layouts`：6×5、8×3、等级空间与主卡固定位置变化、非矩形有效格；Shape/Size/需求数量不符拒绝 |
| 2.2 | `Layouts`：无主功法拒绝；主卡不可动；副功法不扩包；重叠/越界/重复拒绝；编辑回滚、Field/Reserve、布局克隆隔离 |
| 2.3 | `IsolationAndCarry/FullBattles`：同源两场战斗状态独立，世界后续变化不渗入快照，战斗不改世界卡组和关键词 |
| 3.1 | `DrawAndCarry`：开场抽取；正常补充只在本方轮末；计划5仅2空格只生成2扣2MP；满格零成本；MP不足截断；同种子轨迹可重放 |
| 3.2 | `TargetSelection`：过滤已满/元素不符；选较远需要卡；等距下左；无目标原地保留；距离可注入 |
| 4.1、4.2 | `RowOrder`：不足10Tick不扫；跨行/同行多格都在末格判断；A缺灵B满、连续缺灵、B/C满只用B、左灵元先分配、右灵元不可追溯、本行额度用完仍分配；败退停止扫描 |
| 4.3、6.1 | `LuaFixtures/PassiveLifecycle/RowOrder`：真实JSON/Lua主动链路；旧FaShu/Ready/CD不能抢出手；无拼点立即一次Apply，拼点结算后一次；被动只走关键词OnTick与自身OnPassiveTick |
| 5.1 | `SlotsAndTimeline`：行切换双方有数据即拼；单方等待；槽满新卡保留满亮且不占行机会，轮中不能直击 |
| 5.3 | `CombatRegression`：6斩/4斩=6；6斩/4刺=2；6射/4射=2；平局、防御胜出、护盾、直击回归 |
| 6.3 | `CombatRegression/FullBattles`：真实结果、胜利、双方败退安全日志、准确Tick上限 |
| 6.4 | 固定种子控制台演示；上述场景日志覆盖短轮、跨轮保留、空格不足、实际成本和伤口替代 |
| 7.1、7.2 | 核心构建、159断言、世界烟测、两种背包完整胜负、strict校验及规格/任务逐项核对 |

## 后续事项与兼容范围

- 旧冻结/加速/CD、Min/Max/ManaAdj/AboveAll、法宝、灵元转换/减灵、候补部署/召回与线性位移需要按新玩法重设计。核心旧独立CD与池扣费不再运行；保留的旧支付/转换/候补API签名明确拒绝调用，避免恢复第二套资源真值。不能将未迁移旧卡直接当作新机制卡组。
- 正常进入战斗必须显式装配主功法、形状与位置；未自动迁移所有历史NPC/卡牌。
- 距离函数是明确预留；普通卡旋转和特殊触发规则不在本次实现范围。
- `ApplyHeal` 的既有超上限问题、既有 nullable/包警告另行处理。
- 2026-10-24 归档整理：已同步三份背包战斗主规格，并修正、更新 `passive-card` 主规格；其原“主动卡的 Tick 不受影响”场景由本变更“主动卡由扫描驱动”场景替代，不保留相互矛盾的旧 CD 行为。四份主规格与本变更均通过 strict 校验。`zhaoshi-rotation` 保留未完成状态，提案已注明旧 CD 方案与现行扫描机制冲突、须重新设计。全库另有 16 份历史主规格缺少标准 Purpose/Requirements 结构，未在本次扩范围修改；全库规格校验仍未通过。
- Godot 展示、拖拽布局及美术尚未接入，核心验证不能替代界面验收。

## 归档记录

- 变更：`backpack-scan-combat`；schema：`spec-driven`；任务：21/21。
- 归档路径：`openspec/changes/archive/2026-10-24-backpack-scan-combat/`。
- 四份 delta 已逐项核对到主规格，无剩余待应用内容；归档时跳过重复同步。
- 归档仅整理规格和文档；上述运行验收结果来自实施阶段，本次未改战斗代码。旧卡重设计、历史 NPC 装配、距离算法定案、Godot 界面及其他后续事项仍未完成。
