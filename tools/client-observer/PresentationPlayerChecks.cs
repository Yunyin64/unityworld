using UnityWorld.Game.Domain.Combat;

namespace Xianxia.Client.Battle;

/// <summary>值夹具驱动真实调度器和归并器；不初始化世界，也不模拟核心规则。</summary>
public static class PresentationPlayerChecks
{
    public static void Run()
    {
        var assertions = 0;
        var scenarios = 0;
        void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException("PRESENTATION_PLAYER_FAILED " + message);
        }
        void Same(BattleSnapshot expected, BattleSnapshot actual, string message) =>
            Check(BattleJson.SerializeSnapshot(expected) == BattleJson.SerializeSnapshot(actual), message);
        void Test(Action run) { scenarios++; run(); }
        BattlePresentationPlayer Start(BattleSnapshot? initial = null, string session = "test")
        {
            var player = new BattlePresentationPlayer();
            player.Submit(Frame(initial ?? Snapshot(0), 0, session: session, initialization: true));
            return player;
        }
        void Drain(BattlePresentationPlayer player)
        {
            player.Update(1000);
            Check(player.IsIdle && player.ActiveCues.Count == 0, "all committed animations settle");
        }

        Test(() =>
        {
            var player = new BattlePresentationPlayer();
            var initial = Snapshot(0);
            var add = Fact(CombatPresentationKind.CardAddedToBackpack, 1, 10, Card(101)) with
            { Tick = 0, IsInitialization = true, CardAdded = new(Array.Empty<PresentationManaCell>()) };
            player.Submit(Frame(initial, 0, new[] { add }, initialization: true));
            Check(player.IsIdle && player.StartedCueCount == 0, "initial cards do not fly into backpack");
            Same(initial, player.DisplaySnapshot, "initial snapshot direct display");
            var next = Change(Snapshot(1), 10, n => n with { Mp = 19 });
            player.Submit(Frame(next, 1));
            Check(player.IsIdle && player.DisplaySnapshot.Tick == 1, "empty Tick is delivered without animation");
            Same(next, player.DisplaySnapshot, "empty Tick commits all ordinary state");
        });

        Test(() =>
        {
            var player = Start();
            var callbacks = new List<string>();
            player.CueStarted += c => callbacks.Add("S" + c.Event.Sequence);
            player.CueCompleted += c => callbacks.Add("E" + c.Event.Sequence);
            var first = Mana(1, Card(101), 10, 3, Array.Empty<string>(), new[] { "Shui" });
            var second = Mana(2, Card(101), 10, 4, new[] { "Shui" }, new[] { "Shui", "Shui" });
            var used = Fact(CombatPresentationKind.CardUsed, 3, 10, Card(101)) with
            { CardUsed = new(new[] { "Shui", "Shui" }, Array.Empty<string>()) };
            var end = Change(Snapshot(1), 10, n => n with
            {
                Cards = n.Cards.Select(c => c.Id == 101 ? c with { UseCount = 1 } : c).ToArray(),
                Backpack = n.Backpack with { Mana = Array.Empty<ManaSnapshot>() }
            });
            player.Submit(Frame(end, 0, new[] { first, second, used }));
            Check(Visible(player, 101).AssignedElements.Count == 0, "resource is not lit before arrival");
            Check(player.DisplaySnapshot.Participants[0].Backpack.Mana.Any(m => m.X == 4), "later resource stays at source");
            Check(player.ActiveCues.Count == 1, "same card resource arrivals serialize");
            player.Update(.181);
            Check(Visible(player, 101).AssignedElements.Count == 1, "first arrival lights half of size two card");
            player.Update(.18);
            Check(Visible(player, 101).AssignedElements.Count == 2, "full light remains during use flash");
            Check(player.ActiveCues.Single().Event.Kind == CombatPresentationKind.CardUsed, "use waits for both arrivals");
            Drain(player);
            Same(end, player.DisplaySnapshot, "use returns to final zero light state");
            Check(string.Join(",", callbacks) == "S1,E1,S2,E2,S3,E3", "same Tick absorption and use preserve causal order");
        });

        Test(() =>
        {
            var player = Start();
            player.Submit(Frame(Snapshot(1), 0, new[]
            {
                Mana(1, Card(101), 10, 3, Array.Empty<string>(), new[] { "Shui" }),
                Mana(2, Card(201), 20, 3, Array.Empty<string>(), new[] { "Shui" })
            }));
            Check(player.ActiveCues.Count == 2, "unrelated cards animate in parallel");
            player.Update(.09);
            Check(player.ActiveCues.All(c => Math.Abs(c.Progress - .5f) < .001), "parallel cards share display time");
            Drain(player);
        });

