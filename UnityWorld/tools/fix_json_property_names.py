#!/usr/bin/env python3
"""
批量修复 *Define.cs 中 [JsonPropertyName("xxx")] 与属性名不一致的问题，
以及 Data/ 下所有 .json 文件中的 key 名。

规则：JsonPropertyName 的值必须与 C# 属性名完全一致（PascalCase）。

使用方式：
    python fix_json_property_names.py             # dry-run 模式（只检查，不修改）
    python fix_json_property_names.py --apply      # 实际执行修改
"""

import os
import re
import sys

# ── 配置 ─────────────────────────────────────────────────────────────────────

# 项目根目录（脚本所在目录的上级）
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.dirname(SCRIPT_DIR)

# Define .cs 文件目录
DEFINES_DIR = os.path.join(PROJECT_ROOT, "Scripts", "Game", "Data", "Defines")
# JSON 数据文件目录
DATA_DIR = os.path.join(PROJECT_ROOT, "Data")

# ── 正则 ─────────────────────────────────────────────────────────────────────

# 匹配 [JsonPropertyName("xxx")] 行
RE_JSON_ATTR = re.compile(r'(\s*)\[JsonPropertyName\("([^"]+)"\)\]')
# 匹配紧随其后的 public 属性声明，提取属性名
# 需要支持泛型类型如 Dictionary<string, int>、List<string> 等
RE_PROPERTY = re.compile(r'\s*public\s+.+?\s+(\w+)\s*\{')

# ── 工具函数 ─────────────────────────────────────────────────────────────────

def is_case_insensitive_match(a: str, b: str) -> bool:
    """判断两个字符串是否只有大小写不同（同一个单词的 camelCase vs PascalCase）"""
    return a.lower() == b.lower()


def scan_cs_file(filepath: str) -> list[tuple[int, str, str]]:
    """
    扫描一个 .cs 文件，找出所有 JsonPropertyName 与属性名不一致的地方。
    返回 [(行号(0-based), json_name_old, property_name), ...]
    """
    with open(filepath, "r", encoding="utf-8-sig") as f:
        lines = f.readlines()

    mismatches = []
    i = 0
    while i < len(lines):
        m_attr = RE_JSON_ATTR.match(lines[i])
        if m_attr:
            json_name = m_attr.group(2)
            # 向下找属性声明（可能隔了其他 attribute 行）
            j = i + 1
            while j < len(lines) and j < i + 5:
                m_prop = RE_PROPERTY.match(lines[j])
                if m_prop:
                    prop_name = m_prop.group(1)
                    if json_name != prop_name:
                        # 只处理大小写不一致的情况
                        if is_case_insensitive_match(json_name, prop_name):
                            mismatches.append((i, json_name, prop_name))
                        else:
                            # 名称完全不同（如 LegacyStoryIds -> storyIds），跳过
                            pass
                    break
                # 跳过其他 attribute 行
                if lines[j].strip().startswith("["):
                    j += 1
                    continue
                break
        i += 1

    return mismatches


def collect_rename_map_from_cs(filepath: str) -> dict[str, str]:
    """
    从 .cs 文件中收集 JsonPropertyName 与属性名的映射（包括已一致的）。
    返回 {camelCase_json_name: PascalCase_property_name}，仅包含不一致的项。
    """
    with open(filepath, "r", encoding="utf-8-sig") as f:
        lines = f.readlines()

    rename_map = {}
    i = 0
    while i < len(lines):
        m_attr = RE_JSON_ATTR.match(lines[i])
        if m_attr:
            json_name = m_attr.group(2)
            j = i + 1
            while j < len(lines) and j < i + 5:
                m_prop = RE_PROPERTY.match(lines[j])
                if m_prop:
                    prop_name = m_prop.group(1)
                    # 收集当前已正确的 PascalCase 映射的 camelCase 对应
                    # 例如 CardDefine 已修好: JsonPropertyName("Desc") -> Desc
                    # 但 JSON 里可能还是 "desc"，所以需要 camelCase -> PascalCase
                    if json_name == prop_name and prop_name[0].isupper():
                        camel = prop_name[0].lower() + prop_name[1:]
                        if camel != prop_name:  # 避免纯小写名
                            rename_map[camel] = prop_name
                    break
                if lines[j].strip().startswith("["):
                    j += 1
                    continue
                break
        i += 1

    return rename_map


