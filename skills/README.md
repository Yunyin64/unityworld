# 共享技能库

项目自建技能的唯一源文件放在本目录，格式为 `<技能名>/SKILL.md`。

## Codex 项目技能

仓库根目录的 `.agents/skills` 整体链接到本目录。新增技能时，直接把完整目录放进 `skills/`。例如，`skills/plan-fiction/SKILL.md` 会直接出现在 `.agents/skills/plan-fiction/SKILL.md`，从仓库根目录或 `UnityWorld/` 内启动 Codex 都能发现。已有的 `generator_pack` 嵌套结构也可以直接扫描。

目录链接是本机设置，源文件随 Git 检出。新检出缺少入口时，创建 `.agents` 目录，然后在仓库根目录的 Windows 命令提示符一次性运行以下命令。Windows 创建符号链接需要开发者模式或管理员权限。

```cmd
mklink /D ".agents\skills" "..\skills"
```

如果 Codex 的技能列表没有刷新，重启 Codex。

## Claude 项目技能

Claude 的项目入口在 `UnityWorld/.claude/skills`。要接入新技能，在该目录建立指向 `skills/` 中对应技能的目录链接。

共享技能应避免依赖特定 Agent 的工具名称；本次迁移保留所有原文，尚未改写旧技能里的 Bash 等约定。

## 在其他项目使用随机摘录技能

`extract-md-excerpt/` 是可独立分发的完整技能包，包含 `SKILL.md` 和 `scripts/extract_md_excerpt.py`。需要 Python 3.10+，无第三方依赖；复制或提交时保留整个文件夹即可，不需要 UnityWorld 的其他文件。

- 仅用于另一个项目：将整个文件夹放入该项目的 `.agents/skills/`（Codex）或 `.claude/skills/`（Claude），随目标项目提交。
- 本机多个项目共用时，将整个文件夹放入 `%USERPROFILE%\.agents\skills`（Codex）或 `%USERPROFILE%\.claude\skills`（Claude）。

在本仓库提交 `skills/extract-md-excerpt/` 下的完整源文件。技能按安装位置查找脚本，输入文档可以在任意项目目录中。
