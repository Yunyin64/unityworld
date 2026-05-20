"""
convert_cards_to_lua.py
━━━━━━━━━━━━━━━━━━━━━━
批量将现有 CardDefine (JSON) 转为 Lua 卡牌脚本。
读取 Card/*.json + Effect/*.json + Action/*.json，
为每张卡生成 Data/LuaCards/{card_id}.lua，并清空 EffectIds。

用法：
    cd project_root
    python tools/convert_cards_to_lua.py
"""

import json
import os
from pathlib import Path

# ── 路径配置 ────────────────────────────────────────────
BASE_DIR = Path(__file__).parent.parent
DATA_DIR = BASE_DIR / "Data"
CARD_DIR = DATA_DIR / "Card"
EFFECT_DIR = DATA_DIR / "Effect"
ACTION_DIR = DATA_DIR / "Action"
LUA_DIR = DATA_DIR / "LuaCards"
CONDITION_FILE = DATA_DIR / "ConditionDefines.json"

# ── Trigger → Hook 映射 ────────────────────────────────
TRIGGER_TO_HOOK = {
    "trigger_on_use": "OnUse",
    "trigger_on_attack": "OnAttack",
    "trigger_on_contest_win": "ContestWin",
    "trigger_on_contest_lose": "ContestLose",
    "trigger_on_dominate": "OnDominate",
    "trigger_on_dominated": "OnDominated",
    "trigger_on_hit_body": "OnHitBody",
    "trigger_after_card_use": "OnAfterCardUse",
}

# ── Keyword 特殊 Effect ────────────────────────────────
KEYWORD_EFFECTS = {"kw_initial": "Initial", "kw_weapon": "Weapon"}


def load_all_json(directory: Path, pattern="*.json") -> list:
    """加载目录下所有 JSON 文件，返回合并的列表"""
    items = []
    if not directory.exists():
        return items
    for f in sorted(directory.glob(pattern)):
        try:
            data = json.loads(f.read_text(encoding="utf-8"))
            if isinstance(data, list):
                items.extend(data)
        except Exception as e:
            print(f"  [WARN] 无法解析 {f.name}: {e}")
    return items


def build_index(items: list) -> dict:
    """将列表按 ID 索引"""
    return {item["ID"]: item for item in items if "ID" in item}


def format_params_for_lua(func_name: str, params: list) -> str:
    """将 ActionDefine.Params 格式化为 Lua 函数调用参数"""
    parts = []
    for p in params:
        if isinstance(p, str):
            parts.append(f'"{p}"')
        elif isinstance(p, (int, float)):
            parts.append(str(int(p)) if isinstance(p, int) or p == int(p) else str(p))
        else:
            parts.append(str(p))
    return ", ".join(parts)


