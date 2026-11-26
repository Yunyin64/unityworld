using Godot;
using UnityWorld.Game.Domain.Combat;
using Xianxia.Client.Battle;

namespace Xianxia.Client.UI;

public partial class ObserverDuelStage
{
    internal Rect2 ReadyCardBounds(int side)
    {
        var actor = _actors[side];
        float height = Mathf.Clamp(GetViewportRect().Size.Y * .082f, 53, 88);
        float width = height * .73f;
        int outward = side == 0 ? -1 : 1;
        var center = actor.Foot + new Vector2(outward * (actor.Height * .78f + width * .55f + 13), -actor.Height * .70f);
        center.X = Mathf.Clamp(center.X, width / 2 + 12, Size.X - width / 2 - 12);
        center.Y = Mathf.Clamp(center.Y, height / 2 + 8, Size.Y - height / 2 - 10);
        return new Rect2(center - new Vector2(width, height) / 2, new Vector2(width, height));
    }

    private void DrawReadyCard(int side)
    {
        if (_layout!.Pending.Card(side) is not { } pending || _layout.Pending.Flight(side) != null) return;
        // 起镜时小牌沿人物外侧放大，已经打出的牌随镜头结束离场。
        float opacity = _shot?.Event.Contest == null ? 1 : _shot.Progress < .32f ? 1 - ContestCardsOpacity : 0;
        if (opacity > .01f) DrawPreparedCard(this, pending, ReadyCardBounds(side), opacity);
    }

    internal void DrawPreparedCard(Control surface, PendingSnapshot pending, Rect2 bounds, float alpha)
    {
        var element = ObserverArtCatalog.ElementColor(pending.Element);
        surface.DrawRect(bounds, new Color("122836", alpha * .97f));
        surface.DrawRect(bounds, new Color(element, alpha * .82f), false, 1.3f);
        float titleHeight = Mathf.Clamp(bounds.Size.Y * .24f, 13, 20);
        var art = new Rect2(bounds.Position + new Vector2(3, 3),
            new Vector2(bounds.Size.X - 6, bounds.Size.Y - titleHeight - 6));
        DrawCardArt(surface, _layout!.Art, pending.DefineId, pending.ContestType, art, element, alpha);
        var textAt = new Vector2(bounds.Position.X + 3, bounds.End.Y - 4);
        int fontSize = Mathf.RoundToInt(Mathf.Clamp(titleHeight - 3, 10, 14));
        surface.DrawString(surface.GetThemeDefaultFont(), textAt, ObserverArtCatalog.CardName(pending.Name, pending.DefineId),
            HorizontalAlignment.Center, bounds.Size.X - 6, fontSize, new Color("f0e9d5", alpha));
    }

    private void RefreshContestCards(ContestPresentation? contest)
    {
        if (contest == null || ContestCardsOpacity <= .01f)
        {
            foreach (var card in _contestCards) card.Hide();
            return;
        }
        var left = _contestLeftIsPlayer ? contest.Left : contest.Right;
        var right = _contestLeftIsPlayer ? contest.Right : contest.Left;
        PlaceContestCard(left, 0, contest.WinnerNpcId);
        PlaceContestCard(right, 1, contest.WinnerNpcId);
    }

    private void PlaceContestCard(ContestSidePresentation action, int side, int? winner)
    {
        var card = _contestCards[side];
        var actor = _actors[side];
        var preferred = card.PreferredSize;
        // 卡面的默认宽高由其场景决定。窄窗口按同一比例收窄，留足正文行高。
        float width = preferred.X * Mathf.Clamp(Size.X * .167f / 322, 172f / 322, 1);
        float height = preferred.Y * Mathf.Clamp(width / preferred.X, 101f / 132, 1);
        float mid = Mathf.Clamp((_actors[0].Foot.X + _actors[1].Foot.X) * .5f, width + 30, Size.X - width - 30);
        float inset = actor.Height * .37f + 19;
        float left = side == 0 ? Mathf.Clamp(actor.Foot.X - inset - width, 20, mid - width - 10)
            : Mathf.Clamp(actor.Foot.X + inset, mid + 10, Size.X - width - 20);
        float top = Mathf.Clamp(actor.Foot.Y - actor.Height - height * .62f, 20, Size.Y - height - 18);
        var size = new Vector2(width, height);
        var readyAt = ReadyCardBounds(side).GetCenter() - size / 2;
        // 牌面从相同的待发位置展开，此后沿该人物的轮廓外侧跟随。
        var bounds = new Rect2(readyAt.Lerp(new Vector2(left, top), Smooth(_shot!.Progress, .08f, .26f)), size);
        card.Submit(_layout!.Art, action, bounds, ContestCardsOpacity, side == 0, _contact && winner == action.NpcId);
    }