        Test(() =>
        {
            var player = Start();
            var replacement = Card(102);
            var end = Change(Snapshot(1), 10, n => n with { Pending = Pending(replacement) });
            var facts = new[]
            {
                PendingFact(1, 10, Card(101), true), PendingFact(2, 20, Card(201), true),
                PendingFact(3, 10, Card(101), false), PendingFact(4, 20, Card(201), false),
                Contest(5), PendingFact(6, 10, replacement, true)
            };
            player.Submit(Frame(end, 0, facts));
            Check(player.DisplaySnapshot.Participants[0].Pending?.CardId == 101 &&
                  player.DisplaySnapshot.Participants[1].Pending?.CardId == 201,
                "same Tick removed slots reappear as actual historical collision pair");
            Check(player.ActiveCues.Single().Event.Kind == CombatPresentationKind.ContestResolved, "one real contest cue");
            player.Update(.1);
            Check(player.DisplaySnapshot.Participants[0].Pending?.CardId == 101, "replacement does not overwrite unfinished collision");
            Drain(player);
            Same(end, player.DisplaySnapshot, "new pending card survives old collision completion");
        });

        Test(() =>
        {
            var player = Start();
            player.Submit(Frame(Snapshot(1), 0, new[] { Contest(1) with
            { Contest = Contest(1).Contest! with { WinnerNpcId = null, Difference = 0 } } }));
            Check(player.ActiveCues.Single().Event.Contest!.WinnerNpcId == null, "draw retains a two card collision");
            Drain(player);
            var straight = Start();
            straight.Submit(Frame(Snapshot(1), 0, new[]
            { PendingFact(1, 10, Card(101), true), PendingFact(2, 10, Card(101), false) }));
            Check(straight.ActiveCues.All(c => c.Event.Kind != CombatPresentationKind.ContestResolved), "direct hit invents no collision");
            Check(straight.DisplaySnapshot.Participants[1].Pending == null, "direct hit invents no opponent slot");
            Drain(straight);
        });

        Test(() =>
        {
            var player = Start();
            var settled = new List<int>();
            player.SnapshotSettled += s => settled.Add(s.Tick);
            var first = Frame(Snapshot(1), 0, new[] { Damage(1) });
            player.Submit(first);
            player.Submit(first);
            player.Submit(Frame(Snapshot(2), 1));
            player.Submit(Frame(Snapshot(3), 1, new[] { Damage(2) with { Tick = 3 } }));
            Check(player.StartedCueCount == 2 && player.ActiveCues.Count == 2,
                "duplicate frame is ignored while later Tick damage starts concurrently");
            Drain(player);
            Check(settled.Order().SequenceEqual(new[] { 1, 2, 3 }), "each Tick settles once; independent empty Tick can finish first");
            Check(player.StartedCueCount == 2 && player.CompletedCueCount == 2, "each damage appears exactly once");
            Same(Snapshot(3), player.DisplaySnapshot, "multiple frame final state");
        });

        Test(() =>
        {
            var gap = Start();
            gap.Submit(Frame(Snapshot(2), 0));
            Check(gap.Diagnostic?.Contains("缺失") == true && gap.IsIdle, "Tick gap explicitly recovers");
            Same(Snapshot(2), gap.DisplaySnapshot, "Tick gap uses complete latest snapshot");
            gap.Submit(Frame(Snapshot(3), 0, new[] { Damage(1) with { Tick = 3 } }));
            Drain(gap);
            Check(gap.CompletedCueCount == 1, "playback resumes after gap recovery");
            var version = Start();
            version.Submit(Frame(Snapshot(1), 0) with { SchemaVersion = 999 });
            Check(version.Diagnostic?.Contains("版本") == true && version.IsIdle, "unknown version explicitly recovers");
            Same(Snapshot(1), version.DisplaySnapshot, "version recovery final snapshot");
            var wrongTick = Start();
            wrongTick.Submit(Frame(Snapshot(2), 0) with { Tick = 1 });
            Check(wrongTick.Diagnostic?.Contains("Tick") == true, "frame and snapshot Tick mismatch is diagnosed");
        });

        Test(() =>
        {
            var player = Start();
            player.Submit(Frame(Snapshot(1), 0, new[] { Damage(1) }));
            Drain(player);
            player.Submit(Frame(Snapshot(2), 1, new[] { Damage(1) with { Tick = 2 } }));
            Check(player.Diagnostic?.Contains("序号") == true, "duplicate event in new Tick is diagnosed");
            Check(player.StartedCueCount == 1 && player.IsIdle, "duplicate event never becomes a cue");
            var range = Start();
            range.Submit(Frame(Snapshot(1), 0, new[] { Damage(1) }) with { FirstSequence = 50 });
            Check(range.Diagnostic?.Contains("范围") == true, "invalid event range is diagnosed");
        });

