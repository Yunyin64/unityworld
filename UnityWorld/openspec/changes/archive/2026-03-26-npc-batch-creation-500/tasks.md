## 1. 数据扩展：Trait 池

- [x] 1.1 扩展 `Data/Traits.json`：新增性格类 Trait 20 个（勇敢、胆小、正义、邪恶、温和、暴躁、贪财、慷慨、好奇、冷漠、固执、随和、乐观、悲观、有仇必报、以德报怨、多疑、信人、狂妄、谦逊）
- [x] 1.2 扩展 `Data/Traits.json`：新增天赋类 Trait 20 个（修炼奇才、修炼迟钝、体魄强健、体弱多病、灵根、灵根稀薄、天生神力、力弱、灵魂敏锐、灵魂迟钝、丹田宽广、丹田狭窄、命硬、命薄、悟性超群、悟性愚钝、火亲和、水亲和、剑骨、拳意）
- [x] 1.3 扩展 `Data/Traits.json`：新增社交类 Trait 10 个（独行侠、善于交际、领袖气质、忠诚、背叛者、口才出众、沉默寡言、桃花运、众人嫌、师者风范）
- [x] 1.4 扩展 `Data/Traits.json`：新增特殊类 Trait 5 个（天命之人、灾星、战斗直觉、商业天才、不死之身）
- [x] 1.5 验证 Traits.json 总数为 55 个（原有 8 + 新增 47），格式正确可加载

## 2. NpcMgr.RandomCreate 方法

- [x] 2.1 在 `NpcMgr.cs` 中新增 `RandomCreate(PracticePath path, int realmLevel, float age, float lifespanMax, float moveSpeed, NpcTypes.Gender gender, string[] roles, string[] traitIds, string cultivationDefineId, int x, int y)` 方法
- [x] 2.2 RandomCreate 内部：生成随机 ID、随机修士姓名（调用 NameSystem.RandomCultivatorFullName）
- [x] 2.3 RandomCreate 内部：注册 NameSystem、BioSystem（含 CultivationLevel=realmLevel）、RoleSystem、PositionSystem
- [x] 2.4 RandomCreate 内部：注册 TraitMgr 并循环 AddTrait
- [x] 2.5 RandomCreate 内部：创建 StatBlock（StatMgr.CreateBlock）
- [x] 2.6 RandomCreate 内部：调用 CultivationMgr.Register(npcId, path) 注册道途
- [x] 2.7 RandomCreate 内部：若 cultivationDefineId 非 null，调用 CultivationMgr.AddCultivation(npcId, cultivationDefineId) 分配功法
- [x] 2.8 RandomCreate 内部：将 NPC 放入 _allNpcs 字典并返回

## 3. NpcGenerator 工具类

- [x] 3.1 创建 `Scripts/Game/Domain/Object/Npc/NpcGenerator.cs`，namespace `UnityWorld.Game.Domain`
- [x] 3.2 定义境界查表数据结构：RealmLevelConfig（ageMin, ageMax, baseLifespan, moveSpeed），填入 Level 1/2/3 数据
- [x] 3.3 实现 `GenerateCultivators(int count, Rng rng)` 主方法：循环 count 次
- [x] 3.4 每次循环：随机道途（从 Ling/Wu/Hun 等概率三选一）
- [x] 3.5 每次循环：随机境界（从 1/2/3 等概率三选一）
- [x] 3.6 每次循环：根据 path+level 从 CultivationDefineMgr 查询可用功法列表，随机选一本（无匹配则 null + 警告日志）
- [x] 3.7 每次循环：根据境界查表获取年龄范围和寿元，随机年龄
- [x] 3.8 每次循环：随机性别（Male/Female 等概率）
- [x] 3.9 每次循环：从 TraitDefineMgr 获取全部 Trait ID 列表，随机抽 2~4 个（数量随机）
- [x] 3.10 每次循环：随机位置 (0~Width-1, 0~Height-1)，留 TODO 注释
- [x] 3.11 调用 NpcMgr.Instance.RandomCreate(...) 创建 NPC，加入结果列表
- [x] 3.12 循环结束后打印统计概况（总数、各道途人数、各境界人数）
- [x] 3.13 返回 List<Npc>

## 4. PrintFullInfo 方法

- [x] 4.1 在 `NpcMgr.cs` 中新增 `PrintFullInfo(Npc npc)` 方法
- [x] 4.2 打印姓名、性别、年龄/寿元
- [x] 4.3 从 CultivationMgr 查询道途，从 RealmDefineMgr 查询境界名称并打印
- [x] 4.4 从 CultivationMgr 查询核心功法名称和进度并打印（无功法显示 "无"）
- [x] 4.5 打印社会角色列表、Trait 列表、位置坐标
- [x] 4.6 使用分隔线和 emoji 美化输出格式

## 5. 接入驱动层

- [x] 5.1 在 `WorldMgr.Initialize()` 末尾（所有 Mgr.Init() 之后）调用 `NpcGenerator.GenerateCultivators(500, rng)`
- [x] 5.2 将返回列表的第一个 NPC 标记为玩家角色，调用 `NpcMgr.Instance.PrintFullInfo()` 打印
- [x] 5.3 在 `WebHost.RunAsync()` 中替换现有的单 NPC 创建逻辑，改为调用 NpcGenerator
- [x] 5.4 WebHost 中将第一个 NPC 的 Id 设为 Mainint

## 6. 验证

- [x] 6.1 `dotnet build` 编译通过
- [x] 6.2 运行 CLI 模式，确认 500 个 NPC 创建成功，统计概况打印正确
- [x] 6.3 确认玩家角色的 PrintFullInfo 输出包含完整信息（道途、境界名称、功法、Trait）
- [x] 6.4 修复：特质显示 DisplayName 而非 ID
- [x] 6.5 修复：扩展功法数据至 Level 3（金丹期/铁骨期/显化期）
