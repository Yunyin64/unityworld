## Context

Tag 系统是游戏世界语义描述的核心基础设施。当前实现：
- `TagDefine` 仅包含 `ID`、`DisplayName`、`Desc` 三个字段
- 数据存储在单一文件 `Data/TagDefines.json`
- `TagDefineMgr` 只提供基础查询（Get、GetAll、Contains）
- `TagMgr` 已实现核心匹配算法，但生命周期方法未完成

设计哲学中，Tag 是世界语言的"字"，需要支撑三层创作权限梯度：
- Depth 1（元分类）→ 人类配置
- Depth 2（类别）→ AI + 人类审核
- Depth 3+（语义Tag）→ AI + AI 自检

## Goals / Non-Goals

**Goals:**
- 实现 Tag 的树状层级结构（通过 Parent 字段）
- 实现冲突关系的显式声明（ConflictTags 字段）
- 将数据存储从单文件迁移到多文件目录结构
- 为 TagDefineMgr 添加层级查询能力
- 保持现有 TagMgr.Match() 匹配逻辑不变

**Non-Goals:**
- 不实现父子 Tag 的继承匹配（如查询"火"自动匹配"燃烧"）
- 不自动传递冲突关系到子 Tag
- 不修改 Trigger/Action/Condition 中的 conflictTags 语义
- 不实现 Tag 的运行时动态增删（本期仅静态定义）

## Decisions

### D1：Tag 层级表达方式

**决策**：单 Parent 字段，树状结构

**理由**：
- 每个 Tag 只有一个父节点，结构清晰
- 简单字段比嵌套配置更易维护
- 符合现有 DefineBase 的扁平设计风格

**备选方案**：
- `Domain` + `Category` 字段：需要预定义分类，不够灵活
- 嵌套 JSON 结构：读写复杂，不利于 AI 生成

### D2：数据文件组织方式

**决策**：按一级元分类分目录，每文件多个 Tag

```
Data/Tag/
  ├── Core/ElementTag.json      # 五行 + 火水金木土
  ├── Virtual/StatusTag.json    # 状态 + 燃烧冻结...
  └── ...
```

**理由**：
- 目录结构直观反映创作权限梯度
- 文件粒度适中，便于分批管理
- AI 可按分类增量生成文件

**备选方案**：
- 单文件存储：Tag 数量增长后难以维护
- 每 Tag 一个文件：文件数爆炸

### D3：层级深度计算方式

**决策**：运行时通过 Parent 链条计算，不存储 Depth

**理由**：
- 避免数据冗余和不一致
- Tag 数量不大（预估 <500），计算开销可接受
- 可通过启动时构建索引优化

**备选方案**：
- 存储 Depth 字段：需在 Parent 变更时同步更新

### D4：冲突关系存储位置

**决策**：在 TagDefine 中存储 ConflictTags，显式声明

**理由**：
- 冲突是 Tag 的语义属性，应在定义层声明
- 人工配置场景有限，不需要自动继承
- 保持简单，避免复杂推理

## Risks / Trade-offs

**[R1] 循环引用风险**
- Parent 链条可能出现循环（A → B → C → A）
- **缓解**：加载时校验，检测到循环则警告并跳过该 Tag

**[R2] 孤儿 Tag 风险**
- Tag 的 Parent 不存在于系统中
- **缓解**：加载时校验，警告但不阻止加载，孤儿 Tag 的 Depth 视为 0

**[R3] ID 重复风险**
- 多个文件中可能定义相同 ID 的 Tag
- **缓解**：后加载的覆盖先加载的，并输出警告日志

**[R4] 性能权衡**
- 每次调用 GetDepth 都需遍历 Parent 链条，有 O(h) 开销
- **缓解**：在 Init 时构建 `_depthCache` 缓存，后续 O(1) 查询

## Migration Plan

**Phase 1：代码准备**
1. 扩展 `TagDefine` 添加 Parent、ConflictTags 字段
2. 重构 `TagDefineMgr` 支持多文件加载
3. 添加层级查询方法
4. 实现 `TagMgr.Init()`

**Phase 2：数据迁移**
1. 创建目录结构 `Data/Tag/Core/`、`Data/Tag/Entity/`、`Data/Tag/Virtual/`
2. 将现有 TagDefines.json 内容拆分到对应文件
3. 重命名目录：`Object` → `Entity`，`xuni` → `Virtual`
4. 更新 `GameDataMgr` 中的初始化路径

**Phase 3：清理**
1. 删除 `Data/TagDefines.json`
2. 验证所有 Tag 查询正常工作

**Rollback**：保留 TagDefines.json 副本，必要时可回退代码和数据

## Open Questions

- [ ] 层级索引缓存策略：启动时全量构建 vs 懒加载？（倾向于启动时构建）
- [ ] 是否需要支持 Tag 的运行时热重载？（暂不需要）
