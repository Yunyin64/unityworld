"""从战斗 TCA 配置抽样，使用模板 JSON 中的 APIType 和 Weight。"""

import argparse
from collections import Counter
from itertools import product
import json
import math
from pathlib import Path
import random
import secrets
import sys


TOOLS = Path(__file__).resolve().parent
DEFAULT_DATA = TOOLS.parent / "Data"
VALUE_NAMES = {
    "Self": "己方", "Target": "敌方", "ContestA": "对拼方 A", "ContestB": "对拼方 B",
    "All": "己方全场", "TargetAll": "敌方全场", "Random": "己方随机一卡",
    "TargetRandom": "敌方随机一卡", "Other": "己方其他卡", "Adjacent": "场序相邻卡",
    "AboveOne": "场序前一卡", "AboveAll": "上方所有卡", "BelowOne": "场序后一卡",
    "BelowAll": "场序后方所有卡", "Selected": "选中的卡",
    "GridUp": "上侧共边卡", "GridDown": "下侧共边卡", "GridLeft": "左侧共边卡",
    "GridRight": "右侧共边卡", "GridNeighbor": "四向共边卡",
    "Up": "上", "Down": "下", "Left": "左", "Right": "右", "Neighbor": "四向相邻",
    "None": "任意", "Any": "任意", "Jin": "金", "Mu": "木", "Shui": "水", "Huo": "火", "Tu": "土",
    "FaShu": "法术", "FaBao": "法宝", "ZhaoShi": "招式", "Passive": "被动",
    "Wound": "伤势", "GongFa": "功法",
    "Attack": "攻击", "Defend": "防御", "Defense": "防御", "Speed": "速度",
    "Zhan": "斩", "Ci": "刺", "Da": "打", "SheJi": "射击",
    "Shield": "盾", "Block": "格挡", "Dodge": "闪避",
    "Equal": "等于", "AtLeast": "至少", "AtMost": "至多", "Win": "胜", "Lose": "负",
    "GainShield": "获得灵盾", "AddArmor": "获得护甲", "AddFortification": "获得坚甲",
    "RecoverMP": "回复 MP", "ReduceMP": "减少 MP", "QiXue": "气血", "HpMax": "生命上限",
    "AmountMaxAdd": "数量上限", "Ci_Changed": "刺击强化", "Zhan_DamageIn_Changed": "斩击抗性",
    "Deflection": "卸力", "Armor": "护甲",
}
CONDITION_EVENTS = {
    "cond_check_contest_outcome": {"OnContestOverflow"},
    "cond_was_card_used_on_side": {"OnUse"},
    "cond_was_card_displaced": {"OnCardDisplaced"},
    "cond_check_mana_charged": {"OnManaCharged"},
    "cond_check_wound_event": {"OnWoundGained", "OnWoundHealed"},
    "cond_check_mana_used": {"OnManaUsed"},
    "cond_check_element_stack_change": {"OnElementStackChanged"},
    "cond_card_moved_into_side": {"OnCardDisplaced"},
    "cond_check_shield_exhaustion": {"OnShieldExhausted"},
}
EVENT_REPEATS = {
    "OnManaDraw": {"Draw"},
    "OnMPChanged": {"Draw", "RecoverMP", "ReduceMP", "Convert"},
    "OnHeal": {"Heal"},
    "OnDamage": {"SelfDamage"},
    "OnCardDisplaced": {"SlideCard"},
    "OnDisplaceOtherCard": {"SlideCard"},
    "OnCardLeave": {"RemoveRandomWound"},
    "OnWoundHealed": {"RemoveRandomWound"},
}
BLOCKED_SLIDE = {
    "GridUp": "Down", "GridDown": "Up", "GridLeft": "Right", "GridRight": "Left",
}
ENEMY_DOMAINS = {"Target", "TargetAll", "TargetRandom"}
ALLY_ACTIONS = {
    "AddArmor", "AddArtifactPower", "AddDeflection", "AddFortification", "AddNpcBuff",
    "AddStatBuff", "Charge", "Convert", "Draw", "GainShield", "Haste",
    "Heal", "RecoverMP", "RemoveRandomWound",
}
ENEMY_ACTIONS = {"ArmorBreak", "Freeze", "ReduceMana", "ReduceMP", "SelfDamage", "Slow"}


