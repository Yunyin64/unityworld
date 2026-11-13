using Godot;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xianxia.Client.Battle;

namespace Xianxia.Client.UI;

/// <summary>用表现夹具既有的冻结快照和真实 GUI 命中检查卡牌、Buff 详情；检查结束恢复显示绑定。</summary>
internal static class ObserverDetailFixture
{
    public static async Task Check(BattleObserverView view, ObserverBattleLayout layout, BattlePresentationPlayer player, string directory)
    {
        var snapshot = player.DisplaySnapshot;
        string before = BattleJson.SerializeSnapshot(snapshot);
        var actor = layout.Actors[0];
        var pack = layout.Packs[0];
        var npc = snapshot.Participants[0];
        var first = new BuffSnapshot("first-instance", "timed", "计时状态", 2, 10, 3.5f, "TimeBased");
        var second = new BuffSnapshot("second-instance", "stacked", "叠层状态", 4, -1, 0, "StackBased");
        var cards = npc.Cards.Select(c => c.Id == 103 ? c with { Name = "两格水卡", IsFrozen = true } : c).ToArray();
        var fixture = npc with { Buffs = new[] { first, second }, Cards = cards };
        actor.Submit(fixture);
        pack.Submit(fixture, snapshot.TicksPerRow, false);
        var viewport = view.GetViewport();
        var list = layout.Field.GetNode<HBoxContainer>("Duel/LeftActor/BuffScroll/Buffs");
        if (viewport.GuiGetHoveredControl() == null) viewport.NotifyMouseEntered();
        await view.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        void Hover(Vector2 point) => viewport.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
        void HoverCell(int x, int y) => Hover(pack.GetGlobalTransformWithCanvas() * pack.CellCenter(x, y));
        Control? Tip() => view.GetWindow().GetNodeOrNull<DetailTips>("ClientDetailTips")?.GetChildren()
            .OfType<Control>().SingleOrDefault(control => control.IsVisibleInTree());
        void Expect(string title, params string[] fields)
        {
            var tip = Tip() ?? throw new InvalidOperationException($"详情未命中：{title}");
            string actual = tip.GetNode<Label>("%Description").Text;
            if (tip.GetNode<Label>("%Title").Text != title || fields.Any(field => !actual.Contains(field, StringComparison.Ordinal)))
                throw new InvalidOperationException($"详情绑定不符：{title} / {actual}");
        }

        layout.FieldScroll.EnsureControlVisible(pack);
        await view.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        // 第二个格仍命中同一实例；换到同名的另一张后冻结标记与实例号都要更新。
        HoverCell(0, 1); Expect("两格水卡", "实例 101");
        var page = pack.GetParent<Control>();
        var originalPosition = page.Position;
        var oldPoint = pack.GetGlobalTransformWithCanvas() * pack.CellCenter(0, 1);
        page.Position += new Vector2(40, 0);
        Hover(oldPoint);
        if (Tip() != null) throw new InvalidOperationException("背包移位 40 像素后仍残留旧热区。");
        HoverCell(0, 1); Expect("两格水卡", "实例 101");
        page.Position = originalPosition;
        HoverCell(1, 1); Expect("两格水卡", "实例 101");
        HoverCell(0, 3); Expect("两格水卡", "实例 103", "冻结");
        await Screenshot(view, directory, "detail-card");
        HoverCell(4, 0);
        if (Tip() != null || !pack.TooltipText.Contains("格 (5, 1)")) throw new InvalidOperationException("空格残留卡牌详情或丢失坐标说明。");

        layout.FieldScroll.EnsureControlVisible(list);
        await view.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Hover(list.GetChild<Label>(0).GetGlobalRect().GetCenter());
        Expect(first.Name, first.Id, "层数 2", "剩余 3.5 Tick");
        Hover(list.GetChild<Label>(1).GetGlobalRect().GetCenter());
        Expect(second.Name, second.Id, "层数 4");
        await Screenshot(view, directory, "detail-buff");
        actor.Submit(fixture with { Buffs = new[] { first } });
        if (Tip() != null || list.GetChildCount() != 1) throw new InvalidOperationException("被移除的 Buff 仍可命中。");
        // 狼妖等真实修正可能没有实例 ID；相同定义和重复 ID 也要保留各自可悬停的行。
        var unnamed = first with { Id = null!, Name = "无实例状态" };
        var duplicate = second with { Id = "duplicate", DefineId = "same" };
        actor.Submit(fixture with { Buffs = new[] { unnamed, duplicate, duplicate with { Stacks = 9 } } });
        await view.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        Hover(list.GetChild<Label>(0).GetGlobalRect().GetCenter()); Expect(unnamed.Name, "实例标识未提供");
        Hover(list.GetChild<Label>(1).GetGlobalRect().GetCenter()); Expect(duplicate.Name, "层数 4");
        Hover(list.GetChild<Label>(2).GetGlobalRect().GetCenter()); Expect(duplicate.Name, "层数 9");
        actor.Submit(fixture with { Buffs = new[] { unnamed, duplicate } });
        if (Tip() != null || list.GetChildCount() != 2) throw new InvalidOperationException("重复标识的旧命中仍存在。");
        actor.Submit(npc);
        pack.Submit(npc, snapshot.TicksPerRow, false);
        if (list.GetChildCount() != 0 || before != BattleJson.SerializeSnapshot(player.DisplaySnapshot))
            throw new InvalidOperationException("空状态或暂停时的快照发生意外变化。");
        layout.FieldScroll.ScrollVertical = 0;
        GD.Print($"OBSERVER_DETAIL_TARGETS_OK tick={snapshot.Tick} cards=2 buffs=2");
    }

    private static async Task Screenshot(BattleObserverView view, string directory, string name)
    {
        await view.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using var image = view.GetViewport().GetTexture().GetImage();
        if (image.SavePng(Path.Combine(directory, name + ".png")) != Error.Ok)
            throw new IOException("无法保存详情截图：" + name);
    }
}
