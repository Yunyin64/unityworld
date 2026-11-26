"""独立进程驱动单场后端，汇总同批构筑的测量结果。"""
import argparse
import hashlib
import json
import math
import os
import re
import shutil
import subprocess
import sys
import tempfile
import uuid
from collections import Counter
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
from statistics import mean, median

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
BIN = ROOT / "Console/bin/Debug/net8.0"
CONSOLE = "UnityWorld.Console.dll"
DEFAULT_LIMIT = 1000
FIXED_LABELS = {"seed": "战斗种子", "NpcA": "待测方模板", "NpcB": "对手模板",
                "FinsihTick": "战斗结束 Tick", "Finishreson": "战斗结束原因"}
# 口径与每批的冻结清单一起保存，旧批次可独立重建汇总。
METRICS = {
    "damageOutHp": ("HP", "累计对敌扣血"),
    "damageOutShield": ("盾", "打在敌方护盾上的累计伤害"),
    "damageInHp": ("HP", "因伤害累计损失的血量"),
    "maxHitOutHp": ("HP", "单笔对敌扣血最高值"),
    "hpHitsOut": ("次", "造成对敌扣血的伤害笔数"),
    "woundsAdded": ("张", "累计新增伤口张数"),
    "firstCardUseTick": ("Tick", "首次发动卡牌的 Tick"),
    "cardUses": ("次", "累计发动卡牌次数"),
    "roundsCompleted": ("轮", "完成的背包扫描轮数"),
    "manaSpent": ("枚", "发动卡牌累计实付灵元"),
    "manaSpentByElement": ("枚/五行", "发动卡牌实付的各色灵元"),
    "mpSpent": ("MP", "累计消耗灵气"), "mpRecovered": ("MP", "累计回复灵气"),
    "firstWoundTick": ("Tick", "首张新增伤口落盘的 Tick"),
    "firstHpLossTick": ("Tick", "首次因伤害扣血的 Tick"),
    "hp": ("HP", "剩余血量"), "mp": ("MP", "剩余灵气"),
    "spUsed": ("SP", "已占用的 SP"), "shield": ("盾", "剩余护盾"),
}
FIRST = {"firstWoundTick", "firstHpLossTick", "firstCardUseTick"}


def item_labels(plan, dictionary, card_names=None):
    labels = FIXED_LABELS.copy()
    card_names = card_names or {}
    for item in plan:
        side = "待测方" if item["side"] == "subject" else "对手"
        at = item.get("at")
        timing = "" if at is None else ("终局，" if at == "finish" else f"第 {at} Tick，")
        description = dictionary[item["metric"]][1]
        if "cardId" in item:
            name = (card_names.get(item["cardId"]) or "").strip()
            # 显示名可以含标点或换行，保留 Markdown 表格的列结构。
            name = " ".join(name.splitlines()).replace("\\", "\\\\").replace("|", "\\|")
            card = f"卡牌「{name}」" if name else "所选卡牌"
            description = f"{card}的累计发动次数"
        labels[item["id"]] = f"{side}，{timing}{description}"
    return labels


