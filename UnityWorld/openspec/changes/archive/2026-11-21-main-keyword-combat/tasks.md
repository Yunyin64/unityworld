# Main Keyword 实施任务

按 [设计](design.md) 的迁移清单完成数据切换。每组任务一并交付对应的定向验证。控制台命令从 UnityWorld 目录运行，项目入口是 Console/UnityWorld.Console.csproj。

## 1. 建立并迁移唯一 Main

这组任务让创建卡、预检模板和进入战斗时对同一张卡得到同一个 Main。

- [x] 1.1 为 Keyword 注册增加 Main 角色元数据与公共解析校验，供卡定义、世界卡、模板预检、Lua 附加词和战斗快照复用。验证零 Main、双 Main 分别有可定位诊断，普通 Keyword 与唯一 Main 可以共存。
- [x] 1.2 按设计清单迁移 `Data/Card` 的旧定义与 Lua 主词声明，覆盖 12 张伤口、7 张物品、2 张测试功法、4 张扫描夹具和铠甲、利爪两张双标法宝。核对 268 张均有唯一 Main，当前基线分类为法术 183、法宝 3、被动 67、招式 15，普通词和兼容状态按原样保留。
- [x] 1.3 让程序构造的卡、动态伤口和个人模板导入接入同一校验。验证已知旧定义可按映射读入，冲突数据能指出对应卡和条目，主功法及父装备实例引用在导入和预检中保持正确。用 `dotnet run --project Console/UnityWorld.Console.csproj -- --backpack-model-tests` 和 `dotnet run --project Console/UnityWorld.Console.csproj -- --template-tests` 覆盖实际入口。

## 2. 统一末格发动并实现被动生命周期

这组任务使成功发动成为唯一消耗满灵和行额度的时刻，并让法宝在正确的末格处理续期。

- [x] 2.1 根据 Main 分派扫描发动和常驻被动。将对拼待发资格、唤起效果前的蓄灵与行额度提交收在同一个扫描入口。用 `dotnet run --project Console/UnityWorld.Console.csproj -- --backpack-tests` 验证法术、法宝与招式同排时按左到右竞争一次额度，满槽的拼点卡失败后右侧合法卡仍可用额度。
- [x] 2.2 实现法宝的首次激活、末格连续续期、到期关闭及离场处理。用 `dotnet run --project Console/UnityWorld.Console.csproj -- --activation-migration-tests` 验证运行期间可蓄灵、续期不重复挂监听、待发槽满时法宝仍能激活、额度被使用时保留满灵且关闭旧被动。用 `dotnet run --project Console/UnityWorld.Console.csproj -- --backpack-timing-tests` 验证缺灵、冻结和加速下仍在正确的末格处理到期。
- [x] 2.3 实现永远激活的 `Passive` Main，并按卡效果保留原有明确开场回调。验证兽皮开场实际增加 1 层斩击抗性，空被动伤口只占格，插入时不会额外发出 `OnApply`。使用已有 `--effect-tests` 和 `--backpack-tests` 路径覆盖。

## 3. 接通装备自带招式与装配校验

这组任务保证每张招式有明确的装备来源，每件装备自带的招式都能在背包里逐张对应。

- [x] 3.1 按装备实例的生效 `FormList` 解析必带招式，给玩家、生成 NPC 和显式模板共用。正式装配与 Godot 模板预检查齐全性、父装备、位置和 SP。用 `dotnet run --project Console/UnityWorld.Console.csproj -- --template-tests` 覆盖少招式、错父装备、容量不足、同名双装备和重读模板。同一个招式条目重复装配或读取时只对应一个实例。
- [x] 3.2 为招式按自身实例及所属背包轮次记录成功次数，默认 1 次。保留供招式读取父装备攻防和元素的 `GetEquip` 能力。用 `dotnet run --project Console/UnityWorld.Console.csproj -- --backpack-timing-tests` 验证同轮移位后再次扫到仍受上限限制，失败保留次数，轮末重置，同名招式各算各的次数，父法宝未激活也可出招。

