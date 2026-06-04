"""
Hover CLI for vscode-hover-ext — queries DataManager via common-cli.exe.

Usage:
  One-shot mode:  python hover_cli.py "card_fabao_jian"
  Daemon mode:    python hover_cli.py --daemon
                  (stdin/stdout line protocol)

Requires: DataManager GUI running with workspace loaded, common-cli.exe in PATH.
"""
import sys
import os
import json
import subprocess

# Force UTF-8 output on Windows
os.environ["PYTHONIOENCODING"] = "utf-8"
sys.stdout.reconfigure(encoding="utf-8")

def query_datamanager(key: str) -> str:
    """Call common-cli.exe to query by ID, return formatted Markdown or empty string."""
    try:
        result = subprocess.run(
            ["common-cli.exe", "DataManager", "auto", "query", "--ID", key],
            capture_output=True, timeout=5, encoding="utf-8", errors="replace"
        )
        if result.returncode != 0 or not result.stdout or not result.stdout.strip():
            return ""

        data = json.loads(result.stdout)
        return format_as_markdown(data)
    except (subprocess.TimeoutExpired, json.JSONDecodeError, FileNotFoundError, OSError):
        return ""


def format_as_markdown(data: dict) -> str:
    """Format CLI JSON response into compact Markdown for hover display."""
    file_name = data.get("_file", "")
    entry = data.get("data", {})
    if not entry:
        return ""

    lines = []

    # Line 1: DisplayName
    display_name = entry.get("DisplayName", entry.get("ID", ""))
    lines.append(f"**{display_name}**")

    # Line 2: Desc
    desc = entry.get("Desc", "")
    if desc:
        lines.append(f"{desc}")

    # Line 3: Keywords
    keywords = entry.get("Keywords", [])
    if keywords:
        lines.append(f"Keywords\t{json.dumps(keywords, ensure_ascii=False)}")

    # Line 4: Rarity | Size (compact pair)
    rarity = entry.get("Rarity", "")
    size = entry.get("Size", "")
    pair_parts = []
    if rarity != "":
        pair_parts.append(f"Rarity\t{rarity}")
    if size != "":
        pair_parts.append(f"Size\t{size}")
    if pair_parts:
        lines.append(" | ".join(pair_parts))

    # Line 5: Cooldown | ManaCost (compact pair)
    cooldown = entry.get("Cooldown", "")
    mana_cost = entry.get("ManaCost", {})
    pair_parts2 = []
    if cooldown != "":
        pair_parts2.append(f"Cooldown {cooldown}")
    if mana_cost is not None:
        pair_parts2.append(f"ManaCost\t{json.dumps(mana_cost, ensure_ascii=False)}")
    if pair_parts2:
        lines.append(" | ".join(pair_parts2))

    # Line 6: Tags
    tags = entry.get("Tags", [])
    if tags:
        lines.append(f"Tags\t{json.dumps(tags, ensure_ascii=False)}")

    return "\n\n".join(lines)


# ─── One-shot mode ────────────────────────────────────────────────────────
def one_shot():
    if len(sys.argv) < 2:
        sys.exit(0)
    key = sys.argv[1]
    result = query_datamanager(key)
    if result:
        print(result)


# ─── Daemon mode (stdin/stdout line protocol) ─────────────────────────────
def daemon():
    """Read one line from stdin, write one line to stdout. \\n for literal newlines."""
    for line in sys.stdin:
        key = line.strip()
        if not key:
            print("", flush=True)
            continue
        result = query_datamanager(key)
        # Escape real newlines to \\n for the line protocol
        print(result.replace("\n", "\\n"), flush=True)


if __name__ == "__main__":
    if "--daemon" in sys.argv:
        daemon()
    else:
        one_shot()
