using Godot;
using System;
using UnityWorld.Game.Domain.Combat;
using Xianxia.Client.Battle;

namespace Xianxia.Client.UI;

public partial class ObserverDuelStage
{
    private void DrawStageLight()
    {
        for (int side = 0; side < _count; side++)
        {
            var frame = _actors[side];
            float light = _focus * frame.Alpha;
            DrawSetTransform(frame.Foot + new Vector2(0, 3), 0, new Vector2(1, .24f));
            float radius = frame.Height * .30f;
            DrawCircle(Vector2.Zero, radius, new Color(Ink[side], light * .10f), true, -1, true);
            DrawArc(Vector2.Zero, radius, 0, Mathf.Tau, 8, new Color(Ink[side], light * .70f), 2.4f, true);
            DrawArc(Vector2.Zero, radius * .78f, 0, Mathf.Tau, 8, new Color(Ink[side], light * .32f), 1.1f, true);
        }
        DrawSetTransform(Vector2.Zero);
    }

    private void DrawCast(int side, float p, Color color)
    {
        var actor = _actors[side];
        int direction = side == 0 ? 1 : -1;
        float alpha = Smooth(p, .03f, .22f) * (1 - Smooth(p, .53f, .94f)) * actor.Alpha;
        if (alpha <= 0) return;
        float spread = Smooth(p, CastRelease, .82f);
        var seal = new Vector2(direction * .24f, -.62f);
        var raisedHand = actor.Foot + seal.Lerp(ProjectionHand(side), Smooth(p, .37f, CastRelease + .05f)) * actor.Height;
        var hand = _shot == null ? raisedHand : actor.Foot - new Vector2(0, actor.Height * .30f);
        var light = color.Lerp(Colors.White, .59f);
        float gathering = Smooth(p, .03f, CastRelease);
        float radius = (Mathf.Lerp(32, 19, gathering) + 47 * spread) * _unit;
        DrawSetTransform(actor.Foot + new Vector2(0, 3), 0, new Vector2(_unit, _unit * .23f));
        DrawArc(Vector2.Zero, 23 + 39 * spread, 0, Mathf.Tau, 6, new Color(color, alpha * .50f), 1.7f, true);
        DrawSetTransform(hand, p * 1.9f, new Vector2(.67f, 1));
        DrawCircle(Vector2.Zero, radius * .81f, new Color(color, alpha * .18f), true, -1, true);
        DrawArc(Vector2.Zero, radius, 0, Mathf.Tau, 6, new Color(light, alpha), 2.8f, true);
        DrawArc(Vector2.Zero, radius + 9, Mathf.Pi / 6, Mathf.Tau + Mathf.Pi / 6, 6, new Color(color, alpha * .52f), 1.4f, true);
        for (int i = 0; i < 6; i++)
        {
            var ray = Vector2.FromAngle(i * Mathf.Tau / 6);
            var mote = ray * (radius + (27 * (1 - gathering) + 28 * spread) * _unit);
            DrawLine(mote, mote + new Vector2(0, -9 - i % 3 * 4), new Color(light, alpha * .75f), 2, true);
        }
        DrawSetTransform(hand + new Vector2(direction * 34 * spread * _unit, 0), -direction * .14f);
        float release = Smooth(p, .37f, CastRelease) * (1 - Smooth(p, .46f, .67f));
        DrawColoredPolygon(new Vector2[] { new(0, -22), new(7, -3), new(29, 0), new(7, 4), new(0, 22), new(-7, 4), new(-29, 0), new(-7, -3) },
            new Color(light, release));
        DrawSetTransform(Vector2.Zero);
    }

