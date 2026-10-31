using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UnityWorld.Game.Domain.Combat;
using Xianxia.Client.Battle;
using static Xianxia.Client.UI.ObserverWidgets;

namespace Xianxia.Client.UI;

/// <summary>明确标注的纯值视觉案例，不改正式模板、不执行战斗规则，供连续帧复核。</summary>
internal sealed class ObserverPresentationFixture
{
    private readonly BattleObserverView _view;
    private readonly ObserverBattleLayout _layout;
    private readonly BattlePresentationPlayer _player;
    private readonly string _directory;
    private readonly string _session = "visual-fixture-" + Guid.NewGuid().ToString("N");
    private long _sequence;
    private int _tick;
    private BattleSnapshot _snapshot = BattleSnapshot.Empty;

    public ObserverPresentationFixture(BattleObserverView view, ObserverBattleLayout layout, BattlePresentationPlayer player, string directory)
    {
        _view = view; _layout = layout; _player = player; _directory = directory;
        Directory.CreateDirectory(directory);
        layout.PresentationCaption = "表现验收 · 构造值案例（非正式战斗）";
    }

    public async Task Run()
    {
        try
        {
            _player.Reset();
            _snapshot = Initial();
            _player.Submit(new BattlePresentationFrame(1, _session, 0, true, _snapshot, Array.Empty<CombatPresentationEvent>(), 0, 0));
            await Capture("00-initial", 12);

            var water = Card(101);
            Advance(WithCard(water with { AssignedElements = new[] { "Shui" } }),
                Fact(CombatPresentationKind.ManaAssigned, 1, water, mana: new(new(0, 0), "Shui", Array.Empty<string>(), new[] { "Shui" })));
            await Capture("01-water-half", 30);

            var mixed = Card(102);
            Advance(WithCard(mixed with { AssignedElements = new[] { "Shui" } }),
                Fact(CombatPresentationKind.ManaAssigned, 1, mixed, mana: new(new(2, 0), "Shui", Array.Empty<string>(), new[] { "Shui" })));
            await Capture("02-mixed-frozen", 30);

            water = Card(101);
            var enemy = Card(201);
            var wound = MakeCard(301, "bp_wound_small", "轻伤", new[] { new CellSnapshot(3, 4) }, Array.Empty<ElementAmount>(), wound: true);
            var normal = MakeCard(302, "card_mu_haste", "新生木法", new[] { new CellSnapshot(4, 3) }, new[] { new ElementAmount("Mu", 1) });
            var used = water with { AssignedElements = Array.Empty<string>(), UseCount = 1, ApplyCount = 1 };
            var rightUsed = enemy with { UseCount = 1, ApplyCount = 1 };
            var final = WithCard(used);
            final = WithCard(rightUsed, final);
            final = Change(final, 1, n => n with { Hp = 100, Cards = n.Cards.Append(wound).Append(normal).ToArray() });
            var leftSide = new ContestSidePresentation(1, Copy(water), 1001, "Ci", "Shui", 8);
            var rightSide = new ContestSidePresentation(2, Copy(enemy), 1002, "Zhan", "Jin", 12);
            Advance(final,
                Fact(CombatPresentationKind.ManaAssigned, 1, water, action: 1001, mana: new(new(1, 0), "Shui", new[] { "Shui" }, new[] { "Shui", "Shui" })),
                Fact(CombatPresentationKind.CardUsed, 1, water, action: 1001, used: new(new[] { "Shui", "Shui" }, Array.Empty<string>())),
                Fact(CombatPresentationKind.PendingChanged, 1, water, action: 1001, pending: new(true, "Use", "Ci", "Shui", 8)),
                Fact(CombatPresentationKind.PendingChanged, 2, enemy, action: 1002, pending: new(true, "Use", "Zhan", "Jin", 12)),
                Fact(CombatPresentationKind.ContestResolved, 1, water, contestId: 2001, contest: new(leftSide, rightSide, 2, 4)),
                Fact(CombatPresentationKind.CardApplied, 1, water, action: 1001, contestId: 2001, effectId: 3001, applied: new("Apply", false)),
                Fact(CombatPresentationKind.CardApplied, 2, enemy, action: 1002, contestId: 2001, effectId: 3002, applied: new("Apply", false)),
                Fact(CombatPresentationKind.DamageResolved, 1, enemy, action: 1002, contestId: 2001, damage: new(2, Copy(enemy), 1, 80, 55, 25, 0, "Physical", "Jin")),
                Fact(CombatPresentationKind.CardAddedToBackpack, 1, wound, contestId: 2001, added: new(Array.Empty<PresentationManaCell>())),
                Fact(CombatPresentationKind.CardAddedToBackpack, 1, normal, added: new(Array.Empty<PresentationManaCell>())));
            await Capture("03-full-chain", 420);

            leftSide = leftSide with { Value = 10 };
            rightSide = rightSide with { Value = 10 };
            Advance(_snapshot,
                Fact(CombatPresentationKind.PendingChanged, 1, water, pending: new(true, "Use", "Ci", "Shui", 10)),
                Fact(CombatPresentationKind.PendingChanged, 2, enemy, pending: new(true, "Use", "Zhan", "Jin", 10)),
                Fact(CombatPresentationKind.ContestResolved, 1, water, contestId: 2002, contest: new(leftSide, rightSide, null, 0)),
                Fact(CombatPresentationKind.CardApplied, 1, water, contestId: 2002, effectId: 3003, applied: new("Apply", false)),
                Fact(CombatPresentationKind.CardApplied, 2, enemy, contestId: 2002, effectId: 3004, applied: new("Apply", false)));
            await Capture("04-draw", 180);

            Advance(Change(_snapshot, 1, n => n with { HpMax = 120, MpMax = 40, SpMax = 20, Shield = 0 }),
                Fact(CombatPresentationKind.CardApplied, 1, mixed, effectId: 3005, applied: new("KeywordHook", true)),
                Fact(CombatPresentationKind.DamageResolved, 1, null, damage: new(null, null, 1, 100, 100, 0, 18, "Physical", "None")),
                Fact(CombatPresentationKind.DamageResolved, 1, null, damage: new(null, null, 1, 100, 93, 7, 0, "Physical", "None")));
            await Capture("05-passive-shield-limits", 180);

            var removed = Change(_snapshot, 1, n => n with { Cards = n.Cards.Where(c => c.Id != 302).ToArray(), HpMax = null, StatReadError = "视觉测试：明确不可用上限" });
            Advance(removed);
            await Capture("06-removal-unavailable", 12);
            // 同一人物多条提示同时存在，检验固定文字槽位而非排队等淡出。
            Advance(Change(_snapshot, 1, n => n with { Hp = 68, HpMax = 120, StatReadError = null }),
                Fact(CombatPresentationKind.CardApplied, 1, water, effectId: 3006, applied: new("Apply", false)),
                Fact(CombatPresentationKind.CardApplied, 1, mixed, effectId: 3007, applied: new("KeywordHook", true)),
                Fact(CombatPresentationKind.DamageResolved, 1, null, damage: new(null, null, 1, 100, 80, 20, 0, "Physical", "None")),
                Fact(CombatPresentationKind.DamageResolved, 1, null, damage: new(null, null, 1, 80, 68, 12, 0, "Physical", "None")));
            await Capture("07-parallel-popups", 90);
            File.WriteAllText(Path.Combine(_directory, "fixture-result.json"), JsonSerializer.Serialize(new
            {
                purpose = "Constructed value-only presentation fixture, not real battle evidence",
                finalTick = _snapshot.Tick, idle = _player.IsIdle, diagnostic = _player.Diagnostic,
                viewport = new { width = _view.GetViewportRect().Size.X, height = _view.GetViewportRect().Size.Y }
            }, BattleJson.Options));
            GD.Print("OBSERVER_PRESENTATION_FIXTURE_OK " + _directory);
            _view.StopForVerification();
            _view.GetTree().Quit();
        }
        catch (Exception exception)
        {
            File.WriteAllText(Path.Combine(_directory, "fixture-failure.txt"), exception.ToString());
            _view.ShowFailure(exception);
            _view.GetTree().Quit(1);
        }
    }