        Test(() =>
        {
            var player = Start();
            player.MaxQueuedEvents = 1;
            var final = Snapshot(1);
            player.Submit(Frame(final, 0, new[] { Damage(1), Damage(2) }));
            Check(player.Diagnostic?.Contains("已跳过部分动画") == true, "event overflow is visibly diagnosed");
            Check(player.IsIdle && player.ActiveCues.Count == 0 && player.StartedCueCount == 0, "oversized batch does not grow active queue");
            Same(final, player.DisplaySnapshot, "event overflow recovers latest complete state");
        });

        Test(() =>
        {
            var player = Start();
            player.MaxQueuedEvents = 2;
            player.Submit(Frame(Snapshot(1), 0, new[] { Damage(1) }));
            var generation = player.Generation;
            player.Submit(Frame(Snapshot(2), 1, new[] { Damage(2) with { Tick = 2 } }));
            player.Submit(Frame(Snapshot(3), 2, new[] { Damage(3) with { Tick = 3 } }));
            Check(player.Generation > generation && player.IsIdle, "overflow cancels active and pending generations");
            var completed = player.CompletedCueCount;
            player.Update(100);
            Check(player.CompletedCueCount == completed, "canceled overflow cues cannot complete later");
            player.Submit(Frame(Snapshot(4), 3, new[] { Damage(4) with { Tick = 4 } }));
            Drain(player);
            Same(Snapshot(4), player.DisplaySnapshot, "overflow resumes at new consumption cursor");
        });

        Test(() =>
        {
            var player = Start();
            player.MaxQueuedFrames = 2;
            player.Submit(Frame(Snapshot(1), 0, new[] { Damage(1) }));
            for (var tick = 2; tick <= 4; tick++) player.Submit(Frame(Snapshot(tick), 1));
            Check(!player.IsIdle && player.Diagnostic == null && player.ActiveCues.Count == 1,
                "empty Ticks update immediately and occupy no queued frame storage");
            Same(Snapshot(4), player.DisplaySnapshot, "latest empty frame is visible during old damage fade");
            player.Submit(Frame(Snapshot(5), 1, new[] { Damage(2) with { Tick = 5 } }));
            player.Submit(Frame(Snapshot(6), 2, new[] { Damage(3) with { Tick = 6 } }));
            Check(player.IsIdle && player.Diagnostic?.Contains("超限") == true,
                "unfinished nonempty frames remain bounded under cross Tick parallelism");
        });

        Test(() =>
        {
            var player = Start();
            player.AnimationsEnabled = false;
            player.Submit(Frame(Snapshot(1), 0, new[] { Damage(1) }));
            Check(player.IsIdle && player.StartedCueCount == 0, "disabled animation uses snapshot immediately");
            Same(Snapshot(1), player.DisplaySnapshot, "disabled animation final correctness");
            player.AnimationsEnabled = true;
            player.Submit(Frame(Snapshot(2), 1, new[] { Damage(2) with { Tick = 2 } }));
            Drain(player);
            Check(player.CompletedCueCount == 1, "re-enabled animation respects previously consumed cursor");
        });

        Test(() =>
        {
            var player = Start();
            var terminal = Snapshot(1) with
            {
                IsFinished = true,
                Result = new("HpZero", "left", 1, Array.Empty<CombatantResultSnapshot>())
            };
            player.Submit(Frame(terminal, 0, new[] { Damage(1) }));
            Check(!player.IsIdle && !player.DisplaySnapshot.IsFinished, "terminal Tick still displays its last animation");
            player.Update(.1);
            Check(player.DisplaySnapshot.Tick == 1 && !player.IsIdle, "paused simulation permits display-only progress");
            Drain(player);
            Same(terminal, player.DisplaySnapshot, "last animation settles terminal snapshot");
            var stable = BattleJson.SerializeSnapshot(player.DisplaySnapshot);
            player.Update(600);
            Check(stable == BattleJson.SerializeSnapshot(player.DisplaySnapshot), "paused terminal display remains stable");
        });

        Test(() =>
        {
            var player = Start(session: "old");
            var oldInitial = Frame(Snapshot(0), 0, session: "old", initialization: true);
            var oldFrame = Frame(Snapshot(1), 0, new[] { Damage(1) }, "old");
            player.Submit(oldFrame);
            var generation = player.Generation;
            player.Reset();
            Check(player.Generation > generation && player.IsIdle, "reset cancels active generation");
            player.Submit(oldFrame);
            player.Submit(oldInitial);
            Same(BattleSnapshot.Empty, player.DisplaySnapshot, "late old regular and initialization frames do not resurrect after reset");
            var fresh = Change(Snapshot(0), 10, n => n with { Name = "new session" });
            player.Submit(Frame(fresh, 0, session: "new", initialization: true));
            player.Submit(oldInitial);
            player.Submit(oldFrame);
            player.Update(100);
            Same(fresh, player.DisplaySnapshot, "retired session cannot replace new session");
            Check(player.CompletedCueCount == 0, "old reset damage never completes");
        });

