## 1. TagDefine 扩展

- [x] 1.1 在 `TagDefine.cs` 中添加 `Parent` 字段（`string?` 类型）
- [x] 1.2 在 `TagDefine.cs` 中添加 `ConflictTags` 字段（`List<string>` 类型，默认空列表）
- [x] 1.3 确保字段有正确的 XML 注释

## 2. TagDefineMgr 多文件加载

- [x] 2.1 修改 `TagDefineMgr` 构造函数，改为接收目录路径而非文件路径
- [x] 2.2 实现 `LoadFromDirectory()` 方法，递归扫描目录下所有 `.json` 文件
- [x] 2.3 实现 `LoadJsonFile()` 方法，加载单个 JSON 文件并解析为 `List<TagDefine>`
- [x] 2.4 实现加载时的 ID 重复检测，输出警告并采用覆盖策略
- [x] 2.5 实现加载错误容错，单个文件失败不影响其他文件
- [x] 2.6 更新 `Init()` 方法调用新的加载逻辑

## 3. TagDefineMgr 层级索引构建

- [x] 3.1 添加 `_childrenMap` 字段（`Dictionary<string, List<string>>`）
- [x] 3.2 添加 `_depthCache` 字段（`Dictionary<string, int>`）
- [x] 3.3 实现 `BuildHierarchyIndex()` 方法，在加载完成后构建索引
- [x] 3.4 实现循环引用检测，检测到循环时输出警告并将 Depth 设为 0
- [x] 3.5 实现孤儿 Tag 检测，Parent 不存在时输出警告并将 Depth 设为 0

## 4. TagDefineMgr 层级查询方法

- [x] 4.1 实现 `GetDepth(string tagId)` 方法
- [x] 4.2 实现 `GetParent(string tagId)` 方法
- [x] 4.3 实现 `GetChildren(string tagId)` 方法
- [x] 4.4 实现 `GetAncestors(string tagId)` 方法
- [x] 4.5 实现 `GetDescendants(string tagId)` 方法
- [x] 4.6 实现 `GetByDepth(int depth)` 方法
- [x] 4.7 实现 `IsConflict(string tagId1, string tagId2)` 方法

## 5. TagMgr 初始化

- [x] 5.1 实现 `TagMgr.Init()` 方法（当前抛出 NotImplementedException）
- [x] 5.2 实现 `TagMgr.Begin()` 方法的空实现（移除 NotImplementedException）
- [x] 5.3 实现 `TagMgr.End()` 方法的空实现（移除 NotImplementedException）
- [x] 5.4 为 `TagMgr.Name` 和 `TagMgr.Desc` 提供实际值

## 6. 数据文件迁移

- [x] 6.1 重命名目录：`Data/Tag/Object` → `Data/Tag/Entity`
- [x] 6.2 重命名目录：`Data/Tag/xuni` → `Data/Tag/Virtual`
- [x] 6.3 创建 `Data/Tag/Core/CoreTag.json` 并填入元分类 Tag
- [x] 6.4 创建 `Data/Tag/Core/ElementTag.json` 并填入五行 Tag
- [x] 6.5 创建 `Data/Tag/Virtual/StatusTag.json` 并迁移状态 Tag
- [x] 6.6 创建 `Data/Tag/Virtual/CombatTag.json` 并迁移战斗 Tag
- [x] 6.7 清空或删除旧的 `Data/TagDefines.json`（保留备份）

## 7. GameDataMgr 集成

- [x] 7.1 更新 `GameDataMgr` 中 `TagDefineMgr` 的初始化路径，从单文件改为目录
- [x] 7.2 验证游戏启动时 Tag 加载正常

## 8. 验证与测试

- [x] 8.1 编译确保无错误
- [x] 8.2 验证多文件加载功能正常
- [x] 8.3 验证层级查询方法返回正确结果
- [x] 8.4 验证冲突查询方法返回正确结果
- [x] 8.5 验证循环引用检测和警告输出
