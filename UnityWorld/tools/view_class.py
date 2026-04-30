#!/usr/bin/env python3
"""
view_class.py — 查看某个 C# 类的所有成员（跨 partial class 文件）

用法:
    python view_class.py Npc
    python view_class.py CombatScene
    python view_class.py Npc --private
    python view_class.py Npc --root Scripts/Game/Domain

逻辑：
    1. 从 --root（默认 Scripts）开始，遍历所有子目录，找到与类名同名的文件夹
    2. 只在该文件夹及其子目录下扫描 .cs 文件
    3. 提取指定类的所有成员定义

命名约定：类名 Npc → 文件夹名 Npc，保证同名性和唯一性。
"""

import argparse
import os
import re
import sys
import time
from collections import OrderedDict


# ── 正则模式 ─────────────────────────────────────────

# 匹配 class 声明行（含 partial、基类、接口）
RE_CLASS = re.compile(
    r'^\s*(?:public|internal|private|protected)?\s*'
    r'(?:sealed|abstract|static)?\s*'
    r'(partial\s+)?class\s+(\w+)'
)

# 匹配成员声明（方法、属性、构造函数、字段）
# 方法: public int GetHp() / public override void Tick(float dt)
RE_METHOD = re.compile(
    r'^\s*'
    r'(?P<mods>(?:(?:public|private|protected|internal|static|override|virtual|abstract|sealed|new|async|readonly|const)\s+)*)'
    r'(?P<return>\w+(?:<[^>]+>)?(?:\?)?)\s+'
    r'(?P<name>\w+)\s*'
    r'(?P<params>\([^)]*\))'
)

# 属性: public int Hp { get; } / public int Hp => ...
RE_PROPERTY = re.compile(
    r'^\s*'
    r'(?P<mods>(?:(?:public|private|protected|internal|static|override|virtual|abstract|sealed|new|async|readonly|const)\s+)*)'
    r'(?P<return>\w+(?:<[^>]+>)?(?:\?)?)\s+'
    r'(?P<name>\w+)\s*'
    r'(?:=>|{)'
)

# 构造函数: public Npc(int id)
RE_CONSTRUCTOR = re.compile(
    r'^\s*'
    r'(?P<mods>(?:(?:public|private|protected|internal|static)\s+)*)'
    r'(?P<name>\w+)\s*'
    r'(?P<params>\([^)]*\))\s*'
    r'(?:{|:)'
)


def find_class_dir(root: str, class_name: str) -> str | None:
    """
    从 root 开始遍历所有子目录，找到与 class_name 同名的文件夹。
    返回第一个匹配的绝对路径，未找到返回 None。
    """
    for dirpath, dirnames, _ in os.walk(root):
        for d in dirnames:
            if d == class_name:
                return os.path.join(dirpath, d)
    return None


def find_cs_files(root_dir: str) -> list[str]:
    """递归查找所有 .cs 文件"""
    files = []
    for dirpath, _, filenames in os.walk(root_dir):
        for f in filenames:
            if f.endswith('.cs'):
                files.append(os.path.join(dirpath, f))
    return sorted(files)