def read_json(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def load_pool(paths):
    """直接读取项目配置的权重。缺省状态沿用目录的 Ready 规则。"""
    pool = []
    seen = set()
    for path in paths:
        for raw_entry in read_json(path):
            entry = {key.lower(): value for key, value in raw_entry.items()}
            entry_id = entry["id"]
            if entry_id in seen:
                raise ValueError(f"{path} 含重复条目 {entry_id}")
            seen.add(entry_id)
            status = entry.get("status", "Ready")
            if status not in {"Ready", "Pending"}:
                raise ValueError(f"{path} 的 {entry_id} 状态应为 Ready 或 Pending")
            if status == "Pending":
                continue
            if "weight" not in entry:
                raise ValueError(f"{path} 的 {entry_id} 缺少抽取权重")
            weight = validate_weight(entry["weight"], entry_id)
            if weight > 0:
                pool.append((entry, weight))
    return pool


def load_typed_pool(paths, allowed_types):
    pool = load_pool(paths)
    for entry, _ in pool:
        if entry["id"] == "cond_none" and "Condition" in allowed_types:
            entry["apitype"] = None
            continue
        api_type = entry.get("apitype")
        if api_type not in allowed_types:
            expected = "、".join(allowed_types)
            raise ValueError(f"{entry['id']} 的 APIType 为 {api_type or '未填写'}，预期为 {expected}")
    return pool


def validate_weight(weight, entry_id):
    if type(weight) not in (int, float) or not math.isfinite(weight) or weight < 0:
        raise ValueError(f"{entry_id} 的抽样权重须为有限非负数")
    return weight


def draw_pool(rng, pool, count, label):
    if not pool:
        raise ValueError(f"{label} 没有正权重条目")
    entries, weights = zip(*pool)
    return rng.choices(entries, weights=weights, k=count)


def parameter_options(entry):
    options = {}
    for param in entry.get("paramdefs", []):
        if not param["Value"]:
            raise ValueError(f"{entry['id']} 的参数 {param['Name']} 没有可选值")
        options[param["Name"]] = param["Value"]
    return options


def pick_lifetime(rng, choices):
    # 单次和持续原本是等权模板，合并后先取时机，再取持续版的具体期限。
    sustained = [choice for choice in choices if choice != "下一次"]
    groups = ([["下一次"]] if "下一次" in choices else []) + ([sustained] if sustained else [])
    return rng.choice(rng.choice(groups))


def draw_parameters(rng, entry):
    # 先抽模板。参数选项增加不会抬高模板的权重。
    return {
        name: pick_lifetime(rng, values) if name == "Lifetime" else rng.choice(values)
        for name, values in parameter_options(entry).items()
    }


def render_entry(entry, values, card_anchor=False):
    if entry["id"] in {"trigger_none", "cond_none"}:
        return "∅", ""
    params = [f"{name}={json.dumps(value, ensure_ascii=False)}" for name, value in values.items()]
    label = entry["id"]
    node = f"{label}({', '.join(params)})" if params else label
    display_values = {
        name: VALUE_NAMES.get(value, value) if isinstance(value, str) else
        json.dumps(value) if isinstance(value, bool) else "−X" if value < 0 else "X"
        for name, value in values.items()
    }
    if values.get("CardDomain") == "Self":
        display_values["CardDomain"] = "本卡"
    if label in {"cond_all_cards_in_domain", "cond_random_card"}:
        display_values["Domain"] = "敌方" if values["Domain"] in ENEMY_DOMAINS else "己方"
    if values.get("Domain") == "Self" and entry.get("funcname") in {
        "Freeze", "SlideCard", "ChangeEquipmentStat", "AddCardStatBuff",
    }:
        display_values["Domain"] = "本卡"
    description = entry.get("desc", entry.get("displayname", label))
    if card_anchor:
        description = description.replace("来源卡", "此卡")
    if card_anchor and label in {
        "cond_select_directional_cards", "cond_count_directional_cards", "cond_has_directional_card",
    }:
        side = "四向" if values["Direction"] == "Neighbor" else "{Direction}侧"
        kind = "友方" if values["Keyword"] == "Any" else "{Keyword}"
        if entry["apitype"] == "Scope":
            description = f"选取与此卡{side}共边的{kind}卡"
        elif entry["apitype"] == "Scale":
            description = f"每有一张与此卡{side}共边的{kind}卡"
        else:
            description = f"若此卡{side}有共边{kind}卡"
    if label == "cond_check_contest_outcome":
        description = "若己方对拼获胜" if values["Outcome"] == "Win" else "若己方对拼落败"
    if card_anchor and label == "cond_was_card_used_on_side":
        side = "四向" if values["Direction"] == "Neighbor" else "{Direction}侧"
        kind = "友方" if values["Keyword"] == "Any" else "{Keyword}"
        description = f"若发动的是此卡{side}的共边{kind}卡"
    if card_anchor and label == "cond_was_card_displaced":
        description = "若被移动的是此卡"
    if label == "cond_check_consume_count":
        description = "若此卡剩余消耗{Comparison} X 次"
        if values["Amount"] == 0:
            description = "若此卡已耗尽" if values["Comparison"] in {"Equal", "AtMost"} else "此卡是消耗卡"
    if "Rounds" in values:
        if values["Rounds"] == 0:
            description = description.replace("持续{Rounds}轮", "持续至战斗结束")
            description = description.replace("在{Rounds}轮内", "在本场战斗中")
        description = description.replace("，0轮表示本场", "")
    if entry.get("funcname") == "RemoveRandomWound":
        limit = "恰好 X 格" if values["Exact"] else "至多 X 格"
        description = f"移除{{Domain}}场上随机一张{limit}的伤势卡"
    if entry.get("funcname") in {"AddElementBuff", "RemoveElementBuff"} and "IsDebuff" in values:
        display_values["IsDebuff"] = "减益" if values["IsDebuff"] else "增益"
        if entry["funcname"] == "AddElementBuff":
            description = "给{Domain}施加{Count}层{Element}五行{IsDebuff}"
            if values["Element"] == "None":
                description = "给{Domain}随机施加{Count}层五行{IsDebuff}"
        else:
            description = "清除{Domain}至多{Count}层{Element}五行{IsDebuff}"
    if entry.get("funcname") == "Freeze" and values["Domain"] == "TargetRandom":
        description = "随机冻结一张未冻结且未耗尽的敌方主动卡"
    if entry.get("funcname") == "SlideCard" and values["Domain"] == "TargetRandom":
        description = "使敌方一张随机可滑移卡向{Direction}滑移"
    if entry.get("funcname") == "ChangeEquipmentStat":
        display_values["Delta"] = "降低 X" if values["Delta"] < 0 else "提高 X"
        duration = "持续至战斗结束" if values["Rounds"] == 0 else "持续 X 轮"
        description = "使{Domain}对应装备的{Stat}{Delta}，" + duration
    if entry.get("funcname") == "ChangeAffinity":
        display_values["Delta"] = "降低 X" if values["Delta"] < 0 else "提高 X"
        description = description.replace("改变{Delta}", "{Delta}")
    if entry.get("funcname") in {"ModifyContest", "NextContestBonus"}:
        display_values["Delta"] = "降低 X" if values["Delta"] < 0 else "提高 X"
        display_values["Kind"] = "" if values["Kind"] == "Any" else display_values["Kind"]
        if entry["funcname"] == "ModifyContest":
            description = "使{Domain}当前{Kind}拼点{Delta}"
        else:
            duration = "本场" if values["Rounds"] == 0 else "X 轮内"
            description = duration + "，使{Domain}下一次{Kind}实际对拼的拼点{Delta}"
        if values["CardDomain"] != "Any":
            description += "，来源仅限{CardDomain}"
    if entry.get("funcname") == "AddNpcBuff" and "Duration" in values:
        display_values["Duration"] = "本场" if values["Duration"] < 0 else "X Tick"
    if values.get("StatId") == "HpMax":
        description = "{Domain}生命上限提高 X 点"
    return node, description.format_map(display_values)


def tags_conflict(left, right):
    return bool(
        set(left.get("conflicttags", ())).intersection(right.get("tags", ()))
        or set(right.get("conflicttags", ())).intersection(left.get("tags", ()))
    )


def condition_fits(trigger, condition):
    event = trigger["id"]
    required = CONDITION_EVENTS.get(condition["id"])
    if required is not None and event not in required:
        return False
    if condition["id"] == "cond_check_consume_count" and event not in {"trigger_none", "OnUse"}:
        return False
    return not tags_conflict(trigger, condition)


def action_fits(event, condition, action, params, selection):
    """只判断独立效果的上下文和明确目标，复杂交换效果留给原始抽样。"""
    func = action["funcname"]
    domain = params.get("Domain")
    card_domain = params.get("CardDomain")
    selected_domain, anchor_direction, selected_keyword = selection
    target_is_enemy = (selected_domain if domain == "Selected" else domain) in ENEMY_DOMAINS
    if action["apitype"] in {"Contest", "Aura"} and event != "trigger_none":
        return False
    has_card_selection = condition["apitype"] == "Scope"
    uses_selected_cards = "Selected" in (domain, card_domain)
    if has_card_selection != uses_selected_cards:
        return False
    if condition["apitype"] == "Scale":
        # 计数要传给明确的数值参数，例如光环的 Delta。
        if type(params.get(action.get("scaleparam"))) not in (int, float):
            return False
    if func in EVENT_REPEATS.get(event, ()):
        return False
    if func == "ModifyContest" and event != "OnContest":
        return False
    # 用角色归属明确改哪一方。两名参战者的 A/B 索引只用于原始预览。
    if domain in {"ContestA", "ContestB"}:
        return False
    if card_domain == "Selected" and target_is_enemy != (selected_domain in ENEMY_DOMAINS):
        return False
    if card_domain in {"Adjacent", "Self"} and target_is_enemy:
        return False
    if card_domain == "Self":
        return False  # 单效果只登记加值的卡自身没有拼点入口。
    if func == "AddCardStatBuff":
        return False  # 当前仅有数量上限模板，普通卡没有这个读点。
    if func == "Freeze" and domain == "TargetAll" and event not in {"trigger_none", "OnExhausted"}:
        return False  # 全场冻结需要投入本卡发动机会，或消耗最后一次机会。
    if func == "ChangeEquipmentStat":
        if domain == "Self" or (domain == "Selected" and selected_keyword not in {None, "Any", "FaBao", "ZhaoShi"}):
            return False  # 目标必须可能是装备或绑定装备的招式。
    if func == "SlideCard":
        direction = params["Direction"]
        # 朝已经确定有共边卡的一侧推本卡，或把共边目标推向本卡，第一格就冲突。
        if domain == "Self" and direction == anchor_direction:
            return False
        if BLOCKED_SLIDE.get(domain) == direction:
            return False
        if domain == "Selected" and BLOCKED_SLIDE.get(f"Grid{anchor_direction}") == direction:
            return False
    if action["id"] == "action_add_artifact_power" and params["Rounds"] == 1 and event != "OnRoundStart":
        return False
    if event == "OnRoundEnd":
        if func == "AddFortification" or params.get("Rounds") == 1:
            return False
    if func in ALLY_ACTIONS:
        return not target_is_enemy
    if func in ENEMY_ACTIONS:
        return target_is_enemy
    if func == "ChangeAffinity":
        # 己方也可以压低不需要的元素，给其余五行腾出抽取权重。
        return not target_is_enemy or params["Delta"] < 0
    if func in {"ChangeEquipmentStat", "ModifyContest", "NextContestBonus"}:
        return target_is_enemy == (params["Delta"] < 0)
    if func == "AddElementBuff":
        debuff = params.get("IsDebuff", action["id"].endswith("_debuff"))
        return target_is_enemy == debuff
    if func == "RemoveElementBuff":
        return target_is_enemy != params["IsDebuff"]
    return True


def draw_compatible(rng, triggers, conditions, actions, count):
    # 筛掉没有合法后续的 T/C，再按 JSON 权重逐栏抽取。
    action_cache = {}
    condition_options = {
        condition["id"]: parameter_options(condition) for condition, _ in conditions
    }
    side_use = condition_options.get("cond_was_card_used_on_side")
    if side_use:
        side_use["Keyword"] = [keyword for keyword in side_use["Keyword"] if keyword != "Passive"]
    action_options = [
        (action, weight, parameter_options(action)) for action, weight in actions
    ]
    trigger_pool = []
    for trigger, trigger_weight in triggers:
        condition_pool = []
        for condition, condition_weight in conditions:
            if not condition_fits(trigger, condition):
                continue
            options = condition_options[condition["id"]]
            variants = []
            for values in product(*options.values()):
                picked = dict(zip(options, values))
                selection = (
                    picked.get("Domain", "Self"),
                    picked.get("Direction") if condition["id"] in {
                        "cond_select_directional_cards", "cond_has_directional_card", "cond_count_directional_cards",
                    } else None,
                    picked.get("Keyword") if condition["apitype"] == "Scope" else None,
                )
                key = (trigger["id"], condition["id"], selection)
                if key not in action_cache:
                    action_cache[key] = compatible_actions(trigger, condition, selection, action_options)
                pool, available_params = action_cache[key]
                if pool:
                    variants.append((picked, pool, available_params))
            if variants:
                condition_pool.append(((condition, variants), condition_weight))
        if condition_pool:
            trigger_pool.append(((trigger, condition_pool), trigger_weight))

    for trigger, condition_pool in draw_pool(rng, trigger_pool, count, "可组合的 T"):
        event = trigger["id"]
        condition, variants = draw_pool(rng, condition_pool, 1, event + " 的 C")[0]
        picked, pool, available_params = rng.choice(variants)
        action = draw_pool(rng, pool, 1, event + " / " + condition["id"] + " 的 A")[0]
        params = available_params[action["id"]]
        if "Lifetime" in params[0]:
            lifetime = pick_lifetime(rng, list(dict.fromkeys(values["Lifetime"] for values in params)))
            params = [values for values in params if values["Lifetime"] == lifetime]
        yield trigger, condition, action, picked, rng.choice(params)


def compatible_actions(trigger, condition, selection, action_options):
    pool = []
    available_params = {}
    for action, weight, options in action_options:
        if tags_conflict(action, trigger) or tags_conflict(action, condition):
            continue
        legal = []
        for values in product(*options.values()):
            params = dict(zip(options, values))
            if action_fits(trigger["id"], condition, action, params, selection):
                legal.append(params)
        if legal:
            pool.append((action, weight))
            available_params[action["id"]] = legal
    return pool, available_params


def sample(data, count, seed, raw=False):
    triggers = load_pool([data / "TriggerDefines.json"])
    conditions = load_typed_pool(sorted((data / "Condition").glob("*.json")), ("Condition", "Scope", "Scale"))
    actions = load_typed_pool(sorted((data / "Action").glob("*.json")), ("Action", "Contest", "Aura"))

    rng = random.Random(seed)
    if raw:
        triples = zip(
            draw_pool(rng, triggers, count, "T"),
            draw_pool(rng, conditions, count, "C"),
            draw_pool(rng, actions, count, "A"),
        )
        draws = ((t, c, a, draw_parameters(rng, c), draw_parameters(rng, a)) for t, c, a in triples)
    else:
        draws = draw_compatible(rng, triggers, conditions, actions, count)
    totals = Counter()
    results = []
    for number, (trigger, condition, action, c_params, a_params) in enumerate(draws, start=1):
        trigger_prefix = "T+" if trigger["id"] != "trigger_none" else ""
        condition_type = condition["apitype"]
        shape = trigger_prefix + ("C+" if condition_type else "") + "A"
        typed_shape = trigger_prefix + (f"C-{condition_type}+" if condition_type else "") + f"A-{action['apitype']}"
        totals[shape] += 1
        trigger_node, _ = render_entry(trigger, {})
        condition_node, condition_desc = render_entry(condition, c_params, card_anchor=not raw)
        action_node, action_desc = render_entry(action, a_params)
        trigger_desc = "" if trigger_node == "∅" else trigger["displayname"]
        if not raw and trigger["id"] in {
            "OnUse", "OnApply", "OnAttack", "OnDefend", "OnStraight",
            "OnBaseManaDraw", "OnManaDraw", "OnHeal",
            "OnDamage", "OnCardDisplaced", "OnDisplaceOtherCard", "OnCardLeave",
        }:
            trigger_desc = "己方" + trigger_desc
        if not raw and trigger["id"] == "OnMPChanged":
            trigger_desc = "己方 " + trigger_desc
        if not raw and trigger["id"] == "OnExhausted":
            trigger_desc = "本卡耗尽时"
        if not raw and trigger["id"] == "OnUse" and condition["id"] == "cond_check_consume_count":
            trigger_desc = "此卡发动后"
        description = "，".join(part for part in (trigger_desc, condition_desc, action_desc) if part)
        results.append(
            f"{number:03} {shape:5} [{typed_shape}] | {trigger_node} | {condition_node} | {action_node} | {description}"
        )

    return [
        f"种子 {seed} | 共 {count} 条 | {'原始抽样' if raw else '成卡候选'} | " + " | ".join(
            f"{shape} {totals[shape]}" for shape in ("T+A", "T+C+A", "C+A", "A")
        ),
        "编号 结构 [API 类型] | 触发 | 条件 | 动作 | 中文效果",
        *results,
    ]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--count", type=int, default=100, help="抽样条数，默认 100")
    parser.add_argument("--seed", type=int, help="固定种子，省略时生成随机种子")
    parser.add_argument("--data-dir", type=Path, default=DEFAULT_DATA, help="替代 TCA 数据目录")
    parser.add_argument("--raw", action="store_true", help="独立抽取 T/C/A，用于排查不合理的组合")
    parser.add_argument("--output", type=Path, help="写入新的 UTF-8 文本文件")
    args = parser.parse_args()
    if args.count < 1:
        parser.error("--count 必须大于零")

    seed = args.seed if args.seed is not None else secrets.randbits(64)
    try:
        lines = sample(args.data_dir, args.count, seed, args.raw)
        output = "\n".join(lines) + "\n"
        if args.output:
            with args.output.open("x", encoding="utf-8", newline="\n") as file:
                file.write(output)
        else:
            if hasattr(sys.stdout, "reconfigure"):
                sys.stdout.reconfigure(encoding="utf-8")
            print(output, end="")
    except (OSError, KeyError, TypeError, ValueError) as error:
        parser.exit(1, f"抽样失败：{error}\n")


if __name__ == "__main__":
    main()
