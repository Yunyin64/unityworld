# 共享技能库

项目自建技能的唯一源文件放在本目录，格式为 `<技能名>/SKILL.md`。
修改这里的文件，Codex 和 Claude 的符号链接入口会同时读取更新。

新增技能后，从仓库根目录运行：

```powershell
pwsh -ExecutionPolicy Bypass -File ./skills/Connect-Skills.ps1
```

项目入口为外层 `.agents/skills`（Codex）和 `UnityWorld/.claude/skills`（Claude）。
Codex 在内部目录启动时也会向上发现外层技能。Claude 原有调用位置保持不变。
`generator_pack` 保留原始整包结构；Codex 链接其中的实际技能目录。

跨项目的个人技能放在 `%USERPROFILE%\AgentSkills\<技能名>\SKILL.md`，然后运行同一脚本并添加 `-Scope Global`。
该模式接入 `%USERPROFILE%\.agents\skills` 和 `%USERPROFILE%\.claude\skills`。

脚本可重复运行，不覆盖现有冲突入口。OpenSpec 等工具管理的现有技能保留原位。
Windows 创建符号链接需要管理员权限或开发者模式。链接使用本机绝对路径；换机器后应重新运行脚本。
源文件纳入 Git；本机链接不作为可移植安装包。应用未刷新技能列表时重启应用。

共享技能应避免依赖特定 Agent 的工具名称；本次迁移保留所有原文，尚未改写旧技能里的 Bash 等约定。

## 在其他项目使用随机摘录技能

`extract-md-excerpt/` 是可独立分发的完整技能包，包含 `SKILL.md` 和 `scripts/extract_md_excerpt.py`。需要 Python 3.10+，无第三方依赖；复制或提交时保留整个文件夹即可，不需要 UnityWorld 的其他文件。

- 仅用于另一个项目：将整个文件夹放入该项目的 `.agents/skills/`（Codex）或 `.claude/skills/`（Claude），随目标项目提交。
- 本机多个项目共用且 Codex/Claude 共用一份：将整个文件夹放入 `%USERPROFILE%\AgentSkills\extract-md-excerpt`，然后从本仓库根目录运行 `pwsh -ExecutionPolicy Bypass -File ./skills/Connect-Skills.ps1 -Scope Global`。两边入口都链接到同一份全局源文件；全局源与本仓库源是独立安装，后续更新需同步技能包。

在本仓库提交 `skills/extract-md-excerpt/` 下的完整源文件；本机生成的入口链接保持忽略。其他人检出后运行项目连接命令即可建立自己的入口。技能按安装位置查找脚本，输入文档可以在任意项目目录中。
