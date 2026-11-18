# 原型核查与接入顺序

核查范围是当前工作树的 C#、Godot 客户端和游戏数据。源码事实沿用前次静态核查，右栏与下方接入步骤按本次提案更新。这次没有运行客户端、测量手感或验证多人服务。文中行号取自本次核查，路径从仓库根目录算起。

| 环节 | 当前源码能证明什么 | 接入本案还缺什么 |
|---|---|---|
| 建人开局 | 探索会话会在主位面出生一个 NPC。核心另有从定义装配 NPC 和模板背包的入口，适合建立固定 NPC 对手。见 `scripts/client/exploration/ExplorationSession.cs` 第 34 行、`UnityWorld/Scripts/Game/Domain/Object/Npc/NpcMgr.cs` 第 96、135、242 行。 | 按提案，首徒和初始六名弟子使用高资质半固定模板，固定成长与起手配置，只随机名字和外观。出生后绑定玩家或 AI 宗门，保存人物模板、16 岁入门年龄、已结算的培养年度，以及前六次招徒的序号和已生成角色。前六次每次 2 石，之后每次 8 石，所有招徒共享每战略回合一人的上限。失败不扣费也不推进 initial_recruit_index，重复提交返回已有结果。现有 `NpcMgr.Save/Load` 第 82–89 行均直接结束，持久化需单独验证。 |
| 遭遇与功法 | 本格探索读取所在格的遇敌表，随机抽一项，按概率触发，可发行为卡。功法按显式点数解锁节点，跨过阈值可发术法卡。见 `UnityWorld/Scripts/Game/Domain/GamePlay/Behavior/ExploreEncounterData.cs` 第 64–81 行、`UnityWorld/Scripts/Game/Domain/GamePlay/Practice/CultivationMgr.cs` 第 39–57、83–100 行。 | 新手操作引导按进度读取固定事件。早期每个本段游戏年基础修炼统一结算一次 4 × 2 = 8 修为，探索与 NPC 战穿插其中，行动收益不能重复入账。常规培养按行动基础收益乘练气资质倍数。初次授法要明确传入初始点数，当前接口省略点数会发满级功法。功法点、个人境界进度要分别记账。 |
| 年龄与境界 | 生成年龄累计字段从 0 开始，寿元上限字段默认 80。灵修表有练气、筑基、金丹，另外两条修行路线使用各自的境界名。见 `UnityWorld/Scripts/Game/Domain/Object/Npc/Systems/NpcSystemBio.cs` 第 36 行、`UnityWorld/Scripts/Game/Domain/Object/Npc/Data/NpcCultivationData.cs` 第 68 行、`UnityWorld/Data/Practice/RealmDefines.json` 第 3–27、30–81 行。 | 培养入口设16岁。练气potential_deadline_age取60且严格小于，基础寿命独立取300；达到60先结算潜力耗尽，人物继续存活。高阶段潜力线150/400/1000与寿命600/1500/3000均为初调。寿命由当前境界基础值加累积延寿计算，突破保留延寿，延寿不清除潜力耗尽。补每段结果、年龄只提交一次和等待状态，元婴突破化神后仍可经营。已有默认80岁字段需有明确的数据迁移与旧档校验。 |
| 地图与时间 | 地图生成用六个邻接方向；行动候选再过滤可步行邻格。移动用速度积累进度，到达 100 才换格。世界历一年 50 周，探索客户端启动合法行程可推进一周。见 `UnityWorld/Scripts/Game/Domain/Object/Plane/Generation/WorldMapGenerator.cs` 第 78 行、`UnityWorld/Scripts/Game/Domain/Object/Npc/Systems/NpcSystemPosition.cs` 第 81–95 行、`UnityWorld/Scripts/Game/Domain/GamePlay/Behavior/Behaviors/MoveBehavior.cs` 第 17–35 行、`UnityWorld/Scripts/Game/World/WorldDate.cs` 第 10–12 行、`scripts/client/exploration/ExplorationSession.cs` 第 69–81 行。 | 出生地点须容得下六个可经营的邻格，挑选中心时检查地形与相邻出生圈。开宗只建立中央山门归属，邻接六格的持有人为空，产出为零，后续由六名额外筑基逐格到达占领。个人培养推进本段局部年龄，在结果提交时写回角色一次。战略回合独立调度，长度待调，不等于所有人加一岁。经营老化初调为ready角色每现实24小时增加一岁，培养和pending期间暂停该计数。底层50周日历不能直接承担点击一次培养后推进全世界的功能。移动改由独立战略回合推进，培养计岁不触发周AI。 |
| AI 与地权 | NPC 有显式移动、探索、工作三个周行动入口，空闲时会挑选主行动。地图有地区查询。NPC 的派系类仍是占位，列出了职位、势力关系等未来字段。见 `UnityWorld/Scripts/Game/Domain/Object/Npc/Systems/NpcSystemBehavior.Weekly.cs` 第 30–38、168–181 行、`UnityWorld/Scripts/Game/Domain/Object/Region/RegionMgr.cs` 第 28 行、`UnityWorld/Scripts/Game/Domain/Object/Npc/Data/NpcFactionData.cs` 第 4–20 行。 | 宗门账户、招募、可争夺地块的持有人、驻守弟子、战争与核心结算都需要业务规则。创派库房初始 8 灵石，山门每战略回合产 2 灵石。玩家通过招徒按钮招收前六名预设成长的弟子，每次花 2 灵石；之后普通随机招徒每次 8 灵石。地块收入同时检查已占领、角色ready与实际岗位，开始培养或进入等待会解除该人岗位，旧防守分身失效。山门固定资源流与个人岗位加成分别记账。AI 玩家要能用同一命令收徒和经营；地图生成时的地区成员关系仍供地形查询使用。 |
| 背包装配、模板战与同龄切磋 | NPC 有主修功法、卡牌位置和布局校验。模板试战输入可指定两队、世界种子及战斗种子；草稿快照会复制定义并算哈希。观察器驱动战斗 Tick、读取结果、投影战报。见 `UnityWorld/Scripts/Game/Domain/Object/Npc/Systems/NpcSystemBackpack.cs` 第 150–158、175–191 行、`scripts/client/templates/TemplateTrialService.cs` 第 11、31–52 行、`scripts/client/battle/cases/BattleCase.cs` 第 26–32 行、`scripts/client/battle/BattleObserverSession.cs` 第 132–156 行。 | 从实际培养角色导出可战斗的布局、等级、属性和版本号，另存宗门、境界及封存年龄。观战画面保存的是某一帧的显示状态；配对需要开打前可重新装配的对手档案。首徒与初始六名弟子在 20、24 岁使用指定模板战，节点按本段年龄触发，未达潜力截止年龄时先战后突破，达到潜力线先结算potential_exhausted。匹配解锁独立于普通招徒。六名初始弟子均筑基且首次实际守满山门与原始六邻格后写入永久标记，下一场新建对局才走普通异步配对；此前其他修士战按年龄境界实例化模板。普通配对增加发布、同龄筛选、缺人回退和战果防重。模板散列仅验证定义内容。 |