    private void DrawExchange()
    {
        var cue = _shot!;
        float p = cue.Progress;
        if (cue.Event.Contest is { } contest)
        {
            var left = _contestLeftIsPlayer ? contest.Left : contest.Right;
            var right = _contestLeftIsPlayer ? contest.Right : contest.Left;
            const float contact = BattlePresentationPlayer.ContestContact;
            float sweep = Smooth(p, contact - .10f, contact) * (1 - Smooth(p, contact + .07f, contact + .21f));
            bool leftEvades = Evades(cue.Exchange!, left);
            bool rightEvades = Evades(cue.Exchange!, right);
            DrawContestTechnique(left, 0, p, sweep, leftEvades);
            DrawContestTechnique(right, 1, p, sweep, rightEvades);
            if (!leftEvades && !rightEvades)
            {
                var winner = DuelExchange.Winner(contest);
                bool stopped = winner?.ContestType is "Shield" or "Block" ||
                    cue.Exchange?.ContestImpact is { HpDamage: 0, ShieldAbsorbed: > 0 };
                var impactColor = stopped ? new Color("b8eced") : new Color("ffdb90");
                DrawImpact(_contactPoint, impactColor, _impact, 0);
            }
            DrawContestVerdict(left, contest.WinnerNpcId, p);
        }
        else if (cue.Event.Damage is { } damage)
        {
            int direction = _source == 0 ? 1 : -1;
            var color = ObserverArtCatalog.ElementColor(damage.Element);
            const float contact = BattlePresentationPlayer.StrikeContact;
            float sweep = Smooth(p, contact - .10f, contact) * (1 - Smooth(p, contact + .09f, contact + .28f));
            if (damage.DamageType == "SheJi") DrawShot(_source, p, contact, color);
            else DrawTechnique(_contactPoint, direction, damage.DamageType, color, sweep);
            DrawImpact(_contactPoint, damage.HpDamage > 0 ? color : new Color("a5e3ff"), _impact, direction);
        }
        if (IsReturning)
        {
            float departure = Smooth(p, ReturnFadeStart, ReturnJump - .01f) *
                (1 - Smooth(p, ReturnJump, .943f));
            float arrival = Smooth(p, ReturnJump - .022f, ReturnJump + .035f) *
                (1 - Smooth(p, ReturnJump + .04f, .991f));
            float transfer = Smooth(p, ReturnJump - .012f, ReturnJump + .038f);
            for (int side = 0; side < _count; side++)
            {
                if (side == _defeatedSide) continue;
                var actor = _actors[side];
                DrawRecallTrail(actor.Departure, actor.Home, Ink[side], transfer);
                DrawReturn(actor.Departure, Ink[side], departure, false);
                DrawReturn(actor.Home, Ink[side], arrival, true);
            }
        }
        if (_defeatedSide >= 0) DrawDefeat(_defeatedSide, p);
    }

    private void DrawContestTechnique(ContestSidePresentation action, int side, float p, float sweep, bool evades)
    {
        var color = ObserverArtCatalog.ElementColor(action.Element);
        if (action.ContestType == "SheJi")
        {
            DrawShot(side, p, BattlePresentationPlayer.ContestContact, color);
            return;
        }
        string kind = action.ContestType == "Dodge" && !evades ? "Block" : action.ContestType;
        var at = evades ? _actors[side].Foot - new Vector2(0, _actors[side].Height * .42f) : _contactPoint;
        DrawTechnique(at, side == 0 ? 1 : -1, kind, color, sweep);
    }

    private void DrawShot(int source, float p, float contact, Color color)
    {
        float flight = Smooth(p, contact - .10f, contact);
        float alpha = Smooth(p, contact - .10f, contact - .07f) * (1 - Smooth(p, contact, contact + .16f));
        if (alpha <= 0) return;
        var caster = _actors[source];
        var origin = caster.Foot + ProjectionHand(source) * caster.Height;
        var at = origin.Lerp(_contactPoint, flight);
        var trail = origin.Lerp(_contactPoint, Math.Max(0, flight - .38f));
        DrawLine(trail, at, new Color(color, alpha * .34f), 12 * _unit, true);
        DrawLine(trail, at, new Color(color.Lerp(Colors.White, .67f), alpha), 2.7f * _unit, true);
        DrawCircle(at, 4.8f * _unit, new Color(color.Lerp(Colors.White, .82f), alpha), true, -1, true);
    }

    // 两张推出姿态的前手不同高，射击与法术均从各自的指尖发出。
    private static Vector2 ProjectionHand(int side) => side == 0 ? new(.38f, -.71f) : new(-.43f, -.65f);