    private async Task Capture(string name, int maximumFrames)
    {
        _layout.PresentationCaption = "表现验收 · 构造值案例（非正式战斗） · " + name;
        var images = new List<(int Frame, Godot.Image Image)>();
        var records = new List<string>();
        int settledFrames = 0;
        for (int frame = 0; frame < maximumFrames; frame++)
        {
            _player.Update(1.0 / 60);
            _view.RefreshForFixture();
            _layout.Play.Disabled = _layout.Pause.Disabled = _layout.Step.Disabled = _layout.Restart.Disabled = true;
            await _view.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            // 全部逻辑样本存档，图片隔帧保存；对撞至少有约 7 张连续采样图。
            if (frame % 2 == 0 || _player.IsIdle)
                images.Add((frame, _view.GetViewport().GetTexture().GetImage()));
            records.Add(JsonSerializer.Serialize(new
            {
                frame, display = _player.DisplaySnapshot,
                active = _player.ActiveCues.Select(c => new { c.Event, c.Progress }),
                pending = _layout.Pending.ContentPositions.Select(p => new { x = p.X, y = p.Y })
            }, BattleJson.Options));
            settledFrames = _player.IsIdle ? settledFrames + 1 : 0;
            if (settledFrames >= 10) break;
        }
        foreach (var item in images)
        {
            var error = item.Image.SavePng(Path.Combine(_directory, $"{name}-{item.Frame:D3}.png"));
            item.Image.Dispose();
            if (error != Error.Ok) throw new IOException("视觉案例截图失败：" + error);
        }
        File.WriteAllLines(Path.Combine(_directory, name + ".jsonl"), records);
        if (!_player.IsIdle) throw new InvalidOperationException("视觉案例未在预期显示时间内收尾：" + name);
    }

