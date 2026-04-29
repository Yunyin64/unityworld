## Context

当前 `NpcMgr.Birth()` 只调用了 `BioSystem.OnEntityBorn(ctx)` 后返回 null。各 NpcSystem 的 `OnEntityBorn` 均为空实现（继承自 `NpcSystemBase` 的默认空方法）。`BirthContext` 只是空的 `ContextBase` 子类。

Birth 流程需要三步协作：
1. NpcMgr 分配 ID、创建 Npc 实例
2. GlyphMgr 作为玩法系统往 ctx kv 塞入简单值（姓名、性别）
3. 各 NpcSystem.OnEntityBorn 消费 ctx、创建并注册各自的 Data

## Goals / Non-Goals

**Goals:**
- 实现完整的 Birth 编排流程（NpcMgr.Birth）
- BirthContext 持有 MainNpc 固定字段
- BioSystem.OnEntityBorn 具体实现：从 ctx kv 读取姓名/性别，填充 NpcBioData 并 Register
- 其他系统 OnEntityBorn 做最小占位（new Data + Register）
- Birth 完成后 NPC 加入 _allEntities，可被 Tick 驱动

**Non-Goals:**
- CultivationSystem、CardSystem 的具体初始化（后续变更）
- NpcDefine 模板驱动（不使用）
- 事件广播（Birth 事件暂不发）
- 战斗系统集成

## Decisions

### 1. BirthContext.MainNpc 作为固定字段
- **选择**: 直接在 BirthContext 上声明 `public Npc MainNpc` 字段
- **理由**: 每次 Birth 必有主角 NPC，固定字段比 kv 更类型安全
- **替代方案**: 继续用 `ctx.Set("Self", npc)` / `ctx.Get<Npc>("Self")` → 隐式约定，易出错

### 2. GlyphMgr 铭刻 → System 消费的两阶段模式
- **选择**: GlyphMgr 只往 ctx kv 塞简单值，BioSystem 从 kv 取值组装 Data
- **理由**: GlyphMgr 是跨实体的玩法系统（不只服务 NPC），不应直接操作 NpcBioData；各 System 自治管理自己的 Data

### 3. ID 由 NpcMgr.Soul.NewId() 生成
- **选择**: NpcMgr 作为"造物主"统一分配 NPC ID
- **理由**: 保证种子可复现性，所有 NPC 的 ID 序列由 Mgr 的 Soul 种子决定

### 4. 各系统 OnEntityBorn 自行创建并 Register Data
- **选择**: 每个系统在 OnEntityBorn 内 new Data + Register，不由 Birth 统一创建
- **理由**: 系统自治，每个系统知道自己的 Data 需要什么初始值

### 5. NpcType 默认 Human
- **选择**: Bio 初始化时 NpcType 硬编码为 `NpcTypes.NpcType.Human`
- **理由**: 第一版只需人类 NPC，后续通过 ctx kv 覆盖

## Risks / Trade-offs

- [OnEntityBorn 调用顺序] 各系统 Born 的顺序可能产生隐式依赖 → 当前与 Tick 保持一致顺序，后续发现依赖问题再调整
- [占位系统未实现] 其他系统只 new 空 Data → Tick 中访问这些 Data 的字段可能得到默认值 → 可接受，不影响 Bio 层验证