def read_json(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_text(path, content):
    # 同一批次允许离线重建，读者只会看到写完的报告。
    output = tempfile.NamedTemporaryFile(dir=path.parent, delete=False, mode="w", encoding="utf-8")
    temporary = Path(output.name)
    try:
        with output:
            output.write(content)
        temporary.replace(path)
    finally:
        temporary.unlink(missing_ok=True)


def write_json(path, value):
    write_text(path, json.dumps(value, ensure_ascii=False, indent=2) + "\n")


def validate_plan(plan):
    if not isinstance(plan, list):
        raise ValueError("测量清单必须是数组")
    keys = set(FIXED_LABELS)
    first_events = set()
    for item in plan:
        if not isinstance(item, dict):
            raise ValueError(f"无效测项 {item!r}")
        key, side, metric = (item.get(name) for name in ("id", "side", "metric"))
        if not isinstance(key, str) or not re.fullmatch(r"[\w.-]+", key) or key in keys:
            raise ValueError(f"无效或重复的条目 ID：{key}")
        keys.add(key)
        if side not in ("subject", "opponent") or metric not in METRICS:
            raise ValueError(f"{key} 的观察方或指标无效：{side}, {metric}")
        if "cardId" in item:
            card_id = item["cardId"]
            if metric != "cardUses" or not isinstance(card_id, str) or not card_id.strip() or any(
                char in card_id for char in "|\r\n"
            ):
                raise ValueError(f"{key} 仅可在 cardUses 上指定有效的卡牌 ID")
        if metric in FIRST:
            if "at" in item or (side, metric) in first_events:
                raise ValueError(f"{key} 必须是该角色唯一的首次事件且不指定 at")
            first_events.add((side, metric))
        elif item.get("at") != "finish":
            at = item.get("at")
            # 同一套测量表可用于不同上限，未到达的采样点仍保留为空。
            if type(at) is not int or not 0 <= at < 2**31:
                raise ValueError(f"{key} 的 at 须为非负 Int32 或 finish")


def fingerprint(directory, files):
    digest = hashlib.sha256()
    for path in sorted(files):
        digest.update(path.relative_to(directory).as_posix().encode() + b"\0")
        digest.update(hashlib.sha256(path.read_bytes()).digest())
    return digest.hexdigest()


def data_hash(root):
    return fingerprint(root, (p for p in (root / "Data").rglob("*") if p.is_file()))


def resolve_template(templates, key, allow_dummy):
    if allow_dummy and key in ("training_dummy", "木桩"):
        return "training_dummy"
    for npc in templates:
        if npc["ID"] == key:
            return key
    matches = [npc["ID"] for npc in templates if npc.get("DisplayName") == key]
    if len(matches) != 1:
        raise ValueError(f"无法唯一匹配角色 {key}，请指定模板 ID")
    return matches[0]


def distribution(values):
    if not values:
        return None
    values.sort()
    return {"mean": sum(values) / len(values),
            **{f"p{p}": values[math.ceil(p * len(values) / 100) - 1] for p in (10, 50, 90)}}


def summary_row(key, description, values, null_count):
    # 偶数样本取中间两项的均值，原 JSON 的最近秩 P50 仍可独立查询。
    def display(number):
        return f"{number:.2f}".rstrip("0").rstrip(".")

    average = display(mean(values)) if values else "null"
    middle = display(median(values)) if values else "null"
    return f"| {key} | {description} | {average} | {middle} | {null_count} |"


def read_markdown_sample(path):
    if not path.exists():
        return {}
    lines = path.read_text(encoding="utf-8").splitlines()
    if lines[:2] != ["| 条目ID | 值 |", "| --- | --- |"]:
        raise ValueError(f"快照表头无效：{path}")
    rows = {}
    for line in lines[2:]:
        key, value = line.removeprefix("| ").removesuffix(" |").split(" | ", 1)
        # ID 的合法字符不含全角括号，旧报告可能在 ID 后附有说明。
        key = key.partition("（")[0]
        if key in rows:
            raise ValueError(f"重复的快照键 {key}：{path}")
        rows[key] = None if value == "null" else value
    return rows


def summarize(manifest_path, batch_rows=None):
    manifest = read_json(manifest_path)
    labels = manifest.get("labels") or item_labels(manifest["plan"], manifest.get("metricDictionary", METRICS))
    directory = manifest_path.parent
    summary_path = directory / "summary.json"
    if batch_rows is None and manifest.get("formatVersion", 0) >= 3:
        batch_rows = {entry["seed"]: entry["rows"] for entry in read_json(summary_path)["samples"]}

    samples = []
    for seed in manifest["seeds"]:
        error = manifest["errors"].get(str(seed))
        file = None
        if batch_rows is None:
            path = directory / f"seed-{seed}.md"
            rows = read_markdown_sample(path)
            if rows:
                file = path.name
        else:
            rows = batch_rows[seed]
        if not error and (not rows or rows.get("Finishreson") == "Error"):
            error = "缺少有效结算"
        samples.append((seed, file, rows, error))

    failed = sum(bool(sample[3]) for sample in samples)
    successful = len(samples) - failed
    markdown = [
        "# 战斗批次汇总", "",
        f"待测方 `{manifest['subject']}`，对手 `{manifest['opponent']}`。"
        f"共 {len(samples)} 场，成功 {successful} 场，失败 {failed} 场。", "",
    ]
    end_reasons = Counter(rows["Finishreson"] for _, _, rows, error in samples if not error)
    if end_reasons:
        reasons = "，".join(f"`{reason}` {count} 场" for reason, count in sorted(end_reasons.items()))
        markdown.extend([f"按结束原因统计，{reasons}。", ""])
    markdown.extend([
        "| 条目ID | 描述 | 平均数 | 中位数 | null个数 |",
        "| --- | --- | ---: | ---: | ---: |",
    ])
    metrics = {}
    for item in manifest["plan"]:
        key, metric = item["id"], item["metric"]
        description = labels[key]
        reached = 0
        values = []
        for _, _, rows, error in samples:
            if error:
                continue
            at = item.get("at")
            if type(at) is int and int(rows["FinsihTick"]) < at:
                continue
            reached += 1
            if rows[key] is not None:
                values.append(json.loads(rows[key]))
        entry = {"planned": len(samples), "failed": failed, "reached": reached,
                 "endedEarlier": len(samples) - failed - reached}
        # 成功局中，未到采样点和首次事件未发生都记为 null。
        null_count = successful - len(values)
        if metric == "manaSpentByElement":
            elements = sorted({e for paid in values for e in paid})
            markdown.append(summary_row(key, f"{description}，合计", [sum(paid.values()) for paid in values], null_count))
            groups = {}
            for element in elements:
                amounts = [paid.get(element, 0) for paid in values]
                groups[element] = distribution(amounts)
                markdown.append(summary_row(key, f"{description}，{element}", amounts, null_count))
            entry["distribution"] = groups if values else None
        else:
            entry["distribution"] = distribution(values)
            markdown.append(summary_row(key, description, values, null_count))
        if metric in FIRST:
            entry.update(occurred=len(values), denominator=reached,
                         rate=len(values) / reached if reached else None)
            # 定点比较只纳入到达该 Tick 的成功局。首次事件在汇报中只列一行。
            entry["byTick"] = {}
            for tick in sorted({m["at"] for m in manifest["plan"] if type(m.get("at")) is int}):
                eligible = [rows[key] for _, _, rows, error in samples
                            if not error and int(rows["FinsihTick"]) >= tick]
                observed = [int(value) for value in eligible if value is not None and int(value) <= tick]
                entry["byTick"][str(tick)] = {"occurred": len(observed), "denominator": len(eligible),
                                              "rate": len(observed) / len(eligible) if eligible else None,
                                              "distribution": distribution(observed)}
        metrics[key] = entry
    report = {"manifest": manifest_path.name, "metrics": metrics, "samples": []}
    for seed, file, rows, error in samples:
        entry = {"seed": seed, "file": file, "error": error, "end": rows.get("Finishreson")}
        if batch_rows is not None:
            entry["rows"] = rows
        report["samples"].append(entry)
    finish_ticks = [int(rows["FinsihTick"]) for _, _, rows, error in samples if not error]
    markdown.append(summary_row("FinsihTick", labels["FinsihTick"], finish_ticks, 0))
    markdown.extend(["", "仅统计成功场次。平均数和中位数取有读数的场次，保留至多两位小数。"])
    write_json(summary_path, report)
    write_text(directory / "summary.md", "\n".join(markdown) + "\n")
    return failed == 0


def run(args):
    if not args.subject:
        raise ValueError("请指定待测模板")
    seeds = [int(seed.strip()) for seed in args.seeds.split(",")]
    if len(set(seeds)) != len(seeds) or not all(-2**31 <= s < 2**31 for s in seeds + [args.world_seed]):
        raise ValueError("种子必须为 Int32，战斗种子不能重复")
    if not 1 <= args.limit < 2**31:
        raise ValueError("Tick 上限须为正 Int32")
    plan = read_json(args.plan)
    validate_plan(plan)
    fixture = HERE / "training_dummy.json"
    directory = args.output.resolve() / uuid.uuid4().hex
    directory.mkdir(parents=True)

    # 冻结实际读取的数据和程序集，避免同一批次的后半段吃到修改中的配置。
    with tempfile.TemporaryDirectory(prefix="unityworld-benchmark-") as tmp:
        frozen = Path(tmp)
        shutil.copytree(ROOT / "Data", frozen / "Data")
        shutil.copytree(BIN, frozen / "bin", ignore=shutil.ignore_patterns("Data", "Docs", "*.pdb", "Logs"))
        shutil.copy2(fixture, frozen / fixture.name)
        binary = frozen / "bin" / CONSOLE
        assemblies = [CONSOLE, "UnityWorld.dll"]
        def versions(root, bin_dir, dummy_file):
            return {"data": data_hash(root), "assemblies": fingerprint(bin_dir, [bin_dir / name for name in assemblies]),
                    "dummy": hashlib.sha256(dummy_file.read_bytes()).hexdigest()}

        hashes = versions(frozen, binary.parent, frozen / fixture.name)
        if hashes != versions(ROOT, BIN, fixture) or read_json(args.plan) != plan:
            raise ValueError("冻结输入期间数据版本发生变化")
        catalog_command = ["dotnet", str(binary), "--combat-benchmark-catalog", str(frozen)]
        include_card_names = any("cardId" in item for item in plan)
        if include_card_names:
            catalog_command.append("--card-names")
        catalog = subprocess.run(catalog_command, capture_output=True, encoding="utf-8")
        if catalog.returncode:
            raise ValueError(f"读取基准目录失败：{catalog.stderr}")
        definitions = json.loads(catalog.stdout)
        templates = definitions["templates"] if include_card_names else definitions
        card_names = definitions["cardNames"] if include_card_names else {}
        manifest = {"formatVersion": 3, "metricsVersion": 2,
                    "subject": resolve_template(templates, args.subject, False),
                    "opponent": resolve_template(templates, args.opponent, True),
                    "worldSeed": args.world_seed, "seeds": seeds, "limit": args.limit, "plan": plan,
                    "order": ["subject", "opponent"], "hashes": hashes,
                    "metricDictionary": METRICS, "labels": item_labels(plan, METRICS, card_names), "errors": {}}
        manifest["dummyOverrides"] = {"ManaDrawCost": 0} if manifest["opponent"] == "training_dummy" else {}
        manifest_path = directory / "manifest.json"
        write_json(manifest_path, manifest)
        write_json(frozen / "manifest.json", manifest)
        print(f"基准批次 {directory}", flush=True)

        def sample(seed):
            path = frozen / f"seed-{seed}.json"
            command = ["dotnet", str(binary), "--combat-benchmark-sample", str(frozen),
                       str(frozen / "manifest.json"), str(frozen / fixture.name), str(seed), str(path), "--json"]
            completed = subprocess.run(command, capture_output=True, encoding="utf-8", errors="replace")
            error = None
            if completed.returncode:
                error = completed.stderr.strip() or f"退出码 {completed.returncode}"
            try:
                return seed, read_json(path) if path.is_file() else {}, error
            except (OSError, ValueError) as exc:
                return seed, {}, error or f"读取单场读数失败：{exc}"

        batch_rows = {}
        with ThreadPoolExecutor(max_workers=min(4, os.cpu_count() or 1)) as workers:
            for seed, rows, error in workers.map(sample, seeds):
                batch_rows[seed] = rows
                if error:
                    manifest["errors"][str(seed)] = error
        if hashes != versions(ROOT, BIN, fixture) or read_json(args.plan) != plan:
            manifest["inputChanged"] = True
        write_json(manifest_path, manifest)
    success = summarize(manifest_path, batch_rows)
    print(f"汇报 {directory / 'summary.md'}")
    if manifest.get("inputChanged"):
        raise ValueError("批次运行期间工作区输入已变化，报告仅对应冻结版本")
    return success


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--subject")
    parser.add_argument("--opponent", default="training_dummy")
    parser.add_argument("--world-seed", type=int, default=12345)
    parser.add_argument("--seeds", default=",".join(map(str, range(1, 101))))
    parser.add_argument("--limit", type=int, default=DEFAULT_LIMIT)
    parser.add_argument("--plan", type=Path, default=HERE / "dummy-baseline.json")
    parser.add_argument("--output", type=Path, default=ROOT / "Reports/CombatBenchmarks")
    parser.add_argument("--summarize", type=Path, help="从批次清单和已保存的读数重建汇总")
    options = parser.parse_args()
    try:
        success = summarize(options.summarize) if options.summarize else run(options)
    except (OSError, ValueError, KeyError) as exc:
        parser.exit(1, f"{exc}\n")
    sys.exit(0 if success else 1)
