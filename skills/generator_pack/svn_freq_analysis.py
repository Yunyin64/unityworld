#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
SVN 提交频率分析 - 找出高频改动的策划表和代码模块，用于知识库优先级排序。

用法:
  python .context/code/generator_pack/svn_freq_analysis.py
  python .context/code/generator_pack/svn_freq_analysis.py --months 6 --top 30
  python .context/code/generator_pack/svn_freq_analysis.py --dir program/game/gas/lua
"""
from __future__ import annotations

import argparse
import re
import shutil
import subprocess
import sys
from collections import Counter, defaultdict
from datetime import datetime, timedelta
from pathlib import Path

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")

# ── 路径分类 ──────────────────────────────────────

CAT_DESIGN = "design_data"
CAT_GAME = "game_code"
CAT_ENGINE = "engine"
CAT_CLIENT = "client_cs"
CAT_OTHER = "other"

CAT_LABELS = {
    CAT_DESIGN: "策划表",
    CAT_GAME: "游戏代码",
    CAT_ENGINE: "引擎",
    CAT_CLIENT: "C#客户端",
}


def classify_path(path: str) -> tuple[str, str]:
    """Classify a changed file path → (category, group_key).

    group_key is the aggregation unit:
    - design_data: BookName directory, e.g. "Gameplay"
    - game_code: side/module, e.g. "gas/gameplay", "gac/vvm/ActivityIPEVA"
    - engine: module dir, e.g. "src/ArkPipe"
    - client_cs: top dir under Scripts/
    """
    # ── 策划配置表 ──
    pfx = "dev/design/data/"
    if path.startswith(pfx):
        rest = path[len(pfx):]
        parts = rest.split("/")

        # Build / utility scripts — not design data tables
        _skip_prefixes = ("Build", "Check", "DesignDataBuilder", "Merge")
        if parts[-1].startswith(_skip_prefixes) and parts[-1].endswith(".lua"):
            return (CAT_OTHER, "(build_scripts)")

        # Server/<BookName>/... and Client/<BookName>/... → attribute to BookName
        if parts[0] in ("Client", "Server") and len(parts) >= 2:
            if len(parts) >= 3:
                # Server/GamePlay/Arrest.lua → BookName = GamePlay
                return (CAT_DESIGN, parts[1])
            elif len(parts) == 2 and "." in parts[1]:
                # Server/Buff.lua → flat generated file, attribute to filename stem
                stem = parts[1].rsplit(".", 1)[0]
                return (CAT_DESIGN, stem)
            else:
                return (CAT_OTHER, f"({parts[0].lower()}_misc)")

        # Common/<BookName>/... → attribute to BookName
        if parts[0] == "Common" and len(parts) >= 2:
            if len(parts) >= 3:
                return (CAT_DESIGN, parts[1])
            elif len(parts) == 2 and "." in parts[1]:
                stem = parts[1].rsplit(".", 1)[0]
                return (CAT_DESIGN, stem)
            else:
                return (CAT_OTHER, "(common_misc)")

        # Top-level BookName directories: GamePlay/, Status/, Item/, etc.
        book = parts[0] if parts else "(root)"
        # Skip non-table directories
        if book in ("DesignDoc", "Document", "Backup"):
            return (CAT_OTHER, "(docs)")
        return (CAT_DESIGN, book)

    # ── Lua 游戏代码 ──
    pfx = "program/game/"
    if path.startswith(pfx):
        rest = path[len(pfx):]
        parts = rest.split("/")
        if len(parts) >= 3 and parts[1] == "lua":
            side = parts[0]
            mod = parts[2]

            # gac/lua/vvm/ModuleName → already granular
            if side == "gac" and mod == "vvm" and len(parts) >= 4:
                # If VVM module has sub-dirs, go one deeper
                if len(parts) >= 5 and "." not in parts[4]:
                    return (CAT_GAME, f"gac/vvm/{parts[3]}/{parts[4]}")
                return (CAT_GAME, f"gac/vvm/{parts[3]}")

            # gac/lua/vvm/VVMInc.lua → single file, keep as-is
            if side == "gac" and mod == "vvm":
                return (CAT_GAME, "gac/vvm/(shared)")

            # Drill into subdirectories for large modules
            # gas/lua/gameplay/BHLSPlay/xxx.lua → gas/gameplay/BHLSPlay
            # gas/lua/gameplay/GameplayMgr.lua → gas/gameplay/(core)
            if len(parts) >= 5:
                # Has submodule directory: parts[3] is a subdir
                return (CAT_GAME, f"{side}/{mod}/{parts[3]}")
            elif len(parts) == 4 and "." not in parts[3]:
                # parts[3] is a directory name (no extension)
                return (CAT_GAME, f"{side}/{mod}/{parts[3]}")
            elif len(parts) == 4 and "." in parts[3]:
                # File directly under module → core files
                return (CAT_GAME, f"{side}/{mod}/(core)")
            else:
                return (CAT_GAME, f"{side}/{mod}")

        if len(parts) >= 2:
            return (CAT_GAME, f"{parts[0]}/{parts[1]}")
        return (CAT_GAME, parts[0] if parts else "(root)")

    # ── C++ 引擎 ──
    pfx = "program/engine/"
    if path.startswith(pfx):
        rest = path[len(pfx):]
        parts = rest.split("/")
        if parts[0] == "src" and len(parts) >= 3:
            return (CAT_ENGINE, f"src/{parts[1]}")
        if len(parts) >= 2:
            return (CAT_ENGINE, parts[0])
        return (CAT_ENGINE, "(root)")

    # ── C# 客户端 ──
    pfx = "dev/client/nshm/Assets/Scripts/"
    if path.startswith(pfx):
        rest = path[len(pfx):]
        parts = rest.split("/")
        return (CAT_CLIENT, parts[0] if parts else "(root)")

    return (CAT_OTHER, "(other)")


# ── SVN 日志解析 ──────────────────────────────────

REVISION_RE = re.compile(r"^r(\d+)\s*\|")
CHANGED_PATH_RE = re.compile(r"^\s+[MADRC]\s+(.+?)(?:\s+\(from\s+.+\))?$")


def stream_svn_log(directory: Path, start_date: str, timeout: int = 600):
    """Run svn log -v and yield (revision, [normalized_paths]) tuples.

    Uses streaming to avoid loading the full log into memory.
    """
    cmd = [
        "svn", "log", "-v",
        "-r", f"{{{start_date}}}:HEAD",
        str(directory),
    ]
    print(f"  执行: svn log -v -r {{{start_date}}}:HEAD {directory.name}/", file=sys.stderr)

    try:
        proc = subprocess.Popen(
            cmd,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            cwd=str(directory.parent),
        )
    except FileNotFoundError:
        print("ERROR: 'svn' 命令未找到", file=sys.stderr)
        sys.exit(1)

    # Read with streaming, decode line by line
    current_rev: int | None = None
    current_paths: list[str] = []
    repo_prefixes: list[str] = []  # detected once from first paths
    prefix_detected = False
    line_count = 0

    def normalize(raw: str) -> str:
        nonlocal repo_prefixes, prefix_detected
        path = raw.lstrip("/")
        if not prefix_detected and path:
            # Detect prefix from first path, e.g. "nshm/trunk/" or "nshm/"
            parts = path.split("/")
            if len(parts) >= 2:
                if parts[1] in ("trunk", "branches", "tags"):
                    repo_prefixes.append("/".join(parts[:2]) + "/")
                repo_prefixes.append(parts[0] + "/")
                prefix_detected = True
        for pfx in repo_prefixes:
            if path.lower().startswith(pfx.lower()):
                path = path[len(pfx):]
                break
        return path.lstrip("/")

    try:
        for raw_line in proc.stdout:
            line = raw_line.decode("utf-8", errors="replace").rstrip("\n\r")
            line_count += 1

            if line_count % 50000 == 0:
                print(f"    ...已处理 {line_count} 行", file=sys.stderr)

            rev_m = REVISION_RE.match(line)
            if rev_m:
                if current_rev is not None and current_paths:
                    yield (current_rev, current_paths)
                current_rev = int(rev_m.group(1))
                current_paths = []
                continue

            path_m = CHANGED_PATH_RE.match(line)
            if path_m:
                npath = normalize(path_m.group(1).strip())
                if npath and not npath.startswith("."):
                    current_paths.append(npath)

        # Last entry
        if current_rev is not None and current_paths:
            yield (current_rev, current_paths)

        proc.wait(timeout=30)

    except subprocess.TimeoutExpired:
        proc.kill()
        proc.wait()
        print(f"  WARNING: svn log 超时，部分数据可能丢失", file=sys.stderr)

    rc = proc.returncode
    if rc != 0:
        stderr = proc.stderr.read().decode("utf-8", errors="replace")[:300]
        print(f"  WARNING: svn log 退出码 {rc}: {stderr}", file=sys.stderr)


# ── 主逻辑 ────────────────────────────────────────

def detect_project_root() -> Path:
    script_dir = Path(__file__).resolve().parent
    return script_dir.parent.parent  # generator_pack -> .context -> root


def main():
    parser = argparse.ArgumentParser(
        description="SVN 提交频率分析 - 找出高频改动的策划表和代码模块",
    )
    parser.add_argument("--months", type=int, default=4, help="分析最近 N 个月（默认 4）")
    parser.add_argument("--top", type=int, default=30, help="每个分类输出 Top N（默认 30）")
    parser.add_argument("--dir", type=str, action="append", default=None,
                        help="只分析指定目录（可多次使用）")
    parser.add_argument("--timeout", type=int, default=600,
                        help="每个目录 SVN log 超时秒数（默认 600）")
    args = parser.parse_args()

    if not shutil.which("svn"):
        print("ERROR: 'svn' 未安装或不在 PATH 中", file=sys.stderr)
        sys.exit(1)

    project_root = detect_project_root()
    print(f"项目根目录: {project_root}")

    end_date = datetime.now()
    start_date = end_date - timedelta(days=args.months * 30)
    start_date_str = start_date.strftime("%Y-%m-%d")
    date_desc = f"{start_date_str} ~ {end_date.strftime('%Y-%m-%d')}（{args.months}个月）"
    print(f"分析范围: {date_desc}\n")

    # Directories to scan
    key_dirs = [
        "program/game",
        "dev/design/data",
        "program/engine",
        "dev/client/nshm/Assets/Scripts",
    ]
    if args.dir:
        key_dirs = args.dir

    # Accumulators
    group_rev_counter: Counter[tuple[str, str]] = Counter()   # (cat, group) → revision count (dedup per rev)
    group_files: dict[tuple[str, str], set[str]] = defaultdict(set)  # (cat, group) → set of files
    file_rev_counter: Counter[tuple[str, str]] = Counter()    # (cat, path) → revision count

    total_revs = 0
    total_changes = 0

    for dir_name in key_dirs:
        dir_path = project_root / dir_name
        if not dir_path.exists():
            print(f"  跳过不存在的目录: {dir_name}", file=sys.stderr)
            continue

        print(f"{'─'*60}")
        print(f"扫描: {dir_name}/")
        print(f"{'─'*60}")

        rev_count = 0
        change_count = 0

        for rev, paths in stream_svn_log(dir_path, start_date_str, timeout=args.timeout):
            rev_count += 1
            rev_groups: set[tuple[str, str]] = set()

            for path in paths:
                change_count += 1
                cat, group_key = classify_path(path)

                file_rev_counter[(cat, path)] += 1

                gs = (cat, group_key)
                if gs not in rev_groups:
                    rev_groups.add(gs)
                    group_rev_counter[gs] += 1

                group_files[gs].add(path)

        total_revs += rev_count
        total_changes += change_count
        print(f"  结果: {rev_count} 个提交, {change_count} 个文件变更\n", file=sys.stderr)

    # ── 生成报告 ──
    lines: list[str] = []
    lines.append(f"# SVN 提交频率分析报告")
    lines.append(f"")
    lines.append(f"> 分析范围：{date_desc}")
    lines.append(f"> 总计：{total_revs} 个提交，{total_changes} 个文件变更")
    lines.append(f"")

    def _table_header(cols: list[str]):
        lines.append(f"| {' | '.join(cols)} |")
        lines.append(f"|{'------|' * len(cols)}")

    def _table_row(cols: list[str]):
        lines.append(f"| {' | '.join(cols)} |")

    # ── 一、策划配置表（按表目录聚合）──
    dd_groups = [(k, v) for k, v in group_rev_counter.items() if k[0] == CAT_DESIGN]
    dd_groups.sort(key=lambda x: x[1], reverse=True)

    lines.append(f"## 一、策划配置表改动频率 Top {args.top}（按表目录/BookName 聚合）")
    lines.append(f"")
    _table_header(["排名", "表目录", "涉及提交数", "涉及文件数", "代表文件"])
    for i, ((cat, gk), cnt) in enumerate(dd_groups[:args.top], 1):
        fset = group_files[(cat, gk)]
        rep = sorted(f.split("/")[-1] for f in fset if f.endswith(".txt"))[:3]
        rep_str = ", ".join(rep)
        if len(fset) > 3:
            rep_str += f"  …共{len(fset)}个"
        _table_row([str(i), gk, str(cnt), str(len(fset)), rep_str])
    lines.append("")

    # ── 一-B、策划配置表 Book（.txt 源文件）改动频率 ──
    # 每个 .txt 源文件 = 一个 Book，按提交频率排序
    GENERATED_DIRS = {"Server", "Client", "Common"}
    book_files = []
    for (cat, path), cnt in file_rev_counter.items():
        if cat != CAT_DESIGN:
            continue
        if not path.endswith(".txt"):
            continue
        pfx = "dev/design/data/"
        if not path.startswith(pfx):
            continue
        rest = path[len(pfx):]
        parts = rest.split("/")
        # 跳过 Server/Client/Common 下的生成文件目录
        if parts[0] in GENERATED_DIRS:
            continue
        # 跳过非表目录
        if parts[0] in ("DesignDoc", "Document", "Backup", "Build", "Check", "DesignDataBuilder", "Merge"):
            continue
        dir_name = parts[0] if len(parts) >= 2 else "(root)"
        book_name = parts[-1].rsplit(".", 1)[0]
        book_files.append((dir_name, book_name, path, cnt))

    book_files.sort(key=lambda x: x[3], reverse=True)

    lines.append(f"## 一-B、策划配置表 Book 改动频率 Top {min(args.top, len(book_files))}（按 .txt 源文件/Book 聚合）")
    lines.append(f"")
    lines.append(f"> 每个 .txt 源文件 = 一个 Book（如 Buff.txt → Book Buff），目录为分组（如 Status/Buff.txt → Status 分组）。")
    lines.append(f"")
    _table_header(["排名", "目录", "Book名", "文件路径", "涉及提交数"])
    for i, (dir_name, book_name, path, cnt) in enumerate(book_files[:args.top], 1):
        _table_row([str(i), dir_name, book_name, path, str(cnt)])
    lines.append("")

    # ── 二、策划配置表（按所有文件，含 .lua 生成文件）──
    dd_files = [(k, v) for k, v in file_rev_counter.items() if k[0] == CAT_DESIGN]
    dd_files.sort(key=lambda x: x[1], reverse=True)

    lines.append(f"## 二、策划配置表全部文件改动频率 Top {args.top}（含 .lua 生成文件）")
    lines.append(f"")
    _table_header(["排名", "文件路径", "涉及提交数"])
    for i, ((cat, path), cnt) in enumerate(dd_files[:args.top], 1):
        _table_row([str(i), path, str(cnt)])
    lines.append("")

    # ── 三、游戏代码模块 ──
    game_groups = [(k, v) for k, v in group_rev_counter.items() if k[0] == CAT_GAME]
    game_groups.sort(key=lambda x: x[1], reverse=True)

    lines.append(f"## 三、游戏代码模块改动频率 Top {args.top}（按目录聚合）")
    lines.append(f"")
    _table_header(["排名", "模块路径", "涉及提交数", "涉及文件数", "代表文件"])
    for i, ((cat, gk), cnt) in enumerate(game_groups[:args.top], 1):
        fset = group_files[(cat, gk)]
        rep = sorted(f.split("/")[-1] for f in fset if f.endswith(".lua"))[:3]
        rep_str = ", ".join(rep)
        if len(fset) > 3:
            rep_str += f"  …共{len(fset)}个"
        _table_row([str(i), gk, str(cnt), str(len(fset)), rep_str])
    lines.append("")

    # ── 四、游戏代码文件 ──
    game_files = [(k, v) for k, v in file_rev_counter.items() if k[0] == CAT_GAME]
    game_files.sort(key=lambda x: x[1], reverse=True)

    lines.append(f"## 四、游戏代码文件改动频率 Top {args.top}")
    lines.append(f"")
    _table_header(["排名", "文件路径", "涉及提交数"])
    for i, ((cat, path), cnt) in enumerate(game_files[:args.top], 1):
        _table_row([str(i), path, str(cnt)])
    lines.append("")

    # ── 五、引擎模块 ──
    eng_groups = [(k, v) for k, v in group_rev_counter.items() if k[0] == CAT_ENGINE]
    if eng_groups:
        eng_groups.sort(key=lambda x: x[1], reverse=True)
        lines.append(f"## 五、引擎模块改动频率 Top {min(args.top, len(eng_groups))}")
        lines.append(f"")
        _table_header(["排名", "模块路径", "涉及提交数", "涉及文件数"])
        for i, ((cat, gk), cnt) in enumerate(eng_groups[:args.top], 1):
            fset = group_files[(cat, gk)]
            _table_row([str(i), gk, str(cnt), str(len(fset))])
        lines.append("")

    # ── 六、C# 客户端模块 ──
    cs_groups = [(k, v) for k, v in group_rev_counter.items() if k[0] == CAT_CLIENT]
    if cs_groups:
        cs_groups.sort(key=lambda x: x[1], reverse=True)
        lines.append(f"## 六、C# 客户端模块改动频率 Top {min(args.top, len(cs_groups))}")
        lines.append(f"")
        _table_header(["排名", "模块路径", "涉及提交数", "涉及文件数"])
        for i, ((cat, gk), cnt) in enumerate(cs_groups[:args.top], 1):
            fset = group_files[(cat, gk)]
            _table_row([str(i), gk, str(cnt), str(len(fset))])
        lines.append("")

    # ── 综合排名 ──
    all_groups = [(k, v) for k, v in group_rev_counter.items() if k[0] != CAT_OTHER]
    all_groups.sort(key=lambda x: x[1], reverse=True)

    lines.append(f"## 综合排名：所有模块/表改动频率 Top {args.top}")
    lines.append(f"")
    _table_header(["排名", "分类", "模块/表名", "涉及提交数", "涉及文件数"])
    for i, ((cat, gk), cnt) in enumerate(all_groups[:args.top], 1):
        fset = group_files[(cat, gk)]
        _table_row([str(i), CAT_LABELS.get(cat, cat), gk, str(cnt), str(len(fset))])
    lines.append("")

    # ── 知识库生成建议 ──
    lines.append(f"## 知识库生成建议")
    lines.append(f"")
    lines.append(f"根据以上分析，建议按以下优先级生成知识库文档：")
    lines.append(f"")

    # Design data suggestions — Book 级别
    book_top = book_files[:args.top]
    if book_top:
        # 检测同名 Book（不同目录下的同名 .txt）
        name_counts: Counter[str] = Counter()
        for dir_name, book_name, path, cnt in book_top:
            name_counts[book_name] += 1
        lines.append(f"### 策划数据表（design_data）— Book 级别")
        lines.append(f"")
        lines.append(f"```csv")
        lines.append(f"name,entry_file,category,description")
        for dir_name, book_name, path, cnt in book_top:
            # 同名 Book 加目录前缀避免歧义
            if name_counts[book_name] > 1:
                csv_name = f"{dir_name}_{book_name}"
            else:
                csv_name = book_name
            lines.append(f"{csv_name},{path},design_data,{dir_name}/{book_name} 策划配置表（{cnt}次提交）")
        lines.append(f"```")
        lines.append(f"")

    # Game code suggestions
    game_top = [(gk, cnt) for (cat, gk), cnt in game_groups[:args.top]]
    if game_top:
        lines.append(f"### 游戏代码模块（infrastructure/game_foundation/business）")
        lines.append(f"")
        lines.append(f"```csv")
        lines.append(f"name,entry_file,category,description")
        for gk, cnt in game_top:
            side, mod = gk.split("/", 1) if "/" in gk else (gk, "")
            # Suggest entry file
            if side in ("gas", "gac", "common", "master"):
                entry = f"program/game/{side}/lua/{mod}/"
            else:
                entry = ""
            lines.append(f"{mod},{entry},待定,{gk}模块（{cnt}次提交）")
        lines.append(f"```")
        lines.append(f"")
    lines.append('> 注：category 列需人工判定（infrastructure/game_foundation/business），已标为"待定"。')

    report = "\n".join(lines)

    # Output
    print("\n" + report)

    # Save
    ts = datetime.now().strftime("%Y%m%d_%H%M%S")
    out_dir = project_root / "cc_task" / f"svn_freq_{ts}"
    out_dir.mkdir(parents=True, exist_ok=True)
    out_path = out_dir / "report.md"
    out_path.write_text(report, encoding="utf-8")
    print(f"\n报告已保存: {out_path}")


if __name__ == "__main__":
    main()