    private void DrawTechnique(Vector2 point, int direction, string kind, Color hue, float strength)
    {
        if (strength <= 0) return;
        DrawSetTransform(point, 0, new Vector2(direction, 1) * _unit);
        var glow = new Color(hue, strength * .44f);
        var edge = new Color(hue.Lerp(Colors.White, .68f), strength);
        // 横扫、刺射、守御使用不同轮廓，以便碰撞瞬间仍能辨认招式。
        switch (kind)
        {
            case "Ci":
                DrawColoredPolygon(new Vector2[] { new(-153, 34), new(-21, -9), new(28, -4), new(-10, 9) }, glow);
                DrawLine(new Vector2(-143, 32), new Vector2(29, -5), edge, 3.2f, true);
                break;
            case "Da":
                DrawColoredPolygon(new Vector2[] { new(-92, -5), new(-38, -16), new(14, -7), new(21, 6), new(-16, 19) }, glow);
                DrawArc(new Vector2(-25, 1), 41, -.95f, 1.02f, 14, glow, 9, true);
                DrawArc(new Vector2(-25, 1), 41, -.71f, .87f, 12, edge, 3.4f, true);
                break;
            case "Shield":
            case "Block":
                DrawArc(new Vector2(-42, 0), 51, -1.14f, 1.14f, 10, glow, 10, true);
                DrawArc(new Vector2(-42, 0), 51, -1.14f, 1.14f, 10, edge, 2, true);
                break;
            case "Dodge":
                DrawLine(new Vector2(5, 36), new Vector2(-103, 12), glow, 5, true);
                DrawLine(new Vector2(-16, 45), new Vector2(-115, 25), edge, 1.4f, true);
                break;
            default:
                DrawArc(new Vector2(-50, -9), 70, -.97f, .95f, 26, glow, 11, true);
                DrawArc(new Vector2(-50, -9), 70, -.97f, .95f, 26, edge, 3, true);
                DrawArc(new Vector2(-56, -8), 81, -.73f, .48f, 20, glow, 2.6f, true);
                break;
        }
        DrawSetTransform(Vector2.Zero);
    }

    private void DrawImpact(Vector2 point, Color color, float strength, int direction)
    {
        if (strength <= 0) return;
        float expansion = 1 - strength;
        DrawSetTransform(point, 0, Vector2.One * _unit);
        DrawCircle(Vector2.Zero, 32 + expansion * 32, new Color(color, strength * .21f), true, -1, true);
        DrawArc(Vector2.Zero, 22 + expansion * 72, 0, Mathf.Tau, 8, new Color(color, strength * .48f), 2.2f, true);
        DrawColoredPolygon(new Vector2[] { new(-20, 0), new(0, -25), new(20, 0), new(0, 25) },
            new Color("fff5df", strength * .93f));
        for (int i = 0; i < 13; i++)
        {
            var ray = Vector2.FromAngle(i * Mathf.Tau / 13 + .22f);
            float reach = 28 + i % 5 * 10 + expansion * 55;
            var at = ray * (12 + expansion * 35) + new Vector2(direction * expansion * 26, 0);
            DrawLine(at, at + ray * reach, new Color(i % 3 == 0 ? Colors.White : color, strength * .78f), 1 + i % 3, true);
        }
        DrawSetTransform(Vector2.Zero);
    }

    private void DrawRecallTrail(Vector2 from, Vector2 to, Color color, float p)
    {
        if (p <= 0 || p >= 1 || from.DistanceSquaredTo(to) < 24 * 24) return;
        // 只有几帧遁光掠过地面，出发和落点分别留有符纹。
        float alpha = MathF.Sin(p * Mathf.Pi);
        var tip = from.Lerp(to, p) + new Vector2(0, -23 * _unit);
        var ray = (to - from).Normalized() * _unit;
        for (int i = 0; i < 4; i++)
        {
            var at = tip - ray * (i * 19) + new Vector2(0, i * 7 - 8) * _unit;
            DrawLine(at - ray * (56 - i * 8), at, new Color(color, alpha * (.48f - .08f * i)), 3.6f * _unit, true);
        }
        DrawLine(tip - ray * 45, tip, new Color(color.Lerp(Colors.White, .70f), alpha * .91f), 2.0f * _unit, true);
    }

    private void DrawReturn(Vector2 foot, Color color, float strength, bool landing)
    {
        if (strength <= 0) return;
        DrawSetTransform(foot, 0, new Vector2(_unit, _unit * .27f));
        float ring = (landing ? 51 : 43) + 16 * (1 - strength);
        DrawCircle(Vector2.Zero, ring * .77f, new Color(color, strength * .25f), true, -1, true);
        DrawArc(Vector2.Zero, ring, 0, Mathf.Tau, 6, new Color(color, strength * .89f), 3, true);
        DrawArc(Vector2.Zero, ring * .72f, .0f, Mathf.Tau, 32, new Color(color, strength * .57f), 1.25f, true);
        DrawSetTransform(foot, 0, Vector2.One * _unit);
        if (landing)
        {
            var gleam = color.Lerp(Colors.White, .69f);
            DrawColoredPolygon(new Vector2[] { new(-27, -2), new(-7, -34), new(0, -96), new(7, -34), new(27, -2) },
                new Color(gleam, strength * .45f));
        }
        for (int i = 0; i < 7; i++)
        {
            var tip = new Vector2((i - 3) * 17, -(i % 4 + 1) * 21 - (1 - strength) * 63);
            DrawLine(tip, tip + new Vector2(0, 7 + i % 3 * 5), new Color(color.Lerp(Colors.White, .48f), strength * .78f), 2, true);
        }
        DrawSetTransform(Vector2.Zero);
    }

