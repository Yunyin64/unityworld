#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
词元提取模块。供 kb_index_write.py 和 kb_index_query.py 共用。

提取策略：
  1. 清洗 Markdown 格式符号
  2. 提取中文（jieba 分词）+ 英文 + 代码标识符，保留顺序
  3. 生成相邻 token 的 bigram
  4. 从文件头提取 summary（> summary: ...）
"""
from __future__ import annotations
import re
import jieba
import logging
logging.getLogger("jieba").setLevel(logging.ERROR)

# ── 停词 ──────────────────────────────────────────────────────────────────────
CHINESE_STOP = set("的是在了和有这个上不我也就以要到时会你为那他们可以出来去没有用一个我们")
ENGLISH_STOP = {
    "the","a","an","in","on","at","to","of","and","or","is","are","was","were",
    "be","been","has","have","had","it","this","that","for","with","from","by",
    "as","not","but","if","do","does","did","so","all","any","when","than",
    "then","just","into","can","will","would","could","should","may","might",
    "must","its","we","he","she","they","their","our","your","my","which","who",
}
MIN_EN_LEN = 3

# ── Markdown 清洗 ─────────────────────────────────────────────────────────────
_RE_FENCE     = re.compile(r"```[\s\S]*?```")
_RE_HEADER    = re.compile(r"^#+\s+", re.MULTILINE)
_RE_LINK      = re.compile(r"\[([^\]]+)\]\([^\)]+\)")
_RE_IMG       = re.compile(r"!\[[^\]]*\]\([^\)]+\)")
_RE_HTML_CMT  = re.compile(r"<!--[\s\S]*?-->")
_RE_TABLE_SEP = re.compile(r"^\|[-: ]+\|[-| :]*$", re.MULTILINE)

def clean_markdown(text: str) -> str:
    """剥离 Markdown 格式，保留可读纯文本。"""
    text = _RE_FENCE.sub(" ", text)
    text = _RE_HTML_CMT.sub(" ", text)
    text = _RE_IMG.sub(" ", text)
    text = _RE_LINK.sub(r"\1", text)
    text = _RE_TABLE_SEP.sub(" ", text)
    text = _RE_HEADER.sub(" ", text)
    text = re.sub(r"[*_~`>|\\]", " ", text)
    return re.sub(r"\s+", " ", text).strip()

# ── 标识符拆分 ────────────────────────────────────────────────────────────────
_RE_CAMEL = re.compile(r"[A-Z]?[a-z]+|[A-Z]+(?=[A-Z][a-z]|\d|\b)|[A-Z]+|[0-9]+")

def _split_identifier(s: str) -> list[str]:
    parts = re.split(r"[._/\-:]+", s)
    result = []
    for part in parts:
        words = _RE_CAMEL.findall(part)
        if words:
            result.extend(w.lower() for w in words if len(w) >= MIN_EN_LEN)
        elif len(part) >= MIN_EN_LEN:
            result.append(part.lower())
    return result

# ── 有序 token 提取 ───────────────────────────────────────────────────────────
_RE_IDENT   = re.compile(r"[A-Za-z][A-Za-z0-9_:.\-]*[A-Za-z0-9]|[A-Za-z]{3,}")
_RE_CHINESE = re.compile(r"[\u4e00-\u9fff]+")

def extract_ordered_tokens(text: str) -> list[str]:
    """按出现顺序提取 token（保留顺序用于 bigram 生成，可含重复）。"""
    text = clean_markdown(text)
    tokens: list[str] = []
    pos, length = 0, len(text)

    while pos < length:
        m = _RE_IDENT.match(text, pos)
        if m:
            raw = m.group(0)
            lw = raw.lower()
            if lw not in ENGLISH_STOP:
                parts = _split_identifier(raw)
                if len(parts) > 1:
                    # 保留原始组合词（如 "ArkPipe" → "arkpipe" + "ark" + "pipe"）
                    if len(lw) >= MIN_EN_LEN:
                        tokens.append(lw)
                    tokens.extend(parts)
                else:
                    tokens.append(lw)
            pos = m.end()
            continue

        m = _RE_CHINESE.match(text, pos)
        if m:
            for seg in jieba.cut(m.group(0), cut_all=False):
                seg = seg.strip()
                if len(seg) >= 2 and not all(c in CHINESE_STOP for c in seg):
                    tokens.append(seg)
            pos = m.end()
            continue

        pos += 1
    return tokens

def extract_all_tokens(text: str) -> list[str]:
    """提取 unigram，保序去重。供写入索引时调用。"""
    ordered = extract_ordered_tokens(text)
    return list(dict.fromkeys(ordered))


# 位置权重：词出现在标题/摘要里比正文更重要
_POS_WEIGHTS = [
    ("> summary:", 8),
    ("# ",         10),   # H1（排在 ## 前，先匹配更短前缀）
    ("## ",        5),
    ("### ",       3),
]

def extract_weighted_tokens(content: str) -> "Counter[str]":
    """
    按行提取 token，根据所在位置给 TF 加权：
      H1 标题 ×10 / summary ×8 / H2 ×5 / H3 ×3 / 正文 ×1
    返回 Counter，键为 token，值为加权计数（用作 TF 的分子）。
    """
    from collections import Counter
    tokens: Counter = Counter()
    for line in content.splitlines():
        stripped = line.strip()
        if not stripped:
            continue
        weight = 1
        text = stripped
        for prefix, w in _POS_WEIGHTS:
            if stripped.startswith(prefix):
                text = stripped[len(prefix):]
                weight = w
                break
        for token in extract_all_tokens(text):
            tokens[token] += weight
    return tokens


def extract_summary(content: str) -> str:
    """从 MD 文件头部提取 '> summary: ...' 行（只查前 10 行）。"""
    for line in content.splitlines()[:10]:
        stripped = line.strip()
        if stripped.startswith("> summary:"):
            return stripped[len("> summary:"):].strip()
    return ""


def extract_title(content: str) -> str:
    """提取 MD 文件的第一个 # 标题行（去掉 # 前缀）。"""
    for line in content.splitlines()[:20]:
        stripped = line.strip()
        if stripped.startswith("# "):
            return stripped[2:].strip()
    return ""