        Test(() =>
        {
            var first = Mana(1, Card(101), 10, 3, Array.Empty<string>(), new[] { "Shui" });
            var second = Mana(2, Card(201), 20, 3, Array.Empty<string>(), new[] { "Shui" });
            var player = Start();
            var starts = 0;
            player.CueStarted += _ => { starts++; player.Reset(); };
            player.Submit(Frame(Snapshot(1), 0, new[] { first, second }));
            Check(starts == 1 && player.IsIdle, "reset inside start callback suppresses all remaining old callbacks");
            var completion = Start();
            completion.CueCompleted += _ => completion.Reset();
            completion.Submit(Frame(Snapshot(1), 0, new[] { Damage(1) }));
            completion.Update(100);
            Check(completion.IsIdle, "reset inside completion callback does not dereference cleared frame");
            Same(BattleSnapshot.Empty, completion.DisplaySnapshot, "completion callback reset is not overwritten by old snapshot");
        });

        Test(() =>
        {
            var facts = new[]
            {
                Mana(1, Card(101), 10, 3, Array.Empty<string>(), new[] { "Shui" }),
                Mana(2, Card(201), 20, 3, Array.Empty<string>(), new[] { "Shui" }),
                Fact(CombatPresentationKind.CardUsed, 3, 10, Card(101)) with
                    { CardUsed = new(new[] { "Shui" }, Array.Empty<string>()) },
                Contest(4), Damage(5) with { ContestId = 700 },
                Fact(CombatPresentationKind.CardApplied, 6, 10, Card(101)) with
                    { ContestId = 700, Applied = new("Apply", false) }
            };
            var frame = Frame(Snapshot(1), 0, facts);
            var input = System.Text.Json.JsonSerializer.Serialize(frame, BattleJson.Options);
            (BattlePresentationPlayer Player, List<string> Trace) Create()
            {
                var p = Start(); var trace = new List<string>();
                p.CueStarted += c => trace.Add("S" + c.Event.Sequence);
                p.CueCompleted += c => trace.Add("E" + c.Event.Sequence);
                p.Submit(frame);
                p.Submit(Frame(Snapshot(2), 6, new[] { Damage(7) with { Tick = 2 } }));
                return (p, trace);
            }
            var small = Create(); var large = Create();
            for (var i = 0; i < 10000 && !small.Player.IsIdle; i++) small.Player.Update(.01);
            Drain(large.Player);
            Check(small.Player.IsIdle, "small delta playback converges");
            Check(string.Join(",", small.Trace) == string.Join(",", large.Trace), "large delta preserves every start and completion boundary order");
            Same(small.Player.DisplaySnapshot, large.Player.DisplaySnapshot, "large delta final state matches small delta");
            Check(input == System.Text.Json.JsonSerializer.Serialize(frame, BattleJson.Options), "reducer never mutates submitted facts or snapshots");
        });

        Test(() =>
        {
            var player = Start();
            var wound = Card(301) with { Name = "伤口", Keywords = new[] { "Wound" }, Cells = new[] { new PresentationCell(3, 0) } };
            var added = Fact(CombatPresentationKind.CardAddedToBackpack, 1, 10, wound) with
            { CardAdded = new(new[] { new PresentationManaCell(new(3, 0), "Shui") }) };
            var mana = Mana(2, wound, 10, 4, Array.Empty<string>(), new[] { "Shui" });
            var final = Change(Snapshot(1), 10, n => n with
            { Cards = n.Cards.Append(BattlePresentationState.Copy(wound, 0) with { AssignedElements = new[] { "Shui" } }).ToArray() });
            player.Submit(Frame(final, 0, new[] { added, mana }));
            Check(player.DisplaySnapshot.Participants[0].Cards.Count(c => c.Id == 301) == 1, "new card has one display identity");
            Check(player.ActiveCues.Single().Event.CardAdded != null, "active add cue supplies static-card hiding lifetime");
            Check(player.DisplaySnapshot.Participants[0].Backpack.Mana.All(m => m.X != 3), "replaced mana disappears without fake mana-flight cue");
            player.Update(.401);
            Check(player.ActiveCues.Single().Event.ManaAssigned != null, "subsequent new-card event waits for insertion animation");
            Check(player.ActiveCues.All(c => c.Event.CardAdded == null), "insertion end releases static-card hiding");
            Drain(player);
            Same(final, player.DisplaySnapshot, "new card settles actual snapshot");
            var removed = Snapshot(2);
            player.Submit(Frame(removed, 2));
            Check(player.DisplaySnapshot.Participants[0].Cards.All(c => c.Id != 301), "later removal is retained after insertion callbacks");
            Same(removed, player.DisplaySnapshot, "later card removal final consistency");
        });