def fix_cs_file(filepath: str, mismatches: list[tuple[int, str, str]], dry_run: bool) -> dict[str, str]:
    """
    修复 .cs 文件中的 JsonPropertyName 值。
    返回 {old_json_name: new_json_name} 映射，供 JSON 修复使用。
    """
    with open(filepath, "r", encoding="utf-8-sig") as f:
        lines = f.readlines()

    rename_map = {}
    for line_no, old_name, new_name in mismatches:
        rename_map[old_name] = new_name
        old_line = lines[line_no]
        new_line = old_line.replace(f'JsonPropertyName("{old_name}")', f'JsonPropertyName("{new_name}")')
        lines[line_no] = new_line

    if not dry_run:
        with open(filepath, "w", encoding="utf-8") as f:
            f.writelines(lines)

    return rename_map


def detect_encoding(filepath: str) -> str:
    """
    检测文件编码：优先 UTF-8-BOM，否则尝试 UTF-8，失败则回退 GBK。
    返回 Python 编码名（如 'utf-8-sig', 'utf-8', 'gbk'）。
    """
    with open(filepath, "rb") as f:
        raw = f.read()

    if raw[:3] == b'\xef\xbb\xbf':
        return "utf-8-sig"

    try:
        raw.decode("utf-8")
        return "utf-8"
    except UnicodeDecodeError:
        pass

    try:
        raw.decode("gbk")
        return "gbk"
    except UnicodeDecodeError:
        pass

    print(f"  ⚠️ 无法确定 {filepath} 的编码，使用 utf-8 + replace 模式")
    return "utf-8"


def fix_json_file(filepath: str, rename_map: dict[str, str], dry_run: bool) -> int:
    """
    修复 JSON 文件中的 key 名（camelCase -> PascalCase）。
    使用文本替换而非 JSON 解析，以保留原始格式。
    自动检测文件编码，写回时统一使用 UTF-8。
    返回替换次数。
    """
    encoding = detect_encoding(filepath)
    with open(filepath, "r", encoding=encoding) as f:
        content = f.read()

    count = 0
    for old_key, new_key in rename_map.items():
        # 匹配 JSON key 模式: "oldKey": 或 "oldKey" :
        pattern = re.compile(r'"' + re.escape(old_key) + r'"(\s*:)')
        new_content = pattern.sub(f'"{new_key}"\\1', content)
        if new_content != content:
            occurrences = len(pattern.findall(content))
            count += occurrences
            content = new_content

    if count > 0 and not dry_run:
        with open(filepath, "w", encoding="utf-8") as f:
            f.write(content)

    return count


def collect_all_json_files(data_dir: str) -> list[str]:
    """递归收集 Data/ 下所有 .json 文件"""
    json_files = []
    for root, dirs, files in os.walk(data_dir):
        for f in files:
            if f.endswith(".json"):
                json_files.append(os.path.join(root, f))
    json_files.sort()
    return json_files


# ── 主流程 ─────────────────────────────────────────────────────────────────────

