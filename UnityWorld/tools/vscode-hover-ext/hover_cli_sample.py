"""
Sample CLI for vscode-hover-ext testing.

Usage:
  One-shot mode:  python hover_cli_sample.py "some_string"
  Daemon mode:    python hover_cli_sample.py --daemon
                  (then write lines to stdin, read responses from stdout)

Replace the lookup logic with your real data source.
"""
import sys
import json

# ─── Your lookup logic goes here ──────────────────────────────────────────
SAMPLE_DATA = {
    "sword_001": "**Sword of Dawn**\n\nDamage: 15\nType: Physical",
    "fire_ball": "**Fire Ball**\n\nCost: 3 MP\nDamage: 20\nElement: Fire",
    "hp_potion": "**Health Potion**\n\nRestore: 50 HP",
}


def lookup(key: str) -> str:
    """Return info string for given key. Empty string = no info."""
    return SAMPLE_DATA.get(key, "")


# ─── One-shot mode ────────────────────────────────────────────────────────
def one_shot():
    if len(sys.argv) < 2:
        sys.exit(0)
    key = sys.argv[1]
    result = lookup(key)
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
        result = lookup(key)
        # Escape real newlines to \\n for the line protocol
        print(result.replace("\n", "\\n"), flush=True)


if __name__ == "__main__":
    if "--daemon" in sys.argv:
        daemon()
    else:
        one_shot()
