"""核对离线重建的覆盖数、首次事件和零值。直接运行即可。"""
import tempfile
import unittest
from pathlib import Path

from benchmark import DEFAULT_LIMIT, HERE, METRICS, item_labels, read_json, summarize, validate_plan, write_json


class BenchmarkTests(unittest.TestCase):
    def test_plan(self):
        baseline = read_json(HERE / "dummy-baseline.json")
        validate_plan(baseline)
        checkpoints = [50, 100, 200, 300, 400, 500, 750, 1000, "finish"]
        measures = [("Hp", "hp"), ("Mp", "mp"), ("rounds", "roundsCompleted")]
        expected = [(f"{prefix}_{'Last' if at == 'finish' else at}", "subject", metric, at)
                    for prefix, metric in measures for at in checkpoints]
        expected_ids = {row[0] for row in expected}
        checkpoints_in_plan = [(item["id"], item["side"], item["metric"], item["at"])
                               for item in baseline if item["id"] in expected_ids]
        self.assertEqual(checkpoints_in_plan, expected)
        ids = [item["id"] for item in baseline]
        damage_ids = [f"damageOut_{tick}" for tick in range(50, 501, 50)] + ["damageOut_finish"]
        first_damage = ids.index(damage_ids[0])
        self.assertEqual(ids[first_damage:first_damage + len(damage_ids)], damage_ids)
        self.assertEqual(set(ids) - expected_ids, {
            *damage_ids, "damageOutShield_finish", "damageIn_finish", "maxHitOut_finish", "hpHitsOut_finish",
            "firstHpLossTick", "firstWoundTick", "opponentFirstHpLossTick", "opponentFirstWoundTick",
            "opponentWoundsAdded_finish", "firstCardUseTick", "cardUses_finish", "manaSpent_finish",
            "mpSpent_100", "mpSpent_finish", "mpRecovered_finish", "mp_finish", "rounds_finish",
            "opponentHp_finish", "opponentSpUsed_finish",
        })
        self.assertEqual(DEFAULT_LIMIT, 1000)
        sample = {"id": "damageOut_50", "side": "subject", "metric": "damageOutHp", "at": 50}
        first = {"id": "firstWoundTick", "side": "subject", "metric": "firstWoundTick"}
        card = {"id": "use", "side": "subject", "metric": "cardUses", "at": "finish", "cardId": "card_form_da"}
        validate_plan([sample, first, card])
        self.assertEqual(item_labels([card], METRICS, {"card_form_da": "打 | 重击\n试装"})["use"],
                         "待测方，终局，卡牌「打 \\| 重击 试装」的累计发动次数")
        self.assertEqual(item_labels([card], METRICS)["use"], "待测方，终局，所选卡牌的累计发动次数")
        for bad in ([sample, sample], [dict(sample, at=-1)], [dict(sample, at=2**31)],
                    [dict(sample, at=0.5)], [dict(sample, at=True)], [dict(sample, at="50")],
                    [dict(sample, id="seed")], [dict(sample, side="other")],
                    [dict(sample, metric="unknown")], [dict(first, at=50)],
                    [first, dict(first, id="another")], [dict(sample, cardId="card_form_da")],
                    [dict(card, cardId=None)], [dict(card, cardId="")], [dict(card, cardId="bad|ID")]):
            with self.subTest(bad=bad), self.assertRaises(ValueError):
                validate_plan(bad)

    def test_offline_distribution(self):
        plan = [{"id": "damage", "side": "subject", "metric": "damageOutHp", "at": 50},
                {"id": "first", "side": "subject", "metric": "firstWoundTick"},
                {"id": "late", "side": "subject", "metric": "mpSpent", "at": 100},
                {"id": "paid", "side": "subject", "metric": "manaSpentByElement", "at": 50},
                {"id": "never", "side": "subject", "metric": "firstHpLossTick"}]
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            manifest = directory / "manifest.json"
            write_json(manifest, {"subject": "a", "opponent": "b", "plan": plan, "limit": 75, "seeds": list(range(100)),
                                  "errors": {str(i): "Error" for i in range(97, 100)}})
            for seed in range(97):
                finish = 75 if seed < 80 else 25
                rows = {"seed": seed, "NpcA": "a", "NpcB": "b",
                        "damage": seed if seed < 80 else "null", "first": 0 if seed < 12 else "null",
                        "late": "null", "never": "null", "FinsihTick": finish, "Finishreson": "AllDefeated"}
                rows["paid"] = "null"
                if seed < 80:
                    rows["paid"] = '{"fire": 4}' if seed < 20 else '{}'
                if seed == 0:
                    rows["damage（待测方，第 50 Tick，累计对敌扣血）"] = rows.pop("damage")
                (directory / f"seed-{seed}.md").write_text(
                    "| 条目ID | 值 |\n| --- | --- |\n" + "".join(f"| {k} | {v} |\n" for k, v in rows.items()),
                    encoding="utf-8")
            self.assertFalse(summarize(manifest))
            metrics = read_json(directory / "summary.json")["metrics"]
            self.assertEqual(metrics["damage"], {"planned": 100, "failed": 3, "endedEarlier": 17, "reached": 80,
                                                  "distribution": {"mean": 39.5, "p10": 7, "p50": 39, "p90": 71}})
            self.assertEqual(metrics["first"]["byTick"]["50"]["denominator"], 80)
            self.assertEqual(metrics["first"]["byTick"]["50"]["rate"], 12 / 80)
            self.assertEqual(metrics["first"]["denominator"], 97)
            self.assertIsNone(metrics["first"]["byTick"]["100"]["rate"])
            self.assertIsNone(metrics["late"]["distribution"])
            self.assertIsNone(metrics["never"]["distribution"])
            report = (directory / "summary.md").read_text(encoding="utf-8").splitlines()
            self.assertIn("| 条目ID | 描述 | 平均数 | 中位数 | null个数 |", report)
            self.assertIn("| damage | 待测方，第 50 Tick，累计对敌扣血 | 39.5 | 39.5 | 17 |", report)
            self.assertIn("| first | 待测方，首张新增伤口落盘的 Tick | 0 | 0 | 85 |", report)
            self.assertIn("| late | 待测方，第 100 Tick，累计消耗灵气 | null | null | 97 |", report)
            self.assertIn("| never | 待测方，首次因伤害扣血的 Tick | null | null | 97 |", report)
            self.assertIn("| paid | 待测方，第 50 Tick，发动卡牌实付的各色灵元，合计 | 1 | 0 | 17 |", report)
            self.assertIn("| paid | 待测方，第 50 Tick，发动卡牌实付的各色灵元，fire | 1 | 0 | 17 |", report)
            self.assertIn("| FinsihTick | 战斗结束 Tick | 66.24 | 75 | 0 |", report)

    def test_rebuild_batch_without_individual_markdown(self):
        plan = [{"id": "damage", "side": "subject", "metric": "damageOutHp", "at": 50}]
        rows = {
            1: {"damage": "0", "FinsihTick": "50", "Finishreson": "TurnLimitReached"},
            2: {"damage": None, "FinsihTick": "25", "Finishreson": "AllDefeated"},
            3: {"damage": None, "FinsihTick": "10", "Finishreson": "Error"},
        }
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            manifest = directory / "manifest.json"
            write_json(manifest, {"formatVersion": 3, "subject": "a", "opponent": "b", "plan": plan,
                                  "seeds": list(rows), "errors": {"3": "failed"}})
            self.assertFalse(summarize(manifest, rows))
            markdown_path = directory / "summary.md"
            summary_path = directory / "summary.json"
            self.assertEqual(list(directory.glob("*.md")), [markdown_path])
            markdown = markdown_path.read_bytes()
            summary = summary_path.read_bytes()
            self.assertIn("| damage | 待测方，第 50 Tick，累计对敌扣血 | 0 | 0 | 1 |",
                          markdown.decode("utf-8").splitlines())
            self.assertIn("`AllDefeated` 1 场，`TurnLimitReached` 1 场", markdown.decode("utf-8"))
            self.assertEqual(read_json(summary_path)["samples"][2]["rows"], rows[3])
            self.assertFalse(summarize(manifest))
            self.assertEqual(markdown_path.read_bytes(), markdown)
            self.assertEqual(summary_path.read_bytes(), summary)


if __name__ == "__main__":
    unittest.main()