    private void DrawSkid(Vector2 foot, int direction)
    {
        if (_skid <= 0) return;
        DrawSetTransform(foot, 0, new Vector2(direction, 1) * _unit);
        for (int i = 0; i < 6; i++)
        {
            var tip = new Vector2(14 - i * 13, -3 - i % 3 * 5);
            DrawLine(tip, tip - new Vector2(18 + i * 4, i % 3 * 6),
                new Color("f2d4a0", _skid * (.80f - i * .078f)), i % 2 + 1, true);
            if (i % 2 == 0) DrawCircle(tip + new Vector2(-15, -7), 1.6f, new Color("ead4ad", _skid * .76f), true, -1, true);
        }
    }

    private void DrawDefeat(int side, float p)
    {
        if (_collapse <= .27f) return;
        var actor = _actors[side];
        var ink = Ink[side].Lerp(new Color("ffe5a7"), .68f);
        float tail = 1 - Smooth(p, .90f, .999f);
        float glow = _collapse * tail;
        DrawSetTransform(actor.Departure + new Vector2(0, 2), 0, new Vector2(_unit, _unit * .25f));
        DrawArc(Vector2.Zero, 34 + _dissolve * 64, 0, Mathf.Tau, 6, new Color(ink, glow * .62f), 2.6f, true);
        DrawArc(Vector2.Zero, 29 + _dissolve * 58, .15f, Mathf.Tau + .15f, 18, new Color(ink, glow * .33f), 1.6f, true);
        // 纸符碎片沿败者的落点散开。路径使用固定相位，逐帧抓取和回放一致。
        for (int i = 0; i < 14; i++)
        {
            float birth = .74f + .007f * (i % 6);
            float travel = Smooth(p, birth, .97f);
            float alpha = Smooth(p, birth, birth + .04f) * tail;
            float spread = (i % 7 - 3) * (8 + 21 * travel) * _unit;
            float rise = (28 + i % 5 * 17 + travel * (45 + i % 3 * 38)) * _unit;
            var at = actor.Departure + new Vector2(spread, -rise);
            DrawSetTransform(at, i * .81f + travel * 1.8f, Vector2.One * _unit);
            DrawRect(new Rect2(-2, -4, 4 + i % 4, 7 + i % 5), new Color(ink, alpha * .76f), true);
        }
        DrawSetTransform(Vector2.Zero);
        var font = GetThemeDefaultFont();
        int size = Math.Max(16, (int)(26 * _unit));
        const string label = "败退";
        float width = font.GetStringSize(label, fontSize: size).X;
        float x = Mathf.Clamp(actor.Departure.X, TextLimits.X + width / 2 + 6, TextLimits.Y - width / 2 - 6);
        float y = actor.Departure.Y - actor.Height - 15;
        var atLabel = new Vector2(x - width / 2, y);
        DrawStringOutline(font, atLabel, label, fontSize: size, size: 6, modulate: new Color("091421", glow));
        DrawString(font, atLabel, label, fontSize: size, modulate: new Color(ink, glow));
    }

    private void DrawContestVerdict(ContestSidePresentation left, int? winner, float p)
    {
        if (!_contact) return;
        float alpha = 1 - Smooth(p, .59f, .77f);
        if (alpha <= 0) return;
        var verdict = winner == null ? "平局" : winner == left.NpcId ? "我方胜招" : "敌方胜招";
        var font = GetThemeDefaultFont();
        int size = Math.Max(14, (int)(18 * _unit));
        var position = ContestReadoutAt + new Vector2(-font.GetStringSize(verdict, fontSize: size).X * .5f, size * .34f);
        DrawStringOutline(font, position, verdict, fontSize: size, size: 4, modulate: new Color("0a121e", alpha));
        DrawString(font, position, verdict, fontSize: size, modulate: new Color("ffe7ad", alpha));
    }
}
