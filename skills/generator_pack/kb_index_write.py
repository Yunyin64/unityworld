#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
知识库索引写入脚本 v5（纯文本索引，无 SQLite 依赖）。

用法：
  python .context/code/generator_pack/kb_index_write.py --rebuild
  python .context/code/generator_pack/kb_index_write.py --rebuild --token-ratio 0.01 --min-keywords 50
  python .context/code/generator_pack/kb_index_write.py --rebuild-entries

所有命令的 --root 默认为 .context/code/。
文本索引输出到 .context/code/index/knowledge_index.txt。

关键词预算算法：
  1. 统计每目录的加权 token 总量
  2. 若 token_ratio × 全部 token 总量 ≤ max_total_keywords，使用 token_ratio
  3. 否则反算 effective_ratio = max_total_keywords / 全部 token 总量
  4. 每目录关键词数 = max(min_keywords, int(dir_tokens × effective_ratio))
"""
from __future__ import annotations

import argparse
import hashlib
import math
import sys
from collections import Counter
from pathlib import Path

# Windows 终端默认 codepage 可能是 GBK，强制 UTF-8 输出避免乱码
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")

from tokenizer import extract_all_tokens, extract_summary, extract_title, extract_weighted_tokens

ROOT_DEFAULT = Path(__file__).resolve().parent.parent          # .context/code/
TXT_DEFAULT  = ROOT_DEFAULT / "index" / "knowledge_index.txt"
DOC_DIRS     = ["infrastructure", "game_foundation", "business", "design_data", "concept", "experience"]
SKIP_NAMES   = {"entry.md", "ENTRY.md", "README.md"}
_FILENAME_CATEGORIES = {"module", "flow", "slice", "designdata"}
_FILENAME_PREFIXES   = {"concept", "experience"}


def extract_filename_name_tokens(filename: str) -> list[str]:
    """从文件名中提取 name 相关字段，强制作为索引关键词。

    命名规则：
      <name>.<category>.md            → 提取 <name> 及其子词
      <name>.<subname>.<category>.md  → 提取 <name>, <subname> 及其子词和组合
      concept.<name>.md               → 提取 <name> 及其子词
      experience.<name>.md            → 提取 <name> 及其子词
    """
    stem = filename[:-3] if filename.endswith(".md") else filename
    parts = stem.split(".")

    name_parts: list[str] = []
    if parts[0] in _FILENAME_PREFIXES and len(parts) >= 2:
        name_parts = parts[1:]
    elif len(parts) >= 2 and parts[-1] in _FILENAME_CATEGORIES:
        name_parts = parts[:-1]
    else:
        name_parts = parts

    result: list[str] = []
    if len(name_parts) > 1:
        result.append(".".join(name_parts).lower())

    for part in name_parts:
        result.append(part.lower())
        sub_tokens = extract_all_tokens(part)
        result.extend(sub_tokens)

    seen: set[str] = set()
    deduped: list[str] = []
    for t in result:
        if t and t not in seen and len(t) >= 2:
            seen.add(t)
            deduped.append(t)
    return deduped


def file_sha1(content: str) -> str:
    return hashlib.sha1(content.encode("utf-8", errors="ignore")).hexdigest()


def rebuild(root: Path, txt_path: Path,
            min_keywords: int = 30, max_total_keywords: int = 200_000,
            token_ratio: float = 0.005) -> dict:
    """目录级 TF-IDF 全量重建 → 直接写 knowledge_index.txt。

    算法：
      1. 统计每目录的加权 token 总量
      2. 若 token_ratio × 全部 token 总量 ≤ max_total_keywords，使用 token_ratio
      3. 否则反算 effective_ratio = max_total_keywords / 全部 token 总量
      4. 每目录关键词数 = max(min_keywords, int(dir_tokens × effective_ratio))
    """
    # Phase 1: collect tokens per directory
    dir_data: dict[str, dict] = {}  # rel_dir → {tokens, entry_md_rel, filename_name_tokens, weighted_token_count}
    for doc_dir_name in DOC_DIRS:
        doc_dir = root / doc_dir_name
        if not doc_dir.exists():
            continue
        subdirs: set[Path] = set()
        for md in doc_dir.rglob("*.md"):
            if md.name not in SKIP_NAMES:
                subdirs.add(md.parent)
        for subdir in sorted(subdirs):
            rel_dir = str(subdir.relative_to(root)).replace("\\", "/")
            entry_md_rel = rel_dir + "/entry.md"
            tokens: Counter = Counter()
            filename_name_tokens: set[str] = set()
            combined: list[str] = []
            for md in sorted(subdir.glob("*.md")):
                if md.name in SKIP_NAMES:
                    continue
                content = md.read_text(encoding="utf-8", errors="ignore")
                combined.append(content)
                for token, weight in extract_weighted_tokens(content).items():
                    tokens[token] += weight
                fnt = extract_filename_name_tokens(md.name)
                filename_name_tokens.update(fnt)
                for t in fnt:
                    tokens[t] += 20
            if not tokens:
                continue
            weighted_token_count = sum(tokens.values())
            dir_data[rel_dir] = {
                "tokens": tokens,
                "filename_name_tokens": filename_name_tokens,
                "entry_md_rel": entry_md_rel,
                "weighted_token_count": weighted_token_count,
            }

    # Phase 2: calculate effective ratio
    total_weighted_tokens = sum(d["weighted_token_count"] for d in dir_data.values())
    effective_ratio = token_ratio
    estimated_total = int(total_weighted_tokens * token_ratio)
    if estimated_total > max_total_keywords:
        effective_ratio = max_total_keywords / total_weighted_tokens

    # Phase 3: document frequency across directories (in memory)
    N = len(dir_data)
    df: Counter = Counter()
    for data in dir_data.values():
        for token in data["tokens"]:
            df[token] += 1

    # Phase 4: TF-IDF → per-directory keyword selection
    txt_lines: list[tuple[str, str]] = []  # (entry_md_rel, "kw1 kw2 ...")
    total_keywords = 0
    for rel_dir, data in sorted(dir_data.items()):
        tokens = data["tokens"]
        entry_md_rel = data["entry_md_rel"]

        dir_kw_budget = max(min_keywords, int(data["weighted_token_count"] * effective_ratio))

        max_tf = max(tokens.values())
        scores: dict[str, float] = {}
        for token, count in tokens.items():
            idf = math.log(N / df[token])
            if idf > 0:
                scores[token] = (count / max_tf) * idf

        top_keywords = sorted(scores, key=lambda k: scores[k], reverse=True)[:dir_kw_budget]
        # Force-include filename name tokens
        keyword_set = set(top_keywords)
        for t in data["filename_name_tokens"]:
            if t not in keyword_set:
                keyword_set.add(t)
                top_keywords.append(t)
        # Deduplicate (defense-in-depth, preserving score order + forced tokens)
        seen: set[str] = set()
        deduped: list[str] = []
        for kw in top_keywords:
            if kw not in seen:
                seen.add(kw)
                deduped.append(kw)
        top_keywords = deduped

        txt_lines.append((entry_md_rel, ",".join(top_keywords)))
        total_keywords += len(top_keywords)

    # Phase 5: write text index
    txt_path.parent.mkdir(parents=True, exist_ok=True)
    with open(txt_path, "w", encoding="utf-8") as f:
        for entry_md_rel, keywords in sorted(txt_lines):
            f.write(f"{entry_md_rel}\t{keywords}\n")

    return {"indexed_dirs": len(txt_lines), "total_keywords": total_keywords,
            "effective_ratio": effective_ratio, "total_weighted_tokens": total_weighted_tokens}


# ── Summary ───────────────────────────────────────────────────────────────────

def read_summary(md_path: Path) -> str:
    """读取 MD 文件的 summary，无则 fallback 到标题。"""
    content = md_path.read_text(encoding="utf-8", errors="ignore")
    return extract_summary(content) or extract_title(content)


# ── entry.md 生成 ─────────────────────────────────────────────────────────────

def rebuild_entries(root: Path) -> int:
    """为每个包含知识文档的子目录生成/更新 entry.md，返回生成数量。"""
    generated = 0
    for doc_dir_name in DOC_DIRS:
        doc_dir = root / doc_dir_name
        if not doc_dir.exists():
            continue
        subdirs: set[Path] = set()
        for md in doc_dir.rglob("*.md"):
            if md.name not in SKIP_NAMES:
                subdirs.add(md.parent)
        for subdir in sorted(subdirs):
            _write_entry_md(subdir, root)
            generated += 1
    return generated


def _infer_type(filename: str) -> str:
    if ".flow."   in filename: return "flow"
    if ".slice."  in filename: return "slice"
    if ".module." in filename: return "module"
    if filename.startswith("concept."):    return "concept"
    if filename.startswith("experience."): return "experience"
    return "doc"


def _write_entry_md(subdir: Path, root: Path) -> None:
    docs = sorted(
        [md for md in subdir.glob("*.md") if md.name not in SKIP_NAMES],
        key=lambda p: p.name,
    )
    if not docs:
        return

    rel_dir = str(subdir.relative_to(root)).replace("\\", "/")
    lines = [
        f"# {rel_dir} — 目录概览",
        "",
        "> 本文件由工具自动生成，勿手动编辑。",
        f"> 更新命令：`python .context/code/generator_pack/kb_index_write.py --rebuild-entries`",
        "",
        "## 文档列表",
        "",
        "| 文件 | 类型 | 简介 |",
        "|------|------|------|",
    ]
    for md in docs:
        doc_type = _infer_type(md.name)
        summary  = read_summary(md)
        summary_cell = summary if summary else "—"
        lines.append(f"| [{md.name}]({md.name}) | {doc_type} | {summary_cell} |")

    (subdir / "entry.md").write_text("\n".join(lines) + "\n", encoding="utf-8")


# ── CLI ───────────────────────────────────────────────────────────────────────

def main() -> None:
    ap = argparse.ArgumentParser(description="KB index writer v5 (pure text, no SQLite)")
    ap.add_argument("--root",    default=str(ROOT_DEFAULT))
    ap.add_argument("--rebuild", action="store_true", help="全量重建文本索引")
    ap.add_argument("--rebuild-entries", action="store_true", help="只刷新 entry.md（不改索引）")
    ap.add_argument("--min-keywords",       type=int,   default=30,
                    help="每目录最少关键词数 (default: 30)")
    ap.add_argument("--max-total-keywords", type=int,   default=200_000,
                    help="全局关键词数上限 (default: 200000)")
    ap.add_argument("--token-ratio",        type=float, default=0.005,
                    help="token→关键词提取比例 (default: 0.005)")
    args = ap.parse_args()

    root = Path(args.root).resolve()
    txt_path = root / "index" / "knowledge_index.txt"

    if args.rebuild:
        stats = rebuild(root, txt_path,
                        min_keywords=args.min_keywords,
                        max_total_keywords=args.max_total_keywords,
                        token_ratio=args.token_ratio)
        n_entries = rebuild_entries(root)
        print(f"Rebuild done: {stats['indexed_dirs']} directories indexed, "
              f"{stats['total_keywords']} total keywords, "
              f"effective_ratio={stats['effective_ratio']:.6f}, "
              f"total_weighted_tokens={stats['total_weighted_tokens']}, "
              f"entry.md generated for {n_entries} directories")
        print(f"Text index: {txt_path}")

    elif args.rebuild_entries:
        n = rebuild_entries(root)
        print(f"Generated entry.md for {n} directories.")

    else:
        ap.print_help()


if __name__ == "__main__":
    main()