        Test(() =>
        {
            var player = Start();
            var card = Card(302);
            player.Submit(Frame(Snapshot(1), 0, new[]
            { Fact(CombatPresentationKind.CardAddedToBackpack, 1, 10, card) with { CardAdded = new(Array.Empty<PresentationManaCell>()) } }));
            Check(player.DisplaySnapshot.Participants[0].Cards.Any(c => c.Id == 302), "same Tick added-then-removed card still shows real insertion");
            Drain(player);
            Check(player.DisplaySnapshot.Participants[0].Cards.All(c => c.Id != 302), "same Tick removal wins after insertion settles");
            var failed = Start();
            failed.Submit(Frame(Snapshot(1), 0));
            Check(failed.StartedCueCount == 0, "failed insertion with no success fact invents no animation");
        });

        Test(() =>
        {
            var player = Start();
            var final = Change(Change(Snapshot(1), 10, n => n with { Mp = 7, Sp = 12, HpMax = 444 }),
                20, n => n with { Hp = 80, Mp = 11, SpMax = 17 });
            var resource = Mana(1, Card(101), 10, 3, Array.Empty<string>(), new[] { "Shui" }) with { ActionId = 90 };
            var damage = Damage(2) with { ParentActionId = 90 };
            player.Submit(Frame(final, 0, new[] { resource, damage }));
            var left = player.DisplaySnapshot.Participants[0]; var right = player.DisplaySnapshot.Participants[1];
            Check(left.Mp == 7 && left.Sp == 12 && left.HpMax == 444 && right.Mp == 11 && right.SpMax == 17,
                "ordinary values untouched by events update immediately");
            Check(right.Hp == 80, "current HP is authoritative even while its historical damage waits to appear");
            Check(player.ActiveCues.Single().Event.Kind == CombatPresentationKind.ManaAssigned, "damage waits for parent action");
            Drain(player);
            Same(final, player.DisplaySnapshot, "mixed ordinary and animated state final correctness");
        });

        Test(() =>
        {
            var player = Start();
            var newCard = Card(303) with { Name = "狼牙", Keywords = new[] { "Passive" } };
            var producer = Fact(CombatPresentationKind.CardApplied, 1, 10, Card(101)) with
                { ActionId = 55, EffectId = 77, Applied = new("Apply", false) };
            var passive = Fact(CombatPresentationKind.CardApplied, 2, 10, newCard) with
                { ParentActionId = 55, EffectId = 88, Applied = new("Apply", true) };
            var addition = Fact(CombatPresentationKind.CardAddedToBackpack, 3, 10, newCard) with
                { ActionId = 55, EffectId = 77, CardAdded = new(Array.Empty<PresentationManaCell>()) };
            var final = Change(Snapshot(1), 10, n => n with
                { Cards = n.Cards.Append(BattlePresentationState.Copy(newCard, 0)).ToArray() });
            var events = new[] { producer, passive, addition };
            var callbacks = new List<string>();
            player.CueStarted += c => callbacks.Add("S" + c.Event.Sequence);
            player.CueCompleted += c => callbacks.Add("E" + c.Event.Sequence);
            player.Submit(Frame(final, 0, events));
            Check(callbacks.SequenceEqual(new[] { "S1", "S3" }), "new insertion overlaps the existing generator name fade");
            Drain(player);
            Check(callbacks.SequenceEqual(new[] { "S1", "S3", "E3", "S2", "E1", "E2" }),
                "new passive card insertion completes before its earlier recorded Start effect is displayed");
            Check(events.Select(e => e.Sequence).SequenceEqual(new long[] { 1, 2, 3 }), "playback plan never reorders authoritative facts");
            Check(player.CompletedCueCount == 3, "shared creator Action and Effect locks do not form a cycle");
            Same(final, player.DisplaySnapshot, "passive-before-add fact sequence settles complete snapshot");
        });

