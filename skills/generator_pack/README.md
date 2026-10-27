# generator_pack — 文件夹导览

本目录包含知识库的**生成规范、查询规范、索引工具和文档模板**。

---

## 规范文档

| 文件 | 适用场景 | 一句话说明 |
|------|----------|------------|
| `SPEC-write.md` | 生成/写入角色 | 创建或更新知识 MD 的完整规范（命名、summary、索引更新流程） |
| `SPEC-business-module.md` | 生成业务模块时 | 从 UI 入口出发端到端追踪业务链路的补充规范 |
| `GUIDE-design-data-location.md` | 涉及策划配置数据时 | 策划表三层模型（Excel→Lua 全局表→运行时）的定位方法 |

> 查询/读取规范已整合进 `ENTRY.md`，不再需要单独加载 SPEC-read.md。

---

## 索引工具脚本

| 文件 | 说明 |
|------|------|
| `tokenizer.py` | 分词核心：jieba（中文）+ 正则（英文/代码标识符）+ 位置加权 TF + bigram |
| `kb_index_write.py` | 索引写入 v5：纯文本输出，比例制关键词预算 + 目录级 TF-IDF，支持 `--rebuild` / `--rebuild-entries`，参数 `--min-keywords` / `--max-total-keywords` / `--token-ratio` |
| `kb_index_query.ps1` | 索引查询（PowerShell）：grep + 自动读 entry.md，一步返回完整文档清单 |
| `requirements.txt` | Python 依赖（jieba, pytest，仅构建时需要） |
| `tests/` | 自动化测试（pytest），覆盖 tokenizer / indexer / query |

---

## 文档模板与示例

| 路径 | 说明 |
|------|------|
| `templates/template.module.md` | Module 文档模板 |
| `templates/template.flow.md` | Flow 文档模板 |
| `templates/template.slice.md` | Slice 文档模板 |
| `templates/template.concept.md` | Concept 文档模板 |
| `demos/demo.arkpipe.module.md` | Module 示例（ArkPipe） |
| `demos/demo.arkpipe.rpc_receive_dispatch.flow.md` | Flow 示例 |
| `demos/demo.arkpipe.rpc_registration_state.slice.md` | Slice 示例 |
| `demos/demo.concept.inter_process_communication.md` | Concept 示例 |

---

## 快速上手

**生成新知识文档：** 读 `SPEC-write.md` → 选对应模板 → 参考 demo → 生成后执行 `--rebuild`

**查询已有知识：** 读 `ENTRY.md` → 用 `kb_index_query.ps1` 搜关键词