def generate_lua_for_card(card: dict, effects_idx: dict, actions_idx: dict) -> str | None:
    """为一张卡生成 Lua 脚本内容，返回 None 表示无需生成"""
    card_id = card["ID"]
    display_name = card.get("DisplayName", card_id)
    desc = card.get("Desc", "")
    effect_ids = card.get("EffectIds", [])

    if not effect_ids:
        return None  # 已经是空的（如 whirlwind 已手动处理）

    keywords = []
    on_use_actions = []  # (func_name, params, desc)
    passive_hooks = {}   # hook_name -> [(func_name, params, desc)]

    for eff_id in effect_ids:
        # 处理 Keyword
        if eff_id in KEYWORD_EFFECTS:
            keywords.append(KEYWORD_EFFECTS[eff_id])
            continue

        eff = effects_idx.get(eff_id)
        if not eff:
            print(f"  [WARN] {card_id}: 找不到 Effect '{eff_id}'，跳过")
            continue

        trigger_id = eff.get("TriggerId", "trigger_on_use")
        hook_name = TRIGGER_TO_HOOK.get(trigger_id, "OnUse")
        action_ids = eff.get("ActionIds", [])

        for act_id in action_ids:
            act = actions_idx.get(act_id)
            if not act:
                print(f"  [WARN] {card_id}: 找不到 Action '{act_id}'，跳过")
                continue

            func_name = act.get("FuncName", "")
            params = act.get("Params", [])
            act_desc = act.get("Desc", "")

            if not func_name:
                print(f"  [WARN] {card_id}: Action '{act_id}' 无 FuncName，跳过")
                continue

            entry = (func_name, params, act_desc)
            if hook_name == "OnUse":
                on_use_actions.append(entry)
            else:
                passive_hooks.setdefault(hook_name, []).append(entry)

    # 如果完全没有有效 action，跳过
    if not on_use_actions and not passive_hooks and not keywords:
        print(f"  [SKIP] {card_id}: 没有有效内容可生成")
        return None

    # ── 生成 Lua 代码 ────────────────────────────────────
    lines = []
    lines.append(f"-- {card_id}.lua")
    lines.append(f"-- {display_name}：{desc}")
    lines.append("")

    # Keywords
    if keywords:
        kw_str = ", ".join(f'"{kw}"' for kw in keywords)
        lines.append(f"CombatCard.Keywords = {{{kw_str}}}")
        lines.append("")

    # OnUse
    if on_use_actions:
        lines.append("-- 使用时")
        lines.append("function CombatCard:OnUse(ctx)")
        for func_name, params, act_desc in on_use_actions:
            lua_params = format_params_for_lua(func_name, params)
            if act_desc:
                lines.append(f"    -- {act_desc}")
            lines.append(f"    {func_name}(ctx, {lua_params})")
        lines.append("end")

    # Passive hooks
    for hook_name, actions in passive_hooks.items():
        lines.append("")
        lines.append(f"-- 被动：{hook_name}")
        lines.append(f"function CombatCard:{hook_name}(ctx)")
        for func_name, params, act_desc in actions:
            lua_params = format_params_for_lua(func_name, params)
            if act_desc:
                lines.append(f"    -- {act_desc}")
            lines.append(f"    {func_name}(ctx, {lua_params})")
        lines.append("end")

    lines.append("")
    return "\n".join(lines)


def main():
    print("═══ convert_cards_to_lua.py ═══")
    print(f"  DATA_DIR: {DATA_DIR}")

    # 加载所有数据
    print("\n[1] 加载数据...")
    effects = load_all_json(EFFECT_DIR)
    actions = load_all_json(ACTION_DIR)
    effects_idx = build_index(effects)
    actions_idx = build_index(actions)
    print(f"  Effects: {len(effects_idx)} 条")
    print(f"  Actions: {len(actions_idx)} 条")

    # 加载所有卡牌
    card_files = sorted(CARD_DIR.glob("*.json"))
    all_cards = []
    for f in card_files:
        try:
            data = json.loads(f.read_text(encoding="utf-8"))
            if isinstance(data, list):
                all_cards.extend(data)
        except Exception as e:
            print(f"  [WARN] 无法解析 {f.name}: {e}")
    print(f"  Cards: {len(all_cards)} 张（来自 {len(card_files)} 个文件）")

    # 确保 LuaCards 目录存在
    LUA_DIR.mkdir(parents=True, exist_ok=True)

    # 生成 Lua 文件
    print("\n[2] 生成 Lua 脚本...")
    generated = 0
    skipped = 0
    for card in all_cards:
        card_id = card["ID"]
        lua_path = LUA_DIR / f"{card_id}.lua"

        # 如果已有手写文件，跳过
        if lua_path.exists():
            print(f"  [EXIST] {card_id}.lua 已存在，跳过")
            skipped += 1
            continue

        lua_content = generate_lua_for_card(card, effects_idx, actions_idx)
        if lua_content:
            lua_path.write_text(lua_content, encoding="utf-8")
            print(f"  [OK] {card_id}.lua")
            generated += 1
        else:
            skipped += 1

    print(f"\n  生成: {generated} 个, 跳过: {skipped} 个")

    # 清空所有 CardDefine 中的 EffectIds
    print("\n[3] 清空 CardDefine 中的 EffectIds...")
    for f in card_files:
        try:
            data = json.loads(f.read_text(encoding="utf-8"))
            if not isinstance(data, list):
                continue
            modified = False
            for card in data:
                if card.get("EffectIds") and len(card["EffectIds"]) > 0:
                    card["EffectIds"] = []
                    modified = True
            if modified:
                f.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")
                print(f"  [OK] {f.name} - EffectIds 已清空")
        except Exception as e:
            print(f"  [ERR] {f.name}: {e}")

    print("\n═══ 完成 ═══")


if __name__ == "__main__":
    main()