        Test(() =>
        {
            var player = Start();
            var name = Fact(CombatPresentationKind.CardApplied, 1, 10, Card(101)) with
                { ActionId = 55, EffectId = 77, Applied = new("Apply", false) };
            player.Submit(Frame(Snapshot(1), 0, new[] { name, Damage(2) with { Card = Card(101), ActionId = 55, EffectId = 77 } }));
            Check(player.ActiveCues.Count == 2, "name and related damage appear together instead of serial full fades");
            player.Update(.05);
            var next = Change(Snapshot(2), 10, n => n with { Mp = 9, Backpack = n.Backpack with { Row = 2, ScanProgress = 7 } });
            player.Submit(Frame(next, 2, new[]
            {
                Mana(3, Card(101), 10, 3, Array.Empty<string>(), new[] { "Shui" }) with { Tick = 2 },
                Fact(CombatPresentationKind.CardUsed, 4, 10, Card(101)) with
                    { Tick = 2, CardUsed = new(new[] { "Shui" }, Array.Empty<string>()) },
                Mana(5, Card(201), 20, 3, Array.Empty<string>(), new[] { "Shui" }) with { Tick = 2 }
            }));
            Check(player.ActiveCues.Count(c => c.Event.ManaAssigned != null) == 2 &&
                  player.ActiveCues.Count(c => c.Event.Applied != null || c.Event.Damage != null) == 2,
                "next Tick both actors absorb while prior name and damage are still fading");
            Check(player.DisplaySnapshot.Tick == 2 && player.DisplaySnapshot.Participants[0].Backpack.Row == 2 &&
                  player.DisplaySnapshot.Participants[0].Backpack.ScanProgress == 7 && player.DisplaySnapshot.Participants[0].Mp == 9,
                "new Tick and scan progress are visible before old animations finish");
            player.Update(.19);
            Check(player.ActiveCues.Any(c => c.Event.CardUsed != null) && player.ActiveCues.Any(c => c.Event.Applied != null),
                "same-card use begins without waiting for old name fade");
            player.Submit(Frame(Snapshot(3), 5, new[] { Contest(6) with { Tick = 3 } }));
            player.Update(.11);
            Check(player.ActiveCues.Any(c => c.Event.Contest != null) && player.ActiveCues.Any(c => c.Event.Applied != null),
                "next Tick collision overlaps old name and damage tails");
            Drain(player);
            Same(Snapshot(3), player.DisplaySnapshot, "cross Tick overlapping actors settle latest snapshot");
        });

        Test(() =>
        {
            var player = Start();
            var one = Change(Snapshot(1), 10, n => n with
                { Cards = n.Cards.Select(c => c with { AssignedElements = new[] { "Shui" } }).ToArray() });
            player.Submit(Frame(one, 0, new[] { Mana(1, Card(101), 10, 3, Array.Empty<string>(), new[] { "Shui" }) }));
            player.Update(.05);
            var two = Change(Snapshot(2), 10, n => n with { Cards = n.Cards.Select(c => c with { UseCount = 1 }).ToArray() });
            player.Submit(Frame(two, 1, new[]
            {
                Mana(2, Card(101), 10, 4, new[] { "Shui" }, new[] { "Shui", "Shui" }) with { Tick = 2 },
                Fact(CombatPresentationKind.CardUsed, 3, 10, Card(101)) with
                    { Tick = 2, CardUsed = new(new[] { "Shui", "Shui" }, Array.Empty<string>()) }
            }));
            Check(player.DisplaySnapshot.Tick == 2 && player.ActiveCues.Count == 1, "only same-card transition waits across Ticks");
            player.Update(.131);
            Check(Visible(player, 101).AssignedElements.Count == 1 && player.DisplaySnapshot.Tick == 2,
                "old Tick arrival updates only local light and never rolls global Tick back");
            player.Update(.18);
            Check(Visible(player, 101).AssignedElements.Count == 2 && player.ActiveCues.Single().Event.CardUsed != null,
                "cross Tick full light is preserved until use flash completes");
            Drain(player);
            Same(two, player.DisplaySnapshot, "local cross Tick chain finishes at newest authoritative card state");
        });

        Test(() =>
        {
            var player = Start();
            CombatPresentationEvent Name(long sequence, int tick) => Fact(CombatPresentationKind.CardApplied, sequence, 20, Card(201)) with
                { Tick = tick, ActionId = 22, EffectId = sequence, Applied = new("Apply", false) };
            var first = Change(Snapshot(1), 20, n => n with { Hp = 80 });
            player.Submit(Frame(first, 0, new[] { Name(1, 1), Damage(2) with { Card = Card(201), ActionId = 22 } }));
            player.Update(.2);
            var second = Change(Snapshot(2), 20, n => n with { Hp = 60 });
            player.Submit(Frame(second, 2, new[]
            {
                Name(3, 2), Damage(4) with { Tick = 2, Card = Card(201), ActionId = 22,
                    Damage = Damage(4).Damage! with { HpBefore = 80, HpAfter = 60 } }
            }));
            Check(player.ActiveCues.Count(c => c.Event.Applied != null) == 2 && player.ActiveCues.Count(c => c.Event.Damage != null) == 2,
                "same actor can display two names and two damage numbers concurrently");
            Check(player.DisplaySnapshot.Participants[1].Hp == 60, "concurrent damage uses latest HP immediately");
            player.Update(.61);
            Check(player.DisplaySnapshot.Tick == 2 && player.DisplaySnapshot.Participants[1].Hp == 60 && player.ActiveCues.Count == 2,
                "old text completion cannot restore old HP or finish new texts");
            Drain(player);
            Same(second, player.DisplaySnapshot, "concurrent head text final state is latest");
        });