## 4. 迁移法宝、招式和正式测试模板

这组任务先让真实长剑链跑通，再处理现有装备已引用的招式，给暂未开放的旧内容保留明确状态。

- [x] 4.1 重做 `card_fabao_jian` 的效果和说明。保留 2 格、2 金灵元与装备基础属性，改为末格激活被动。用 `dotnet run --project Console/UnityWorld.Console.csproj -- --activation-migration-tests` 验证原抽灵事件不能主动触发入槽、激活后的己方斩击获胜能让下次实际招式对拼加 1、本次对拼与轮末直击不使用加成，以及加成不叠加、到期清理符合设计。
- [x] 4.2 清理法宝共用的 `UseFabao` 旧入口，以及招式脚本中的旧 `CardType`、CD 和空费用声明。确认铠甲和利爪各有唯一 `FaBao`，缺失效果的原因和不可装配状态仍可查。验证 `GetEquip` 继续供招式取值，卡脚本使用新生命周期处理法宝效果。
- [x] 4.3 迁移现有装备引用的剑斩、剑刺、剑气和抓击。按设计的首批费用配置核对 1 格和 1 份匹配灵元，逐项验证其 `Contest` 或 `Apply`、装备底值和兼容判定。其余 11 张按结构清理结果记录，并验证它们仍按 `Unmigrated` 处理。通过 `--effect-tests` 或等价的定向用例核对这四个实际定义，区分有效的 `card_form_strike.lua` 和孤立的双下划线脚本。
- [x] 4.4 补齐 `test_npc_a` 的剑斩和剑刺位置及两条 `ParentPlacementIndex = 3` 绑定，按设计补足该测试模板的 SP。用 `dotnet run --project Console/UnityWorld.Console.csproj -- --template-tests` 验证原七张卡连同两张招式全部入包，实际 SP 为 12/12，长剑的攻击 4、防御 3 在被动激活前后都能给招式读取。

## 5. 验收正式入口

这组任务检查卡牌定义、模板预检、背包推进和观察端在同一场战斗中串起来。

- [x] 5.1 构建核心工程与 Godot 客户端，通过现有默认测试入口装配 `test_npc_a` 和 `test_npc_b` 并启动战斗。核对客户端预检和核心装配对相同配置给出一致结果，观察到法术发动、法宝激活或续期、招式对拼及一行只有一次主动发动。
- [x] 5.2 核对迁移后的实际卡表与兼容状态，并运行 `openspec validate main-keyword-combat --type change --strict`。记录首批可用的法宝和招式、仍不可装配的旧内容及其原因，以及装配和战斗验收结果。

## 归档验收

- 控制台定向用例全部通过。Main 与正式模板装配 99 项，背包模型 87 项，背包战斗 88 项，扫描时序 64 项，法宝激活和长剑对拼 52 项，卡牌效果及招式迁移 103 项。
- Godot 编辑器预检 20 项、模板导入导出 33 项通过。测试武修的 9 张卡正式装配和客户端预检均为 12/12 SP，剑斩和剑刺都绑定长剑。缺招式、错父装备及超 SP 会拒绝装配。
- 核心、控制台和 Godot 客户端编译通过，Godot 无界面启动检查通过。默认控制台及客户端观察案例在 345 Tick 正常结束。默认模板使用战斗种子 12346、12347、12348 复跑，均记录到长剑激活和两张剑招发动。四场记录按参战者、轮次、行计数，单行最多一次主动发动。
- 真实长剑用例验证了两次斩击获胜只保留一份 +1。原为 4 点的招式直击仍为 4，下一次双方对拼为 5，再下一次回到 4。缺灵、冻结或行额度用尽时不能续期，已蓄灵元保留。
- 268 张卡均有唯一 Main。38 张 Migrated、48 张 Unmigrated、181 张 Pending、1 张 Deprecated。长剑、剑斩、剑刺、剑气、抓击已验收。铠甲和利爪仍缺效果，其他 11 个招式仍缺确认的费用。纳音 Pending 卡等待实卡效果，金针依赖已废弃的 ManaAdj_Jin。