def main():
    dry_run = "--apply" not in sys.argv

    if dry_run:
        print("=" * 60)
        print("  DRY-RUN 模式（只检查，不修改文件）")
        print("  添加 --apply 参数以实际执行修改")
        print("=" * 60)
    else:
        print("=" * 60)
        print("  APPLY 模式（将实际修改文件）")
        print("=" * 60)
    print()

    # ══════════════════════════════════════════════════════════════
    # Phase 1: 扫描并修复 .cs 文件，同时收集全局 rename_map
    # ══════════════════════════════════════════════════════════════
    print("── Phase 1: 扫描 .cs 文件 ──────────────────────────────")
    print()

    # 收集所有 .cs 文件
    all_cs_files = []
    for root, dirs, files in os.walk(DEFINES_DIR):
        for f in files:
            if f.endswith(".cs"):
                all_cs_files.append(os.path.join(root, f))
    all_cs_files.sort()

    total_cs_fixes = 0
    cs_files_with_issues = []
    # 全局 rename_map: camelCase -> PascalCase（合并所有 Define 的字段映射）
    global_rename_map: dict[str, str] = {}

    for cs_file in all_cs_files:
        rel_path = os.path.relpath(cs_file, PROJECT_ROOT)

        # 1a. 检查有无不一致需要修复
        mismatches = scan_cs_file(cs_file)
        if mismatches:
            cs_files_with_issues.append(rel_path)
            print(f"📄 {rel_path}")
            for line_no, old_name, new_name in mismatches:
                print(f"   L{line_no + 1}: JsonPropertyName(\"{old_name}\") -> \"{new_name}\"")
            total_cs_fixes += len(mismatches)

            # 修复 .cs 并收集 rename_map
            rename_map = fix_cs_file(cs_file, mismatches, dry_run)
            global_rename_map.update(rename_map)
            print()

        # 1b. 从已正确的 .cs 中也收集 camelCase -> PascalCase 映射
        #     因为 .cs 可能已修好，但 JSON 还是旧的 camelCase
        extra_map = collect_rename_map_from_cs(cs_file)
        global_rename_map.update(extra_map)

    if not cs_files_with_issues:
        print("✅ 所有 .cs 文件的 JsonPropertyName 均已与属性名一致")
    else:
        action = "已修复" if not dry_run else "需要修复"
        print(f"📊 .cs {action}：{len(cs_files_with_issues)} 个文件，{total_cs_fixes} 处")
    print()

    # ══════════════════════════════════════════════════════════════
    # Phase 2: 用全局 rename_map 扫描并修复 Data/ 下所有 .json 文件
    # ══════════════════════════════════════════════════════════════
    print("── Phase 2: 扫描 Data/ 下所有 .json 文件 ────────────────")
    print(f"   全局 rename_map 共 {len(global_rename_map)} 条映射：")
    for old, new in sorted(global_rename_map.items()):
        print(f"     \"{old}\" -> \"{new}\"")
    print()

    all_json_files = collect_all_json_files(DATA_DIR)
    total_json_fixes = 0
    json_files_fixed = 0

    for json_file in all_json_files:
        json_rel = os.path.relpath(json_file, PROJECT_ROOT)
        json_count = fix_json_file(json_file, global_rename_map, dry_run)
        if json_count > 0:
            print(f"   📦 {json_rel}: {json_count} 处 key 替换")
            total_json_fixes += json_count
            json_files_fixed += 1

    if total_json_fixes == 0:
        print("   ✅ 所有 JSON 文件的 key 名均已是 PascalCase")
    print()

    # ── 总结 ─────────────────────────────────────────────────────
    print("=" * 60)
    if total_cs_fixes == 0 and total_json_fixes == 0:
        print("✅ 全部通过！无需任何修改。")
    else:
        action = "已修复" if not dry_run else "需要修复"
        print(f"📊 {action}：")
        print(f"   .cs  文件：{len(cs_files_with_issues)} 个文件，{total_cs_fixes} 处 JsonPropertyName")
        print(f"   .json 文件：{json_files_fixed} 个文件，{total_json_fixes} 处 key 名替换")
        if dry_run:
            print()
            print("⚡ 运行 python fix_json_property_names.py --apply 以实际执行修改")
    print("=" * 60)


if __name__ == "__main__":
    main()