        Test(() =>
        {
            var player = Start();
            player.Submit(Frame(Snapshot(1), 0, new[] { Damage(1), Fact(CombatPresentationKind.CardApplied, 2, 10, Card(101)) with
                { Applied = new("Apply", false) } }));
            player.Update(.1);
            var latest = Change(Change(Snapshot(2), 20, n => n with { Hp = 95, Shield = 12 }), 10, n => n with
            {
                Pending = Pending(Card(102)),
                Cards = n.Cards.Select(c => c with { Name = "最新卡面", AssignedElements = new[] { "Shui", "Shui" } }).ToArray()
            });
            player.Submit(Frame(latest, 2));
            Same(latest, player.DisplaySnapshot, "empty Tick state updates while old head text is alive");
            Drain(player);
            Same(latest, player.DisplaySnapshot, "late old text cannot overwrite newer cards shield healing or slots");
        });

        Test(() =>
        {
            var player = Start();
            player.Submit(Frame(Snapshot(1), 0, new[] { Contest(1) }));
            player.Update(.1);
            var next = Change(Snapshot(2), 10, n => n with { Pending = Pending(Card(102)) });
            player.Submit(Frame(next, 1, new[] { PendingFact(2, 10, Card(102), true) with { Tick = 2 } }));
            Check(player.DisplaySnapshot.Tick == 2 && player.DisplaySnapshot.Participants[0].Pending?.CardId == 101,
                "only occupied slot retains historical collision while new Tick is visible");
            player.Update(.16);
            Check(player.DisplaySnapshot.Participants[0].Pending?.CardId == 102 && player.IsIdle,
                "old collision completion releases slot to newer pending card");
            Same(next, player.DisplaySnapshot, "cross Tick collision cannot clear replacement slot");
        });

        Test(() =>
        {
            var player = Start();
            var newCard = Card(304);
            var added = Change(Snapshot(1), 10, n => n with { Cards = n.Cards.Append(BattlePresentationState.Copy(newCard, 0)).ToArray() });
            player.Submit(Frame(added, 0, new[] { Fact(CombatPresentationKind.CardAddedToBackpack, 1, 10, newCard) with
                { CardAdded = new(Array.Empty<PresentationManaCell>()) } }));
            player.Update(.1);
            player.Submit(Frame(Snapshot(2), 1));
            Check(player.DisplaySnapshot.Tick == 2 && player.ActiveCues.Any(c => c.Event.CardAdded != null),
                "late insertion may finish locally after newer removal without holding the Tick");
            Drain(player);
            Check(player.DisplaySnapshot.Participants[0].Cards.All(c => c.Id != 304), "old insertion completion never resurrects removed card");
            Same(Snapshot(2), player.DisplaySnapshot, "cross Tick card removal final consistency");
        });

        Test(() =>
        {
            var initial = Change(Snapshot(0), 10, n => n with
                { Cards = n.Cards.Select(c => c with { AssignedElements = new[] { "Shui", "Shui" } }).ToArray() });
            var player = Start(initial);
            var first = Change(Snapshot(1), 10, n => n with
            {
                Cards = n.Cards.Select(c => c with { UseCount = 1, AssignedElements = new[] { "Shui" } }).ToArray(),
                Backpack = n.Backpack with { Mana = Array.Empty<ManaSnapshot>() }
            });
            player.Submit(Frame(first, 0, new[]
            {
                Fact(CombatPresentationKind.CardUsed, 1, 10, Card(101)) with
                    { CardUsed = new(new[] { "Shui", "Shui" }, Array.Empty<string>()) },
                Mana(2, Card(101), 10, 3, Array.Empty<string>(), new[] { "Shui" })
            }));
            player.Update(.05);
            var latest = Change(first with { Tick = 2 }, 10, n => n with
                { Backpack = n.Backpack with { Mana = new[] { new ManaSnapshot(3, 0, "Huo") } } });
            player.Submit(Frame(latest, 2));
            Check(player.DisplaySnapshot.Participants[0].Backpack.Mana.Single().Element == "Huo",
                "new Tick resource at reused cell appears before old delayed flight starts");
            player.Update(.06);
            Check(player.ActiveCues.Single().Event.ManaAssigned != null &&
                  player.DisplaySnapshot.Participants[0].Backpack.Mana.Single().Element == "Huo",
                "old flight start cannot remove a new resource that reused the same cell");
            Check(Visible(player, 101).AssignedElements.Count == 0, "old use only resets its local chain before new resource arrival");
            Drain(player);
            Same(latest, player.DisplaySnapshot, "old flight completion preserves newer resource cell and latest card charge");
        });

