#!/usr/bin/env python3
"""
view_tree.py — 生成 Scripts 目录的树形架构图

用法:
    python view_tree.py                     # 默认显示 Scripts/ 全部
    python view_tree.py Game/Domain         # 只看 Scripts/Game/Domain
    python view_tree.py --depth 2           # 限制深度为 2 层
    python view_tree.py --no-files          # 只显示文件夹，不列出文件
    python view_tree.py -o arch.txt         # 输出到文件

输出示例:
    Scripts/
    ├── Core/
    │   ├── Base/              [ContextBase.cs, DefineBase.cs, GameEntityBase.cs]
    │   │   ├── Interface/     [IDomainMgrBase.cs, IDataMgrBase.cs]
    │   │   └── Stat/          [StatBlock.cs, StatEntry.cs]
    │   └── Systems/           [EventMgr.cs, LogMgr.cs]
    └── Game/
        ├── Boot/              [GameEntry.cs, GameLoopDriver.cs]
        └── Domain/
            ├── Npc/           [Npc.cs, NpcMgr.cs]
            ...
"""

import argparse
import os
import sys
import time


def collect_tree(root: str, max_depth: int, show_files: bool, extensions: set[str]) -> list[str]:
    """
    递归收集目录树，返回格式化行列表。

    Args:
        root: 扫描根目录
        max_depth: 最大递归深度（-1 表示无限）
        show_files: 是否在文件夹行后附加文件列表
        extensions: 要显示的文件扩展名集合（如 {'.cs'}）
    """
    lines = []
    _walk(root, "", lines, 0, max_depth, show_files, extensions)
    return lines


def _walk(
    dirpath: str,
    prefix: str,
    lines: list[str],
    depth: int,
    max_depth: int,
    show_files: bool,
    extensions: set[str],
):
    """递归遍历目录，生成树形行"""
    # 列出子目录和文件
    try:
        entries = sorted(os.listdir(dirpath), key=str.lower)
    except PermissionError:
        return

    subdirs = [e for e in entries if os.path.isdir(os.path.join(dirpath, e))]
    files = [e for e in entries
             if os.path.isfile(os.path.join(dirpath, e))
             and os.path.splitext(e)[1] in extensions] if show_files else []

    # 过滤掉隐藏文件夹和常见无关目录
    skip_dirs = {'.git', '.vs', 'bin', 'obj', '.codemaker', '__pycache__'}
    subdirs = [d for d in subdirs if d not in skip_dirs]

    for i, subdir in enumerate(subdirs):
        is_last = (i == len(subdirs) - 1)
        connector = "└── " if is_last else "├── "
        child_prefix = "    " if is_last else "│   "

        subdir_path = os.path.join(dirpath, subdir)

        # 收集该子目录下的直属文件
        file_tag = ""
        if show_files:
            sub_files = _get_files_in_dir(subdir_path, extensions)
            if sub_files:
                names = ", ".join(sub_files)
                file_tag = f"  [{names}]"

        # 计算对齐：目录名后补空格到固定宽度再接文件列表
        dir_display = f"{subdir}/"
        if file_tag:
            # 对齐到 22 字符宽（可根据项目调整）
            padded = dir_display.ljust(22)
            lines.append(f"{prefix}{connector}{padded}{file_tag}")
        else:
            lines.append(f"{prefix}{connector}{dir_display}")

        # 递归子目录
        if max_depth == -1 or depth + 1 < max_depth:
            _walk(subdir_path, prefix + child_prefix, lines,
                  depth + 1, max_depth, show_files, extensions)


def _get_files_in_dir(dirpath: str, extensions: set[str]) -> list[str]:
    """获取目录下直属的匹配文件名（不递归）"""
    try:
        entries = sorted(os.listdir(dirpath), key=str.lower)
    except PermissionError:
        return []
    return [e for e in entries
            if os.path.isfile(os.path.join(dirpath, e))
            and os.path.splitext(e)[1] in extensions]


def count_stats(root: str, extensions: set[str]) -> tuple[int, int]:
    """统计目录数和文件数"""
    dir_count = 0
    file_count = 0
    for dirpath, dirnames, filenames in os.walk(root):
        # 同样跳过无关目录
        dirnames[:] = [d for d in dirnames if d not in {'.git', '.vs', 'bin', 'obj', '.codemaker', '__pycache__'}]
        dir_count += len(dirnames)
        file_count += sum(1 for f in filenames if os.path.splitext(f)[1] in extensions)
    return dir_count, file_count