    internal static void DrawCardArt(Control surface, ObserverArtCatalog catalog, string defineId, string type,
        Rect2 area, Color element, float alpha)
    {
        var texture = catalog.Card(defineId);
        if (texture == null)
        {
            DrawContestGlyph(surface, type, area.GetCenter(), Mathf.Min(area.Size.X, area.Size.Y) * .30f,
                new Color(element, alpha * .68f));
            return;
        }
        bool transpose = catalog.CardTranspose(defineId);
        var source = texture.GetSize();
        if (transpose) source = new Vector2(source.Y, source.X);
        if (source.X <= 0 || source.Y <= 0) return;
        var size = source * Mathf.Min(area.Size.X / source.X, area.Size.Y / source.Y);
        surface.DrawTextureRect(texture, new Rect2(area.GetCenter() - size / 2, size), false, new Color(Colors.White, alpha), transpose);
    }

    internal static void DrawContestGlyph(Control surface, string type, Vector2 position, float scale, Color color)
    {
        surface.DrawSetTransform(position, 0, Vector2.One * (scale / 10));
        // 七类图标以轮廓区分，着色只取当前拼点的元素。
        switch (type)
        {
            case "Zhan":
                surface.DrawColoredPolygon(new Vector2[] { new(-8, 9), new(-6, 2), new(3, -8), new(9, -9), new(5, -2), new(0, 3) }, color);
                surface.DrawLine(new Vector2(-8, 3), new Vector2(-1, 9), color, 1.5f, true);
                break;
            case "Da":
                surface.DrawLine(new Vector2(-7, 8), new Vector2(2, -3), color, 2.5f, true);
                surface.DrawColoredPolygon(new Vector2[] { new(-5, -5), new(-1, -10), new(9, -2), new(5, 3) }, color);
                break;
            case "Ci":
                surface.DrawLine(new Vector2(-9, 8), new Vector2(5, -5), color, 1.8f, true);
                surface.DrawColoredPolygon(new Vector2[] { new(9, -9), new(6, 1), new(-1, -6) }, color);
                break;
            case "SheJi":
                surface.DrawArc(new Vector2(-7, 0), 10, -1.27f, 1.27f, 16, color, 1.8f, true);
                surface.DrawLine(new Vector2(-4, -10), new Vector2(-10, 0), color, 1.3f, true);
                surface.DrawLine(new Vector2(-10, 0), new Vector2(-4, 10), color, 1.3f, true);
                surface.DrawLine(new Vector2(-6, 0), new Vector2(9, 0), color, 1.5f, true);
                surface.DrawColoredPolygon(new Vector2[] { new(10, 0), new(5, -3), new(5, 3) }, color);
                break;
            case "Shield":
                surface.DrawPolyline(new Vector2[] { new(-8, -7), new(0, -10), new(8, -7), new(7, 3), new(0, 9), new(-7, 3), new(-8, -7) }, color, 1.8f, true);
                surface.DrawLine(new Vector2(0, -6), new Vector2(0, 4), color, 1.3f, true);
                break;
            case "Block":
                surface.DrawLine(new Vector2(-8, 8), new Vector2(8, -8), color, 2.8f, true);
                surface.DrawLine(new Vector2(-8, -8), new Vector2(8, 8), color, 2.8f, true);
                surface.DrawLine(new Vector2(-9, 2), new Vector2(-2, 9), color, 1.4f, true);
                break;
            case "Dodge":
                surface.DrawPolyline(new Vector2[] { new(-8, -7), new(-2, 0), new(-8, 7) }, color, 1.9f, true);
                surface.DrawPolyline(new Vector2[] { new(-1, -7), new(6, 0), new(-1, 7) }, color, 1.9f, true);
                break;
            default:
                surface.DrawCircle(Vector2.Zero, 4, color);
                break;
        }
        surface.DrawSetTransform(Vector2.Zero);
    }
}
