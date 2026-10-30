#!/usr/bin/env python3
"""随机摘取 Markdown：优先 ##，正文超过 400 字时改取该段内的 ###。

需要 Python 3.10+，仅使用标准库。示例（技能目录运行）：
    python scripts/extract_md_excerpt.py /path/to/渊海子平.md
    python scripts/extract_md_excerpt.py /path/to/滴天髓.md --seed 42 -o excerpt.md
"""

import argparse
from dataclasses import dataclass
from pathlib import Path
import random
import re
import sys


HEADING = re.compile(r"^ {0,3}(#{1,6})(?:[ \t]+(.*?)|[ \t]*)$")
FENCE = re.compile(r"^ {0,3}(`{3,}|~{3,})(.*)$")


@dataclass
class Section:
    level: int
    title: str
    start: int
    body_start: int
    end: int
    source: str

    @property
    def body(self) -> str:
        return self.source[self.body_start:self.end].strip()

    @property
    def text(self) -> str:
        return self.source[self.start:self.end].strip()

    @property
    def char_count(self) -> int:
        # 含标点和内嵌 Markdown 标记，不含起始标题、空格和换行。
        return sum(not char.isspace() for char in self.body)


def parse_sections(text: str) -> list[Section]:
    """识别 ATX 标题；同级或更高级标题结束当前小节，文末也算边界。"""
    sections: list[Section] = []
    stack: list[Section] = []
    lines = text.splitlines(keepends=True)
    frontmatter_end = -1
    if lines and lines[0].strip() == "---":
        frontmatter_end = next(
            (i for i in range(1, len(lines)) if lines[i].strip() in ("---", "...")),
            -1,
        )
    fence = ""
    offset = 0
    for index, raw in enumerate(lines):
        start = offset
        offset += len(raw)
        line = raw.rstrip("\r\n")
        if index <= frontmatter_end:
            continue
        if fence:
            if re.fullmatch(r" {0,3}" + re.escape(fence[0]) + "{" + str(len(fence)) + r",}[ \t]*", line):
                fence = ""
            continue
        match = FENCE.match(line)
        if match and not (match[1][0] == "`" and "`" in match[2]):
            fence = match[1]
            continue
        match = HEADING.match(line)
        if not match:
            continue
        level = len(match[1])
        title = re.sub(r"[ \t]+#+[ \t]*$", "", match[2] or "").strip()
        while stack and stack[-1].level >= level:
            stack.pop().end = start
        section = Section(level, title, start, offset, len(text), text)
        sections.append(section)
        stack.append(section)
    return sections


def extract_excerpt(text: str, max_chars: int = 400, seed: int | None = None) -> Section:
    """先随机选二级段落；超长时仅在选中段落内随机选三级小节。"""
    if max_chars <= 0:
        raise ValueError("字数阈值必须大于 0。")
    sections = parse_sections(text.lstrip("\ufeff"))
    candidates = [s for s in sections if s.level == 2 and s.body]
    if not candidates:
        candidates = [s for s in sections if s.level == 3 and s.body]
    if not candidates:
        raise ValueError("文档中没有含正文的 ## 或 ### 标题段落。")
    rng = random.Random(seed)
    selected = rng.choice(candidates)
    if selected.level == 2 and selected.char_count > max_chars:
        children = [
            s for s in sections
            if s.level == 3 and selected.start < s.start < selected.end and s.body
        ]
        if children:
            selected = rng.choice(children)
    return selected


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("input", type=Path, help="UTF-8 Markdown 文件路径")
    parser.add_argument("--max-chars", type=int, default=400, help="改取三级小节的字数阈值，默认 400")
    parser.add_argument("--seed", type=int, help="可选随机种子，方便复现同一段落")
    parser.add_argument("-o", "--output", type=Path, help="保存为 UTF-8 文件；省略时输出到终端")
    args = parser.parse_args()
    # Windows 管道默认编码可能无法表示古籍中的生僻字。
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8")
    try:
        if args.output and (
            args.output.resolve() == args.input.resolve()
            or (args.output.exists() and args.input.exists() and args.output.samefile(args.input))
        ):
            raise ValueError("输出文件不能与输入文件相同。")
        result = extract_excerpt(args.input.read_text(encoding="utf-8-sig"), args.max_chars, args.seed)
        if args.output:
            args.output.write_text(result.text + "\n", encoding="utf-8")
        else:
            print(result.text)
        print(f"选中：{'#' * result.level} {result.title}（正文 {result.char_count} 字）", file=sys.stderr)
        if result.char_count > args.max_chars:
            print("提示：没有可继续细分的三级小节，或三级小节仍超长；已保留完整段落。", file=sys.stderr)
    except (OSError, UnicodeError, ValueError) as exc:
        print(f"错误：{exc}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