    private void Advance(BattleSnapshot next, params CombatPresentationEvent[] events)
    {
        _tick++;
        // 事实与终态的灵元集合都使用明确源格，避免测试自身制造源格重现。
        next = next with { Tick = _tick };
        foreach (var fact in events.Where(e => e.ManaAssigned != null))
            next = Change(next, fact.NpcId, n => n with { Backpack = n.Backpack with { Mana = n.Backpack.Mana.Where(m => m.X != fact.ManaAssigned!.SourceCell.X || m.Y != fact.ManaAssigned.SourceCell.Y).ToArray() } });
        _snapshot = next;
        _player.Submit(new BattlePresentationFrame(1, _session, _tick, false, next, events,
            events.Length > 0 ? events[0].Sequence : _sequence + 1, _sequence));
        File.AppendAllText(Path.Combine(_directory, "fixture-frames.jsonl"), JsonSerializer.Serialize(_player.DisplaySnapshot, BattleJson.Options) + "\n");
    }

    private CombatPresentationEvent Fact(CombatPresentationKind kind, int npcId, CardSnapshot? card,
        long? action = null, long? contestId = null, long? effectId = null, ManaAssignedPresentation? mana = null,
        CardUsedPresentation? used = null, PendingPresentation? pending = null, ContestPresentation? contest = null,
        DamagePresentation? damage = null, CardAddedPresentation? added = null, CardAppliedPresentation? applied = null) =>
        new(_tick + 1, ++_sequence, false, kind, npcId, card == null ? null : Copy(card), action, null, contestId, effectId,
            mana, used, pending, contest, damage, added, applied);