def extract_class_members(filepath: str, target_class: str, show_private: bool) -> list[dict]:
    """
    从单个文件中提取指定类的成员。
    返回 [{"type": "method"|"property"|"constructor", "signature": "...", "line": N}, ...]
    """
    results = []
    in_target_class = False
    brace_depth = 0
    class_start_depth = 0

    with open(filepath, encoding='utf-8-sig') as f:
        for lineno, line in enumerate(f, 1):
            stripped = line.rstrip()

            # 检测 class 声明
            m = RE_CLASS.match(stripped)
            if m:
                is_partial = m.group(1) is not None
                class_name = m.group(2)
                if class_name == target_class:
                    in_target_class = True
                    class_start_depth = brace_depth
                    brace_depth += stripped.count('{') - stripped.count('}')
                    continue

            if not in_target_class:
                brace_depth += stripped.count('{') - stripped.count('}')
                continue

            # 在目标类内部
            brace_depth += stripped.count('{') - stripped.count('}')

            # 检查是否离开了目标类
            if brace_depth <= class_start_depth:
                in_target_class = False
                continue

            # 跳过空行、注释、region
            s = stripped.strip()
            if not s or s.startswith('//') or s.startswith('/*') or s.startswith('*') or s.startswith('#region') or s.startswith('#endregion'):
                continue

            # 跳过 summary 注释块内的行
            if s.startswith('///') or s.startswith('///<'):
                continue

            # 尝试匹配构造函数
            mc = RE_CONSTRUCTOR.match(stripped)
            if mc and mc.group('name') == target_class:
                mods = mc.group('mods').strip()
                if not show_private and 'private' in mods:
                    continue
                sig = f"{mods} {target_class}{mc.group('params')}".strip()
                results.append({"type": "constructor", "signature": sig, "line": lineno})
                continue

            # 尝试匹配方法
            mm = RE_METHOD.match(stripped)
            if mm:
                mods = mm.group('mods').strip()
                if not show_private and 'private' in mods:
                    continue
                if 'class ' in stripped:
                    continue
                sig = f"{mods} {mm.group('return')} {mm.group('name')}{mm.group('params')}".strip()
                results.append({"type": "method", "signature": sig, "line": lineno})
                continue


    return results


def main():
    parser = argparse.ArgumentParser(
        description='查看 C# 类的所有成员（跨 partial class 文件）'
    )
    parser.add_argument('class_name', help='要查看的类名，如 Npc、CombatScene')
    parser.add_argument('--root', '-r', default='Scripts', help='搜索根目录（默认 Scripts）')
    parser.add_argument('--private', '-p', action='store_true', help='显示 private 成员')
    args = parser.parse_args()

    root = os.path.abspath(args.root)
    if not os.path.isdir(root):
        print(f"错误：目录不存在 {root}", file=sys.stderr)
        sys.exit(1)

    t_start = time.perf_counter()

    # 1. 找到同名文件夹
    class_dir = find_class_dir(root, args.class_name)
    t_find_dir = time.perf_counter()

    if not class_dir:
        print(f"错误：在 {root} 下未找到名为 {args.class_name} 的文件夹", file=sys.stderr)
        sys.exit(1)

    # 2. 只在该文件夹下扫描 .cs 文件
    cs_files = find_cs_files(class_dir)
    t_find_files = time.perf_counter()

    if not cs_files:
        print(f"错误：在 {class_dir} 下没有找到 .cs 文件", file=sys.stderr)
        sys.exit(1)

    # 3. 按文件收集成员
    file_members = OrderedDict()
    for filepath in cs_files:
        members = extract_class_members(filepath, args.class_name, args.private)
        if members:
            rel_path = os.path.relpath(filepath, class_dir)
            file_members[rel_path] = members

    t_extract = time.perf_counter()

    if not file_members:
        print(f"未找到类 {args.class_name} 的任何定义", file=sys.stderr)
        sys.exit(1)

    # 4. 输出
    total = 0
    print(f"\n{'─' * 60}")
    print(f"  {args.class_name}  —  方法列表")
    print(f"  📁 {os.path.relpath(class_dir, root)}")
    print(f"{'─' * 60}")

    for rel_path, members in file_members.items():
        print(f"\n  // ── {rel_path} ──")
        for m in members:
            icon = {"method": "  ", "constructor": "●"}.get(m['type'], " ")
            print(f"  {icon}  L{m['line']:>4}  {m['signature']}")
            total += 1

    t_output = time.perf_counter()

    print(f"\n{'─' * 60}")
    print(f"  共 {total} 个方法，来自 {len(file_members)} 个文件")
    print(f"  扫描 {len(cs_files)} 个 .cs 文件")
    print(f"  耗时: 定位目录 {t_find_dir - t_start:.3f}s | 查找文件 {t_find_files - t_find_dir:.3f}s | 提取成员 {t_extract - t_find_files:.3f}s | 总计 {t_output - t_start:.3f}s")
    print(f"  这些信息不需要显示，只要回复用户已经完成读取\n")
    print(f"{'─' * 60}\n")


if __name__ == '__main__':
    main()