## 界面观察

现有战斗以独立的 NPC 和背包副本运行。伤口会占格，战斗内也有移除伤口的操作。要让一次落败影响后续培养，还需把本场留下的伤口随战果写回该弟子，并让「疗伤」行动在培养时间线上处理这些卡。本次静态核查见 `UnityWorld/Scripts/Game/Domain/Combat/CombatNpc/CombatNpc.cs`、`UnityWorld/Scripts/Game/Domain/Combat/CombatNpc/CombatNpcFunc.cs`、`UnityWorld/Scripts/Game/Domain/Combat/CombatResult.cs`。

探索界面的首次点格更新详情，同一合法邻格再次点击会启程。途中能自动续周，也能手动前进。练气教程可沿用格子选中与详情显示，把同格行动直接放在详情里；锁定范围时应保留查看邻格和查看筑基条件的操作。宗门地图要另行区分查看、派驻和出征，避免第二次点格误下战略命令。对应 `scripts/client/ui/exploration/ExplorationView.cs` 第 55–69、89–111 行。按钮辨识、误触率和战报可读性需要真人试玩。

## 战争规则的本次静态核查

当前通用战斗入口合并参战者后直接创建场景，模板试战校验阵营、快照和装配。查看的入口没有大境界准入或高境界直接压制的判定。模板校验器还提示当前出生流程未消费 InitCultivationLevel，因此仅在模板里填这个字段不能证明境界已参与战斗。对应 UnityWorld/Scripts/Game/Domain/Combat/CombatMgr.cs 第 83–91 行、scripts/client/battle/cases/BattleCase.cs 第 200–241 行、scripts/client/templates/TemplateEditorValidator.cs 第 163 行。

