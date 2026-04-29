"""
扫描 Data/Card/*.json，生成卡牌速览 Markdown。
用法: python Tools/gen_card_overview.py
输出: Docs/CardOverview.md
"""

import json, glob, os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CARD_DIR = os.path.join(ROOT, "Data", "Card")
OUT_PATH = os.path.join(ROOT, "Docs", "CardOverview.md")

def mana_str(cost: dict) -> str:
    if not cost:
        return "无"
    return " ".join(f"{k}{v}" for k, v in cost.items())

def main():
    lines = ["# 卡牌速览", ""]

    for path in sorted(glob.glob(os.path.join(CARD_DIR, "*.json"))):
        group = os.path.splitext(os.path.basename(path))[0]
        lines.append(f"## {group}")
        lines.append("")
        with open(path, "r", encoding="utf-8") as f:
            cards = json.load(f)
        for c in cards:
            name = c.get("DisplayName", "?")
            ID = c.get("ID", "?")
            desc = c.get("Desc", "")
            size = c.get("Size", "?")
            cd   = c.get("Cooldown", "?")
            ctype = c.get("CardType", "?")
            mana = mana_str(c.get("ManaCost", {}))
            lines.append(f"- **{name}|{ID}**｜{desc}｜Size{size} CD{cd} {ctype}｜灵耗:{mana}")
        lines.append("")

    os.makedirs(os.path.dirname(OUT_PATH), exist_ok=True)
    with open(OUT_PATH, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))
    print(f"已生成 → {OUT_PATH}  ({sum(1 for l in lines if l.startswith('- '))} 张卡)")

if __name__ == "__main__":
    main()
