"""批量校验背包迁移 JSON；只读，不改写数据。运行位置不限。"""
import json
from collections import Counter
from pathlib import Path


def field(record, name, default=None):
    return next((value for key, value in record.items() if key.lower() == name.lower()), default)


root = Path(__file__).resolve().parents[1]
card_files = sorted((root / "Data/Card").glob("*.json"))
cards = {}
statuses = Counter()
for path in card_files:
    for card in json.loads(path.read_text(encoding="utf-8-sig")):
        card_id = field(card, "ID")
        assert card_id and card_id not in cards, f"重复或缺失卡 ID: {path.name}: {card_id}"
        size = field(card, "Size", 1)
        assert isinstance(size, int) and size > 0, f"非法 Size: {card_id}"
        cards[card_id] = card
        if card_id.startswith("bp_"):
            continue
        status = field(card, "CompatibilityStatus")
        assert status in {"Migrated", "Unmigrated", "Deprecated", "Pending"}, f"兼容状态缺失: {card_id}"
        statuses[status] += 1
        if status != "Migrated":
            assert field(card, "CompatibilityReason"), f"兼容原因缺失: {card_id}"
            continue
        active = set(field(card, "Keywords", [])) & {"FaShu", "FaBao", "ZhaoShi"}
        if active:
            costs = field(card, "ManaCost", {})
            assert costs and all(value > 0 for value in costs.values()) and sum(costs.values()) == size, f"主动资源与占格不一致: {card_id}"
        assert field(card, "ProducesContest") in (True, False), f"拼点能力未声明: {card_id}"

templates = []
for path in sorted((root / "Data/Npc").glob("*.json")):
    for template in json.loads(path.read_text(encoding="utf-8-sig")):
        template_id = field(template, "ID")
        main = field(template, "MainGongFaId")
        placements = field(template, "InitCardPlacements", [])
        assert not field(template, "InitCardDeck", []), f"模板混用旧卡组: {template_id}"
        assert sum(field(entry, "Key") == main for entry in placements) == 1, f"主功法缺失或歧义: {template_id}"
        occupied = set()
        for entry in placements:
            card_id, position = field(entry, "Key"), field(entry, "Value")
            assert card_id in cards, f"未知模板卡: {template_id}/{card_id}"
            card = cards[card_id]
            assert field(card, "CompatibilityStatus") == "Migrated", f"模板引用未迁移卡: {template_id}/{card_id}"
            assert position, f"缺少位置: {template_id}/{card_id}"
            x, y = field(position, "X"), field(position, "Y")
            if card_id == main:
                assert (x, y) == (3, 3), f"主功法不在中心: {template_id}"
            for column in range(x, x + field(card, "Size", 1)):
                cell = (column, y)
                assert 1 <= column <= 5 and 1 <= y <= 5, f"模板越界: {template_id}/{card_id}/{cell}"
                assert cell not in occupied, f"模板重叠: {template_id}/{card_id}/{cell}"
                occupied.add(cell)
        assert any((x, y) not in occupied and (x + 1, y) not in occupied for y in range(1, 6) for x in range(1, 5)), f"未预留完整两格伤口: {template_id}"
        templates.append({"id": template_id, "cards": len(placements), "cells": len(occupied)})

print(json.dumps({"cardFiles": len(card_files), "formalStatus": dict(statuses), "templates": templates}, ensure_ascii=False))