    private CardSnapshot Card(int id) => _snapshot.Participants.SelectMany(n => n.Cards).Single(c => c.Id == id);
    private BattleSnapshot WithCard(CardSnapshot card, BattleSnapshot? source = null) => (source ?? _snapshot) with
    {
        Participants = (source ?? _snapshot).Participants.Select(n => n with { Cards = n.Cards.Select(c => c.Id == card.Id ? card : c).ToArray() }).ToArray()
    };
    private static BattleSnapshot Change(BattleSnapshot source, int id, Func<NpcSnapshot, NpcSnapshot> change) =>
        source with { Participants = source.Participants.Select(n => n.Id == id ? change(n) : n).ToArray() };
    private static CombatPresentationCard Copy(CardSnapshot card) => new(card.Id, card.DefineId, card.Name, card.Size,
        card.IsWound ? new[] { "Wound" } : Array.Empty<string>(), card.RequiredElements.Select(e => new PresentationManaCost(e.Element, e.Amount)).ToArray(),
        card.AssignedElements, card.Cells.Select(c => new PresentationCell(c.X, c.Y)).ToArray(), card.Cells.Select(c => new PresentationCell(c.X, c.Y)).ToArray());
    private static CardSnapshot MakeCard(int id, string define, string name, CellSnapshot[] cells, ElementAmount[] need, bool frozen = false, bool wound = false) =>
        new(id, define, name, cells.Length, "Waiting", frozen, 0, 0, false, wound, cells, Array.Empty<string>(), need);

    private static BattleSnapshot Initial()
    {
        var valid = Enumerable.Range(0, 25).Select(i => new CellSnapshot(i % 5, i / 5)).ToArray();
        var mana = new[] { new ManaSnapshot(0, 0, "Shui"), new ManaSnapshot(1, 0, "Shui"), new ManaSnapshot(2, 0, "Shui"), new ManaSnapshot(4, 2, "Huo") };
        var pack = new BackpackSnapshot(5, 5, 25, 104, 0, 1, 0, 0, 0, valid, mana);
        var water = MakeCard(101, "card_shui_freeze", "两格水卡", new[] { new CellSnapshot(0, 1), new CellSnapshot(1, 1) }, new[] { new ElementAmount("Shui", 2) });
        var mixed = MakeCard(102, "card_tu_slow", "混色冻结", new[] { new CellSnapshot(2, 1), new CellSnapshot(3, 1), new CellSnapshot(2, 2) },
            new[] { new ElementAmount("Jin", 1), new ElementAmount("Mu", 1), new ElementAmount("Shui", 1) }, frozen: true);
        var three = MakeCard(103, "card_shui_freeze", "三格水卡", new[] { new CellSnapshot(0, 3), new CellSnapshot(1, 3), new CellSnapshot(2, 3) }, new[] { new ElementAmount("Shui", 3) }) with { AssignedElements = new[] { "Shui" } };
        var main = MakeCard(104, "jin_fa", "无蓄灵", new[] { new CellSnapshot(4, 4) }, Array.Empty<ElementAmount>()) with { IsMain = true };
        var left = new NpcSnapshot(1, "测试甲", "A", "Active", 80, 24, 7, 18, null, null, null, pack, new[] { water, mixed, three, main }, Array.Empty<BuffSnapshot>(), null)
            { HpMax = 100, MpMax = 30, SpMax = 12 };
        var enemy = MakeCard(201, "card_jin_slash", "金刃", new[] { new CellSnapshot(0, 1) }, new[] { new ElementAmount("Jin", 1) });
        // 长剑跨两格：验收图像在 x=2 的内部格界连续，保留相邻空格与外部间距。
        var sword = MakeCard(202, "card_fabao_jian", "连贯长剑", new[] { new CellSnapshot(1, 3), new CellSnapshot(2, 3) }, new[] { new ElementAmount("Jin", 2) });
        var right = new NpcSnapshot(2, "测试乙", "B", "Active", 0, 0, 3, 0, null, null, null, pack with { Mana = Array.Empty<ManaSnapshot>() }, new[] { enemy, sword }, Array.Empty<BuffSnapshot>(), null)
            { HpMax = 0, MpMax = 0, SpMax = 9 };
        return new BattleSnapshot(0, 0, 10, false, new[] { left, right }, null, null);
    }
}