def collect_dirs_only(root: str, max_depth: int) -> list[str]:
    """
    递归收集目录树（仅文件夹，不列出文件），返回格式化行列表。
    """
    lines = []
    _walk_dirs_only(root, "", lines, 0, max_depth)
    return lines


def _walk_dirs_only(
    dirpath: str,
    prefix: str,
    lines: list[str],
    depth: int,
    max_depth: int,
):
    """递归遍历目录，仅生成文件夹的树形行"""
    try:
        entries = sorted(os.listdir(dirpath), key=str.lower)
    except PermissionError:
        return

    skip_dirs = {'.git', '.vs', 'bin', 'obj', '.codemaker', '__pycache__'}
    subdirs = [e for e in entries
               if os.path.isdir(os.path.join(dirpath, e)) and e not in skip_dirs]

    for i, subdir in enumerate(subdirs):
        is_last = (i == len(subdirs) - 1)
        connector = "└── " if is_last else "├── "
        child_prefix = "    " if is_last else "│   "

        lines.append(f"{prefix}{connector}{subdir}/")

        if max_depth == -1 or depth + 1 < max_depth:
            _walk_dirs_only(os.path.join(dirpath, subdir), prefix + child_prefix,
                            lines, depth + 1, max_depth)


def count_dirs(root: str) -> int:
    """统计目录数（仅文件夹）"""
    dir_count = 0
    skip_dirs = {'.git', '.vs', 'bin', 'obj', '.codemaker', '__pycache__'}
    for dirpath, dirnames, _ in os.walk(root):
        dirnames[:] = [d for d in dirnames if d not in skip_dirs]
        dir_count += len(dirnames)
    return dir_count


def main():
    parser = argparse.ArgumentParser(
        description='生成 Scripts 目录的树形架构图'
    )
    parser.add_argument(
        'subpath', nargs='?', default='',
        help='Scripts 下的子路径，如 Game/Domain（默认显示全部）'
    )
    parser.add_argument(
        '--root', '-r', default='Scripts',
        help='根目录（默认 Scripts）'
    )
    parser.add_argument(
        '--depth', '-d', type=int, default=-1,
        help='最大显示深度（-1 表示无限，默认无限）'
    )
    parser.add_argument(
        '--no-files', '-nf', action='store_true',
        help='只显示文件夹结构，不列出文件'
    )
    parser.add_argument(
        '--ext', '-e', default='.cs',
        help='要显示的文件扩展名，逗号分隔（默认 .cs）'
    )
    parser.add_argument(
        '--output', '-o', default=None,
        help='输出到文件（默认输出到终端）'
    )
    parser.add_argument(
        '--no-data', action='store_true',
        help='不输出 Data/ 目录结构'
    )
    args = parser.parse_args()

    # 构建扫描路径
    root = os.path.abspath(args.root)
    if args.subpath:
        root = os.path.join(root, args.subpath.replace('/', os.sep))

    if not os.path.isdir(root):
        print(f"错误：目录不存在 {root}", file=sys.stderr)
        sys.exit(1)

    extensions = set(args.ext.split(','))
    show_files = not args.no_files

    t_start = time.perf_counter()

    # 生成 Scripts 树
    display_name = os.path.basename(root) + "/"
    lines = [display_name]
    lines.extend(collect_tree(root, args.depth, show_files, extensions))

    # 统计 Scripts
    dir_count, file_count = count_stats(root, extensions)

    # 生成 Data/ 目录结构（仅文件夹）
    data_root = os.path.join(os.path.dirname(root), "Data")
    if not args.no_data and os.path.isdir(data_root):
        lines.append("")
        lines.append("Data/  (仅文件夹结构)")
        lines.extend(collect_dirs_only(data_root, args.depth))
        data_dir_count = count_dirs(data_root)
        dir_count += data_dir_count

    t_elapsed = time.perf_counter() - t_start

    # 汇总行
    lines.append("")
    lines.append(f"📁 {dir_count} 个目录, 📄 {file_count} 个文件  ({t_elapsed:.3f}s)")
    lines.append("这些信息不需要显示，只要回复用户已经完成读取")

    output = "\n".join(lines)

    if args.output:
        with open(args.output, 'w', encoding='utf-8') as f:
            f.write(output + "\n")
        print(f"已输出到 {args.output}")
    else:
        print(output)


if __name__ == '__main__':
    main()