已有时长控制是战斗逻辑 Tick。模板试战默认上限 5000 Tick，观察器单倍每秒推进 10 Tick，并支持暂停和单步；核心 Run 会循环模拟直到结束。达到 Tick 上限结束本场，仍有双方存活时不会自动生成胜方。世界物理秒由 Tick 累积，周历由命令推进。本次查看的世界、战斗和客户端入口没有可直接复用的权威现实时间地权等待流程。对应 scripts/client/templates/TemplateTrialService.cs 第 11–12 行、scripts/client/battle/BattleObserverSession.cs 第 53、109–139 行、UnityWorld/Scripts/Game/Domain/Combat/CombatScene.cs 第 230–244、303–349 行、UnityWorld/Scripts/Game/World/WorldTime.cs 第 27–37 行。

本次战争方向仍是设计要求。绝对境界压制需要接到战争准入和结果结算，同境界战斗可以沿用已有模板模拟。地权僵持需要独立保存现实开始、截止时间和当时的控制状态，离线后能继续核对，播放暂停与模拟步数不改变截止时间。低阶段参与的临时收益要单独记录有效期间，并随现实战线变化结算。核心目标同时表达元婴镇守条件和外围金丹战线，后续验收应覆盖高阶到场后的压制、僵持到时的地权变更及临时收益停止。这些接入点本次只做静态评估，尚未运行战争流程。

## 下一版原型按这个顺序接

1. **先让一段培养能结束。** 首徒16岁进入练气，二十岁32修为打固定NPC战，二十四岁64修为后突破并提交本段结果。另建五十九岁与六十岁的练气档，验证五十九岁可突破，六十岁先potential_exhausted且继续存活。已有寿元字段与新增潜力字段分别保存，旧档载入不能把年龄60判为死亡。
2. **让现实等待可恢复。** 采用常规等待，测试16→24等待8小时、24→42等待18小时、16→60潜力耗尽等待44小时。重复提交、离线、重进与多端并发都沿用同一个stage_run_id和ready_at。等待不重复增加年龄，尚未到时不能任职、出征、产出或开下一段。到ready_at逻辑转ready并开始经营老化，离线后晚领要按游标补算年龄与小数余量，不能继续冻龄。角色实际派驻前没有岗位产出。
3. **核对同一个角色的岗位。** 给一位筑基安排药圃岗位，再开始筑基培养。开始操作成功时解除旧岗位与实战驻防，下一次地块收入只检查当前在岗人。培养结束后的pending继续保持不可用，ready后重新派驻。元婴突破化神走同样结果和等待流程，完成后仍在名册中可经营。
4. **分开个人年龄和世界进度。** 一个角色连续操作八个培养年，核对其他人物年龄、战略回合和世界周调度均未被这八次操作推动。ready经营角色按独立实时速率补算自然年龄，开始新段前先结算已经过的经营时间。培养与pending期间不会又叠一份经营老化。
5. **验证延寿与长期保留。** 给已耗尽练气潜力的六十岁人物使用两份小延寿丹和一份归元丹，库存各扣一次，寿命上限由300变460，潜力耗尽保持。再用长生药变760。重复请求不多扣库存、不重复加寿命，已死亡者不能用药复活。
6. **再让宗门铺开。** 库房初始8石，前六次招徒各2石，之后8石。新招人物从untrained开始，可进入首段，不能直接任职或操作地图，首段结束后才经pending或零等待直接转ready。按战略回合招募、按本段培养年成长、按现实时间等待，各自记录。人物ready后才派驻，两次战略移动后筑基到邻格，地块收入依据实际岗位结算。六名初始弟子曾筑基且山门与原始六邻格首次实际守满后，写永久匹配解锁标记，下一场新建对局切换。
7. **验证两家争地与后方经营。** 真人和AI都不能用cultivating或pending角色驻防和出战。同回合争同一块地，结果只有一个持有人与一份驻守。核心城池、奇观作为战区或联盟目标的后续载体，计分周期和胜利门槛待定，普通宗门可持续培养和经营。

复合免等待规则为draft，检查开服窗口、金丹条件、允许阶段与去重人数额度。真实参数留空时使用常规等待。独立fixture可以使用72小时、每宗7人来测并发准入，reserved与used都计人数，同一人物再开段重新检查窗口和进程。本段入段时原子锁定资格与规则版本，途中条件关闭不改变已经锁定的这段，下一段重新判定。上述阶段结束、等待、延寿和经营时间均为0.3.0策划接入要求，静态源码核查尚不能证明这些完整流程已经运行。