        Test(() =>
        {
            var player = Start();
            foreach (var invalid in new[] { -1d, double.NaN, double.PositiveInfinity })
            {
                var rejected = false;
                try { player.Update(invalid); }
                catch (ArgumentOutOfRangeException) { rejected = true; }
                Check(rejected, "invalid display delta is rejected");
            }
            Check(player.IsIdle, "invalid delta never changes playback state");
        });

        Console.WriteLine($"PRESENTATION_PLAYER_OK scenarios={scenarios} assertions={assertions} causalOrder=true parallel=true bounded=true resetSafe=true inputImmutable=true");
    }

    private static CombatPresentationCard Card(int id) => new(id, "WaterCard", "水卡", 2, Array.Empty<string>(),
        new[] { new PresentationManaCost("Shui", 2) }, Array.Empty<string>(),
        new[] { new PresentationCell(0, 0), new PresentationCell(1, 0) },
        new[] { new PresentationCell(0, 0), new PresentationCell(1, 0) });
    private static NpcSnapshot Npc(int id, int card) => new(id, "角色" + id, id == 10 ? "left" : "right", "Alive", 100, 30, 2, 0,
        100, 30, 25, new(5, 5, 25, 0, 0, 0, 0, 0, 0,
            Array.Empty<CellSnapshot>(), new[] { new ManaSnapshot(3, 0, "Shui"), new ManaSnapshot(4, 0, "Shui") }),
        new[] { BattlePresentationState.Copy(Card(card), 0) }, Array.Empty<BuffSnapshot>(), null)
        { HpMax = 100, MpMax = 30, SpMax = 25 };
    private static BattleSnapshot Snapshot(int tick) => new(tick, 0, 10, false,
        new[] { Npc(10, 101), Npc(20, 201) }, null, null);
    private static BattleSnapshot Change(BattleSnapshot snapshot, int npcId, Func<NpcSnapshot, NpcSnapshot> change) =>
        snapshot with { Participants = snapshot.Participants.Select(n => n.Id == npcId ? change(n) : n).ToArray() };
    private static CardSnapshot Visible(BattlePresentationPlayer player, int card) =>
        player.DisplaySnapshot.Participants.SelectMany(n => n.Cards).Single(c => c.Id == card);
    private static BattlePresentationFrame Frame(BattleSnapshot snapshot, long previousSequence,
        IReadOnlyList<CombatPresentationEvent>? events = null, string session = "test", bool initialization = false)
    {
        events ??= Array.Empty<CombatPresentationEvent>();
        return new(BattlePresentationFrame.CurrentSchemaVersion, session, snapshot.Tick, initialization, snapshot,
            events, events.Count == 0 ? previousSequence : events[0].Sequence,
            events.Count == 0 ? previousSequence : events[^1].Sequence);
    }
    private static CombatPresentationEvent Fact(CombatPresentationKind kind, long sequence, int npc = 10,
        CombatPresentationCard? card = null) => new(1, sequence, false, kind, npc, card, null, null, null, null);
    private static CombatPresentationEvent Mana(long sequence, CombatPresentationCard card, int npc, int x,
        IReadOnlyList<string> before, IReadOnlyList<string> after) => Fact(CombatPresentationKind.ManaAssigned, sequence, npc, card) with
        { ManaAssigned = new(new(x, 0), "Shui", before, after) };
    private static PendingSnapshot Pending(CombatPresentationCard card) => new(card.Id, card.DefineId, card.Name, "Attack", 10, "Shui");
    private static CombatPresentationEvent PendingFact(long sequence, int npc, CombatPresentationCard card, bool entered) =>
        Fact(CombatPresentationKind.PendingChanged, sequence, npc, card) with { Pending = new(entered, "test", "Attack", "Shui", 10) };
    private static CombatPresentationEvent Contest(long sequence) => Fact(CombatPresentationKind.ContestResolved, sequence) with
    {
        ContestId = 700,
        Contest = new(new(10, Card(101), null, "Attack", "Shui", 12), new(20, Card(201), null, "Attack", "Shui", 10), 10, 2)
    };
    private static CombatPresentationEvent Damage(long sequence) => Fact(CombatPresentationKind.DamageResolved, sequence, 20) with
    { Damage = new(10, Card(101), 20, 100, 80, 20, 0, "Physical", "Shui") };
}
