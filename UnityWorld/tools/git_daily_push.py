#!/usr/bin/env python3
"""
一键 commit + push 今日的 git 改动。

功能：
  1. 自动 git add 所有改动（新增/修改/删除）
  2. 生成带日期和改动摘要的 commit message
  3. push 到远程

使用方式：
    python tools/git_daily_push.py              # 预览模式（只看改动，不提交）
    python tools/git_daily_push.py --apply      # 实际执行 commit + push
    python tools/git_daily_push.py --no-push    # 只 commit，不 push
    python tools/git_daily_push.py --msg "xxx"  # 自定义 commit 消息前缀
"""

import os
import sys
import subprocess
from datetime import datetime

# ── 配置 ─────────────────────────────────────────────────────────────────────

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.dirname(SCRIPT_DIR)          # UnityWorld/UnityWorld
GIT_ROOT = os.path.dirname(PROJECT_ROOT)             # UnityWorld（.git 所在目录）

# ── 工具函数 ──────────────────────────────────────────────────────────────────

def run_git(*args, capture=True):
    """执行 git 命令，返回 (returncode, stdout)"""
    cmd = ["git", "-C", GIT_ROOT] + list(args)
    result = subprocess.run(cmd, capture_output=capture, text=True, encoding="utf-8")
    return result.returncode, result.stdout.strip()


def get_status():
    """获取 git status --short 的结果，返回分类后的文件列表"""
    code, output = run_git("status", "--short")
    if code != 0:
        return None, None, None

    added = []
    modified = []
    deleted = []

    for line in output.splitlines():
        if not line.strip():
            continue
        status = line[:2].strip()
        filepath = line[3:].strip().strip('"')
        if status in ("??", "A"):
            added.append(filepath)
        elif status in ("M", "MM", "AM"):
            modified.append(filepath)
        elif status == "D":
            deleted.append(filepath)
        else:
            modified.append(filepath)  # 其他状态归入 modified

    return added, modified, deleted


def get_current_branch():
    """获取当前分支名"""
    code, output = run_git("branch", "--show-current")
    return output if code == 0 else "unknown"


def build_commit_message(added, modified, deleted, custom_prefix=None):
    """生成 commit message"""
    today = datetime.now().strftime("%Y-%m-%d")
    total = len(added) + len(modified) + len(deleted)

    # 统计改动涉及的目录（取第一级有意义的目录）
    dirs = set()
    for f in added + modified + deleted:
        parts = f.replace("\\", "/").split("/")
        if len(parts) > 1:
            dirs.add(parts[0])
        else:
            dirs.add(f)

    summary_parts = []
    if added:
        summary_parts.append(f"+{len(added)}")
    if modified:
        summary_parts.append(f"~{len(modified)}")
    if deleted:
        summary_parts.append(f"-{len(deleted)}")
    summary = " ".join(summary_parts)

    dirs_str = ", ".join(sorted(dirs)[:5])  # 最多显示5个目录
    if len(dirs) > 5:
        dirs_str += f" 等{len(dirs)}个目录"

    if custom_prefix:
        msg = f"{custom_prefix} [{today}] ({summary}) [{dirs_str}]"
    else:
        msg = f"daily: {today} ({summary}) [{dirs_str}]"

    return msg


def print_file_list(label, files, symbol):
    """打印文件列表"""
    if not files:
        return
    print(f"\n  {label}（{len(files)} 个）：")
    for f in files[:20]:  # 最多显示20个
        print(f"    {symbol} {f}")
    if len(files) > 20:
        print(f"    ... 还有 {len(files) - 20} 个文件")


# ── 主流程 ────────────────────────────────────────────────────────────────────

def main():
    # 解析参数
    apply_mode = "--apply" in sys.argv
    no_push = "--no-push" in sys.argv
    custom_msg = None
    if "--msg" in sys.argv:
        idx = sys.argv.index("--msg")
        if idx + 1 < len(sys.argv):
            custom_msg = sys.argv[idx + 1]

    # 检查 git 可用性
    code, _ = run_git("status")
    if code != 0:
        print("❌ 当前目录不是 git 仓库，或 git 不可用")
        sys.exit(1)

    # 获取当前分支
    branch = get_current_branch()
    print(f"── Git 每日提交 ──")
    print(f"📁 仓库：{GIT_ROOT}")
    print(f"🌿 分支：{branch}")

    # 获取改动
    added, modified, deleted = get_status()
    if added is None:
        print("❌ 无法获取 git status")
        sys.exit(1)

    total = len(added) + len(modified) + len(deleted)
    if total == 0:
        print("\n✅ 工作区干净，没有需要提交的改动。")
        sys.exit(0)

    # 打印改动摘要
    print(f"\n📊 改动统计：共 {total} 个文件")
    print_file_list("新增", added, "＋")
    print_file_list("修改", modified, "～")
    print_file_list("删除", deleted, "－")

    # 生成 commit message
    msg = build_commit_message(added, modified, deleted, custom_msg)
    print(f"\n💬 Commit 消息：{msg}")

    if not apply_mode:
        print("\n── 预览模式 ──")
        print("以上为预览，未执行任何操作。")
        print("添加 --apply 参数以实际执行 commit + push。")
        print("添加 --no-push 参数只 commit 不 push。")
        print("添加 --msg \"xxx\" 自定义消息前缀。")
        sys.exit(0)

    # 执行 git add
    print("\n⏳ git add -A ...")
    code, output = run_git("add", "-A")
    if code != 0:
        print(f"❌ git add 失败：{output}")
        sys.exit(1)
    print("  ✅ 已暂存所有改动")

    # 执行 git commit
    print(f"⏳ git commit ...")
    code, output = run_git("commit", "-m", msg)
    if code != 0:
        print(f"❌ git commit 失败：{output}")
        sys.exit(1)
    print(f"  ✅ 已提交：{msg}")

    # 执行 git push
    if no_push:
        print("\n── 完成（仅 commit，未 push）──")
    else:
        print(f"⏳ git push origin {branch} ...")
        code, output = run_git("push", "origin", branch)
        if code != 0:
            print(f"⚠️ git push 失败：{output}")
            print("  可以稍后手动执行 git push")
            sys.exit(1)
        print(f"  ✅ 已推送到 origin/{branch}")
        print("\n── 完成 ──")

    print(f"📝 {total} 个文件已提交并推送。")


if __name__ == "__main__":
    main()
