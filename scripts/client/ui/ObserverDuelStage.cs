using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityWorld.Game.Domain.Combat;
using Xianxia.Client.Battle;

namespace Xianxia.Client.UI;

/// <summary>用事实驱动舞台镜头、角色姿态和位置。面板继续使用原来的场景锚点。</summary>
public partial class ObserverDuelStage : Control
{
    internal const float CastRelease = .42f;
    internal const float ReturnJump = .86f;
    private const float ReturnFadeStart = .78f;
    private const float ReturnSettled = .95f;
    private const float HeavyFollowThrough = 26;
    private const float MinimumWithdrawStep = 31;
    internal enum Pose { Ready, Strike, Guard, Recoil, Cast, Advance, Withdraw, Slash, Smash, Shot, Evade, Kneel, Departed, Windup, FollowThrough, Brace, Land }
    private readonly record struct ActorFrame(Vector2 Foot, Vector2 Home, Vector2 Departure, float Height,
        Pose Pose, float Alpha, float Tilt, Color Tint);
    private readonly record struct StageSpace(Vector2 Left, Vector2 Right, float Min, float Width, float Unit)
    {
        internal float Center => Min + Width / 2;
        internal float Floor => (Left.Y + Right.Y) / 2;
        internal float Gap => Mathf.Clamp(Width * .13f, 38, 174);
        private float Edge => Math.Min(101 * Unit + 12, Width * .27f);
        internal Vector2 Home(int side) => side == 0 ? Left : Right;
        internal float ClampFoot(float x) => Mathf.Clamp(x, Min + Edge, Min + Width - Edge);
    }
    private readonly record struct FootworkAtContact(Vector2 Point, float Push, float FollowThrough,
        bool Victim, bool Stagger, Pose Technique)
    {
        internal bool Defending => Technique is Pose.Guard or Pose.Evade;
        internal bool Ranged => Technique == Pose.Shot;
        internal bool Melee => !Victim && Technique is Pose.Strike or Pose.Slash or Pose.Smash;
        internal float Shift(float recoil, float follow) => Push * recoil + FollowThrough * follow;

        internal Vector2 Settled(StageSpace space, int outward) =>
            new(space.ClampFoot(Point.X + outward * Shift(1, 1)), Point.Y);
    }

    private static readonly Color[] Ink = { new("8ce3cc"), new("f6ab79") };
    private readonly ActorFrame[] _actors = new ActorFrame[2];
    private readonly (PresentationCue? Cue, string? Element)[] _casts = new (PresentationCue?, string?)[2];
    private IReadOnlyList<NpcSnapshot> _participants = Array.Empty<NpcSnapshot>();
    private IReadOnlyList<PresentationCue> _cues = Array.Empty<PresentationCue>();
    private BattleResultSnapshot? _settledResult;
    private int? _departedNpcId;
    private ObserverBattleLayout? _layout;
    private ObserverArena? _arena;
    private ColorRect? _shade;
    private readonly Control[] _sidePanels = new Control[2];
    private readonly Color[] _sidePanelColors = new Color[2];
    private ObserverContestCard[] _contestCards = Array.Empty<ObserverContestCard>();
    private Color _baseShade;
    private PresentationCue? _shot;
    private int _count, _source = -1, _target = -1, _defeatedSide = -1;
    private bool _contestLeftIsPlayer;
    private bool _contact;
    private float _focus, _impact, _unit;
    private float _skid;
    private float _collapse, _dissolve;
    private Vector2 _contactPoint;
    internal Vector2 ContestReadoutAt { get; private set; }
    internal Vector2 TextLimits { get; private set; }
    internal float BackpackOpacity { get; private set; } = 1;
    internal float ContestCardsOpacity { get; private set; }
    internal bool IsFighting => _shot != null;
    internal bool IsReturning => _shot is { ContinuesOnStage: false, Progress: >= ReturnFadeStart };
    internal bool HoldsDamage(CombatPresentationEvent fact) => _shot is { IsHoldingImpact: true } &&
        (fact == _shot.Event || fact.ContestId is { } id && id == _shot.Event.ContestId);

    internal void Configure(ObserverBattleLayout layout)
    {
        _layout = layout;
        _arena = layout.Canvas.GetNode<ObserverArena>("Arena");
        _shade = layout.Canvas.GetNode<ColorRect>("Shade");
        _baseShade = _shade.Color;
        _contestCards = [GetNode<ObserverContestCard>("LeftContestCard"), GetNode<ObserverContestCard>("RightContestCard")];
        for (int side = 0; side < 2; side++)
        {
            _sidePanels[side] = layout.Packs[side].GetParent<Control>();
            _sidePanelColors[side] = _sidePanels[side].Modulate;
        }
        foreach (var actor in layout.Actors) actor.Image.Hide();
        ConfigureAudio();
    }

    internal void Submit(IReadOnlyList<NpcSnapshot> participants, BattlePresentationPlayer player)
    {
        _participants = participants;
        _cues = player.ActiveCues;
        _settledResult = player.DisplaySnapshot.IsFinished ? player.DisplaySnapshot.Result : null;
        _departedNpcId = player.DepartedNpcId;
        _arena!.PresentationFrozen = player.IsHoldingApproach || player.IsHoldingImpact;
        RefreshGeometry();
        SyncAudio(player.Generation);
    }

    internal void RefreshGeometry()
    {
        if (_layout == null) return;
        var participants = _participants;
        var cues = _cues;
        _count = Math.Min(2, participants.Count);
        _shot = _count == 2 ? cues.FirstOrDefault(c => BattlePresentationPlayer.OwnsDuelStage(c.Event)) : null;
        var exchange = _shot?.Exchange;
        var fact = exchange?.Fact;
        var contest = fact?.Contest;
        var damage = fact?.Damage;
        _contestLeftIsPlayer = contest != null && contest.Left.NpcId == participants[0].Id;
        _source = damage?.SourceNpcId == participants.FirstOrDefault()?.Id ? 0 : 1;
        _target = 1 - _source;
        if (damage != null && (participants[_source].Id != damage.SourceNpcId || participants[_target].Id != damage.TargetNpcId))
        {
            _shot = null;
            exchange = null;
        }
        float p = _shot?.Progress ?? 0;
        float contactAt = _shot?.ContactProgress ?? BattlePresentationPlayer.StrikeContact;
        _defeatedSide = exchange?.FinishingNpcId is int lost ? (participants[0].Id == lost ? 0 : 1) : -1;
        _collapse = _defeatedSide < 0 ? 0 : Smooth(p, Mathf.Lerp(contactAt, 1, .28f), .81f);
        _dissolve = _defeatedSide < 0 ? 0 : Smooth(p, .79f, .96f);
        float shotDuration = (float)(_shot?.MotionDuration ?? 1);
        const float hitLead = .008f;
        float sinceContact = (p - contactAt) * shotDuration;
        float releaseRecoil = _shot?.ImpactProgress ?? contactAt;
        float reactionElapsed = (p - releaseRecoil) * shotDuration;
        var previous = _shot?.PreviousExchange;
        bool stays = _shot?.ContinuesOnStage == true;
        float approachEnd = _shot?.ApproachProgress ?? contactAt - .15f;
        float arrival = Smooth(p, previous == null ? .08f : .035f, approachEnd);
        float retreat = stays ? 0 : Smooth(p, .72f, ReturnJump);
        float homeward = stays ? 0 : Smooth(p, .81f, .985f);
        float zoomIn = previous != null ? 1 : Smooth(p, .02f, .26f);
        _focus = _shot == null ? 0 : zoomIn * (1 - homeward);
        BackpackOpacity = 1 - Smooth(_focus, .10f, .94f);
        ContestCardsOpacity = contest == null ? 0 : Smooth(p, .08f, .23f) * (1 - Smooth(p, .74f, .95f));
        _contact = p >= contactAt;
        _impact = _contact ? 1 - Smooth(sinceContact, .055f, .32f) : 0;

        var left = Slot(0);
        var right = Slot(1);
        var packLeft = _sidePanels[0];
        var packRight = _sidePanels[1];
        float minX = InStage(packLeft, new Vector2(packLeft.Size.X + 8, 0)).X;
        float maxX = InStage(packRight, new Vector2(-8, 0)).X;
        float room = Math.Max(100, maxX - minX);
        TextLimits = new Vector2(minX, minX + room);
        _unit = Mathf.Clamp(room / 640, .44f, 1.2f);
        float screenHeight = GetViewportRect().Size.Y;
        var hit = damage ?? exchange?.ContestImpact;
        bool heavy = HeavyHit(hit);
        float pullback = hit is { HpDamage: > 0 }
            ? (heavy ? .18f : .10f) * Smooth(reactionElapsed, .018f, .17f) * (1 - Smooth(reactionElapsed, .20f, .49f))
            : 0;
        float viewFocus = _focus * (1 - pullback);
        // 同一块石台的远近景。镜头向下取景时，接触点和地面一起抬进画面中段。
        const float framingDepth = .134f;
        var framing = new Vector2(0, -screenHeight * framingDepth * viewFocus);
        var space = new StageSpace(Feet(left), Feet(right), minX, room, _unit);
        float center = ExchangeCenter(exchange, space);
        float priorCenter = ExchangeCenter(previous, space);
        float settledCenter = 0;
        float priorSettledCenter = 0;
        float shake = _impact * _impact * MathF.Sin((p - contactAt) * 155) * 10 * _unit;
        float recoilTime = heavy ? .24f : .17f;
        float recoilT = Mathf.Clamp(reactionElapsed / recoilTime, 0, 1);
        float rearFootDown = heavy ? .145f : .107f;
        float braceEnd = heavy ? .37f : .30f;
        _skid = Smooth(reactionElapsed, rearFootDown - .045f, rearFootDown + .02f) *
            (1 - Smooth(reactionElapsed, rearFootDown + .035f, braceEnd));
        // 命中先拉开间距，后脚撑地那一拍才开始卸力和追步。
        float coast = 1 - MathF.Pow(1 - recoilT, heavy ? 4.6f : 3.6f);
        float follow = Smooth(p, releaseRecoil + .075f / shotDuration, Math.Max(releaseRecoil + .09f / shotDuration,
            Math.Min(.69f, releaseRecoil + .40f / shotDuration)));

        for (int side = 0; side < 2; side++)
        {
            _casts[side] = default;
            if (side < _count)
            {
                int npcId = participants[side].Id;
                foreach (var cue in cues)
                    if (cue.Event.NpcId == npcId && _layout.Art.CastElement(cue.Event) is { } cast)
                        _casts[side] = (cue, cast);
            }
            var home = space.Home(side);
            int outward = side == 0 ? -1 : 1;
            bool defeated = side == _defeatedSide;
            var move = Placement(exchange, space, center, side);
            var prior = Placement(previous, space, priorCenter, side);
            var start = previous != null ? prior.Settled(space, outward) : home;
            settledCenter += move.Settled(space, outward).X * .5f;
            priorSettledCenter += start.X * .5f;
            float anticipation = previous != null ? 0 :
                Smooth(p, .035f, .11f) * (1 - Smooth(p, .11f, .25f)) * (move.Victim ? 5 : 27) * _unit;
            float stride = move.Point.X - start.X;
            bool stepping = !move.Victim && !move.Ranged && move.Technique != Pose.Evade && p >= .13f && p < approachEnd &&
                Mathf.Abs(stride) >= 37 * _unit;
            float stepLift = stepping ? MathF.Abs(MathF.Sin(p * shotDuration * 25)) * 6 * _unit : 0;
            float duck = move.Technique == Pose.Evade
                ? Smooth(p, contactAt - .10f, contactAt) * (1 - Smooth(p, contactAt + .12f, .69f)) : 0;
            float hop = move.Stagger ? (heavy ? 17 : 8) * _unit * MathF.Sin(Mathf.Pi * recoilT) : 0;
            float shift = move.Technique == Pose.Evade ? move.Shift(Math.Max(duck * .57f, coast), follow) : move.Shift(coast, follow);
            var foot = start.Lerp(move.Point, arrival) + new Vector2(outward * (anticipation + shake + shift),
                -MathF.Sin(arrival * Mathf.Pi) * (previous != null || move.Victim || move.Defending ? 4 : 20) * _unit - stepLift - hop + duck * 7 * _unit);
            if (exchange != null)
                foot.X = Mathf.Lerp(foot.X, space.ClampFoot(foot.X), Smooth(p, .025f, .18f));
            // 先面朝对手退一步。受击者可能已越过原站位，回原位仍交给闪回。
            if (!defeated && retreat > 0)
            {
                float behind = (home.X - foot.X) * outward;
                float step = Mathf.Clamp(behind * .58f, MinimumWithdrawStep * _unit, space.Width * .16f);
                foot.X = Mathf.Lerp(foot.X, space.ClampFoot(foot.X + outward * step), retreat);
                foot.Y -= MathF.Sin(retreat * Mathf.Pi) * 8 * _unit;
            }
            var departure = foot;
            float baseHeight = Math.Min(Mathf.Clamp(screenHeight * .086f, 46, 130), room * .30f);
            float availableHeight = Math.Max(baseHeight, space.Floor + framing.Y - 30);
            // 远景约占屏高 9%，交手时推到约 24%，窄窗口按舞台余量收住。
            float closeupHeight = Math.Max(baseHeight, Math.Min(Math.Min(screenHeight * .236f, availableHeight), space.Gap * 3.15f));
            float height = Mathf.Lerp(baseHeight, closeupHeight, viewFocus);
            if (move.Victim) _contactPoint = move.Point + framing + new Vector2(-outward * 28, -height * .54f);
            var pose = Pose.Ready;
            if (exchange != null && (previous != null || p > .055f) && (stays || p < ReturnJump))
            {
                if (p > .72f && !stays) pose = Pose.Withdraw;
                else if (move.Stagger && sinceContact >= hitLead && reactionElapsed < braceEnd)
                    pose = reactionElapsed < rearFootDown ? Pose.Recoil : Pose.Brace;
                else if ((previous != null && arrival < .23f) || (move.Stagger && _contact) || p > .72f) pose = Pose.Guard;
                else if (move.Melee && p >= contactAt - .185f && p < contactAt - .095f) pose = Pose.Windup;
                else if (move.Melee && reactionElapsed >= .128f)
                    pose = move.FollowThrough < 0 && reactionElapsed < .25f ? Pose.Advance : Pose.FollowThrough;
                else if (stepping) pose = stride * outward < 0 ? Pose.Advance : Pose.Withdraw;
                else if (duck > .12f) pose = Pose.Evade;
                else if (move.Defending || move.Victim && (!_contact || damage!.ShieldAbsorbed > 0)) pose = Pose.Guard;
                else if (!move.Victim) pose = p < contactAt - .10f ? Pose.Guard : move.Technique;
            }
            float castLean = 0;
            // 结印、送出、收手共用施法时钟。同时交锋时，动作优先让给交手，法光留在人物附近。
            if (_shot == null && _casts[side].Cue is { Progress: > .06f and < .84f } castCue)
            {
                float cast = castCue.Progress;
                float gather = Smooth(cast, .06f, .32f) * (1 - Smooth(cast, .34f, CastRelease + .10f));
                float release = Smooth(cast, CastRelease, CastRelease + .083f) * (1 - Smooth(cast, .58f, .84f));
                pose = cast < CastRelease ? Pose.Cast : cast < .63f ? Pose.Shot : Pose.Land;
                foot += new Vector2(outward * (4 * gather - 14 * release), 4 * gather) * _unit;
                castLean = -outward * .063f * release;
            }
            if (p >= ReturnJump && !stays && !defeated)
            {
                float land = Smooth(p, ReturnJump, ReturnSettled);
                foot = home - new Vector2(0, (1 - land) * 13 * _unit);
                if (p < ReturnSettled) pose = Pose.Land;
                height *= 1 - .075f * (1 - land);
            }
            if (pose == Pose.Evade) height *= .89f;
            if (pose == Pose.Brace) height *= 1 - .09f * _skid;
            float alpha = stays ? 1 : p < ReturnJump ? 1 - Smooth(p, ReturnFadeStart, ReturnJump) : Smooth(p, ReturnJump, .924f);
            var tint = _contact && move.Stagger ? new Color(1 + _impact * .34f, 1 - _impact * .28f, 1 - _impact * .34f)
                : Colors.White;
            float windup = move.Defending || move.Victim || move.Ranged ? 0 :
                Smooth(p, contactAt - .16f, contactAt - .035f) * (1 - Smooth(p, contactAt + .035f, contactAt + .11f));
            float tilt = outward * (shift * .00145f + anticipation * .0018f - windup * .08f) *
                (1 - retreat) * (1 - Smooth(p, .68f, .96f)) + castLean;
            if (defeated)
            {
                float kneel = _collapse;
                pose = _dissolve >= 1 ? Pose.Departed : kneel > .22f ? Pose.Kneel : pose;
                height *= 1 - .33f * kneel;
                tilt *= 1 - kneel;
                alpha = 1 - _dissolve;
            }
            else if (side < _count && !IsFighting && (_departedNpcId == participants[side].Id ||
                _settledResult?.Combatants.Any(a => a.NpcId == participants[side].Id && a.Status == "Defeated") == true))
            {
                pose = Pose.Departed;
                alpha = 0;
                _casts[side] = default;
            }
            _actors[side] = new ActorFrame(foot + framing, home + framing, departure + framing, height, pose, alpha, tilt, tint);
        }
        if (contest != null)
        {
            float contactY = (_actors[0].Departure.Y + _actors[1].Departure.Y) * .5f;
            float averageHeight = (_actors[0].Height + _actors[1].Height) * .5f;
            _contactPoint = new Vector2(center, contactY - averageHeight * .60f);
            // 胜负位于接触点上方，双方点数跟随各自卡牌。
            float gutter = Math.Min(room * .5f, 124 * _unit);
            float x = Mathf.Clamp(center, minX + gutter, minX + room - gutter);
            float y = _contactPoint.Y - 65 * _unit;
            ContestReadoutAt = new Vector2(x, y);
        }
        RefreshContestCards(contest);
        // 起镜继承上一招收势，接触后焦点才跟随双方拉开的间距。
        float previousFocus = previous == null ? space.Center : Mathf.Lerp(priorCenter, priorSettledCenter, .66f);
        float shotFocus = Mathf.Lerp(previousFocus, center, arrival);
        shotFocus = Mathf.Lerp(shotFocus, settledCenter, .66f * Smooth(p, contactAt, .70f));
        SetCamera(new Vector2(Mathf.Lerp(shotFocus, space.Center, homeward), space.Floor + screenHeight * framingDepth / .78f), viewFocus, shake);
        QueueRedraw();
    }

    internal Vector2 HeadAt(int side) => _actors[side].Foot - new Vector2(0, _actors[side].Height * .89f);
    internal Vector2 FootAt(int side) => _actors[side].Foot;
    internal string PoseAt(int side) => _actors[side].Pose.ToString();
    internal float AlphaAt(int side) => _actors[side].Alpha;
    internal float HeightAt(int side) => _actors[side].Height;

    private float ExchangeCenter(DuelExchange? exchange, StageSpace space)
    {
        if (exchange == null) return space.Center;
        float currentPressure = Pressure(exchange.Fact);
        float pressure = currentPressure;
        float weight = .71f;
        // 连续交锋只保留近三次的推进方向，避免反击立刻跳到舞台另一侧。
        foreach (var prior in exchange.ApproachHistory)
        {
            pressure += Pressure(prior) * weight;
            weight *= .68f;
        }
        float center = space.Center + Mathf.Clamp(pressure, -.20f, .20f) * space.Width;
        // 靠边接触时给受击或闪身一侧留出脚步，避免窄窗口把整段位移夹掉。
        float clearance = space.Width * .12f;
        float direction = MathF.Sign(currentPressure);
        var hit = exchange.Fact.Damage ?? exchange.ContestImpact;
        if (HeavyHit(hit))
        {
            direction = hit!.TargetNpcId == _participants[0].Id ? -1 : 1;
            clearance = Math.Max(clearance, HeavyPush(space) + (HeavyFollowThrough + MinimumWithdrawStep) * space.Unit);
        }
        if (direction == 0) return center;
        float offset = direction * ((exchange.Fact.Contest == null ? 0 : space.Gap) + clearance);
        float limit = space.ClampFoot(center + offset) - offset;
        return offset > 0 ? Math.Min(center, limit) : Math.Max(center, limit);
    }

    private float Pressure(CombatPresentationEvent fact)
    {
        if (fact.Damage is { } hit)
            return (hit.TargetNpcId == _participants[0].Id ? -1f : 1f) * (hit.DamageType == "SheJi" ? .16f : .19f);
        if (DuelExchange.Winner(fact.Contest) is not { } winner) return 0;
        float towardLoser = winner.NpcId == _participants[0].Id ? 1f : -1f;
        return winner.ContestType switch
        {
            "Dodge" => towardLoser * -.10f,
            "Shield" or "Block" => towardLoser * .06f,
            "Da" => towardLoser * .15f,
            _ => towardLoser * .10f
        };
    }

    private FootworkAtContact Placement(DuelExchange? exchange, StageSpace space, float center, int side)
    {
        var home = space.Home(side);
        if (exchange == null) return new(home, 0, 0, false, false, Pose.Ready);
        int id = _participants[side].Id;
        int outward = side == 0 ? -1 : 1;
        var damage = exchange.Fact.Damage;
        var contest = exchange.Fact.Contest;
        bool victim = damage?.TargetNpcId == id;
        var hit = damage ?? exchange.ContestImpact;
        bool onTarget = hit?.TargetNpcId == id;
        bool stagger = onTarget && hit!.HpDamage > 0;
        bool shielded = onTarget && hit!.HpDamage == 0 && hit.ShieldAbsorbed > 0;
        var action = contest == null ? null : contest.Left.NpcId == id ? contest.Left : contest.Right;
        bool wins = contest?.WinnerNpcId == id;
        bool evades = Evades(exchange, action);
        string? kind = damage?.DamageType ?? action?.ContestType;
        var technique = victim ? Pose.Guard : kind switch
        {
            "Zhan" => Pose.Slash,
            "Da" => Pose.Smash,
            "SheJi" => Pose.Shot,
            "Dodge" when evades => Pose.Evade,
            "Shield" or "Block" or "Dodge" => Pose.Guard,
            _ => Pose.Strike
        };
        bool advancing = technique is Pose.Slash or Pose.Smash or Pose.Strike;
        bool yields = victim || contest?.WinnerNpcId != null && !wins;
        bool presses = advancing && !yields && hit is { HpDamage: > 0, SourceNpcId: int source } && source == id;
        bool heavy = HeavyHit(hit);
        float push = stagger && heavy ? HeavyPush(space) :
            (stagger ? 116 : shielded ? 14 : evades ? 66 : yields ? 36 : presses ? -18 : 12) * space.Unit;
        float follow = (stagger ? heavy ? HeavyFollowThrough : 12 : presses ? heavy ? -49 : -31 : evades ? 16 : 0) * space.Unit;
        var point = new Vector2(center + outward * space.Gap, space.Floor);
        if (damage != null)
        {
            var targetHome = space.Home(damage.TargetNpcId == _participants[0].Id ? 0 : 1);
            // 紧凑画面里两个出招轮廓仍要有兵器距离。
            float reach = Mathf.Lerp(1.5f, 1.1f, Smooth(space.Unit, .75f, 1.15f));
            point.X = victim ? center : technique == Pose.Shot ? Mathf.Lerp(home.X, point.X, .19f)
                : center + outward * space.Gap * reach;
            point.Y = Mathf.Lerp(targetHome.Y, space.Floor, .35f);
        }
        else if (technique == Pose.Shot)
            point.X = Mathf.Lerp(home.X, point.X, .28f);
        if (presses)
        {
            // 追步幅度以双方在接触点的间距为上限，窄窗口里也保留一段兵器距离。
            float limit = space.Gap * .74f;
            follow = Math.Max(follow, -limit - push);
        }
        point.X = space.ClampFoot(point.X);
        return new(point, push, follow, victim, stagger, technique);
    }

    internal static bool HeavyHit(DamagePresentation? hit) =>
        hit is { HpDamage: > 0 } && (hit.DamageType == "Da" || hit.HpAfter <= 0 || hit.HpDamage >= Math.Max(5, hit.HpBefore * .45f));

    private static float HeavyPush(StageSpace space) => Math.Max(168 * space.Unit, space.Width * .205f);

    // 闪避失败仍播放真实命中；退步姿势取决于结算结果及已确认的扣血。
    private static bool Evades(DuelExchange exchange, ContestSidePresentation? side) =>
        side is { ContestType: "Dodge" } && exchange.ContestInjuryTarget != side.NpcId &&
        (exchange.Fact.Contest?.WinnerNpcId is not int winner || winner == side.NpcId);

    private void SetCamera(Vector2 midpoint, float focus, float shake)
    {
        var arena = _arena!;
        var canvas = _layout!.Canvas;
        // 只推拉背景，HUD 根节点的坐标和点击区保持在原位。
        var pivot = canvas.GetGlobalTransform().AffineInverse() * (GetGlobalTransform() * (midpoint + new Vector2(shake, 0)));
        arena.PivotOffset = pivot.Clamp(Vector2.Zero, arena.Size);
        arena.Scale = Vector2.One * (1 + .78f * focus);
        _shade!.Color = new Color(_baseShade, _baseShade.A + .34f * _focus);
        // 推镜时储物阵和入包演出退出构图，阵盘本身仍持续接收扫描与结算。
        for (int side = 0; side < 2; side++)
        {
            _sidePanels[side].Modulate = new Color(_sidePanelColors[side], _sidePanelColors[side].A * BackpackOpacity);
            _sidePanels[side].Visible = BackpackOpacity > .01f;
        }
    }

    private Rect2 Slot(int side)
    {
        var image = _layout!.Actors[side].Image;
        return (GetGlobalTransform().AffineInverse() * image.GetGlobalTransform()) * new Rect2(Vector2.Zero, image.Size);
    }

    private Vector2 InStage(CanvasItem item, Vector2 local) =>
        GetGlobalTransform().AffineInverse() * (item.GetGlobalTransform() * local);

    private static Vector2 Feet(Rect2 slot) => new(slot.GetCenter().X, slot.End.Y - 3);
    private static float Smooth(float p, float from, float to)
    {
        float t = Mathf.Clamp((p - from) / (to - from), 0, 1);
        return t * t * (3 - 2 * t);
    }

    /// <summary>绘制舞台、角色、刀光和落点。文本提示在上方效果层绘制。</summary>
    public override void _Draw()
    {
        if (_layout == null) return;
        if (_shot != null) DrawStageLight();
        for (int side = 0; side < _count; side++)
        {
            DrawReadyCard(side);
            DrawActor(side);
            if (_casts[side] is { Cue: { } cast, Element: { } element })
                DrawCast(side, cast.Progress, ObserverArtCatalog.ElementColor(element));
        }
        if (_shot != null) DrawExchange();
    }

    private void DrawActor(int side)
    {
        var actor = _actors[side];
        var texture = _layout!.Actors[side].TextureFor(actor.Pose);
        if (texture == null || texture.GetHeight() == 0 || actor.Alpha <= 0) return;
        DrawSetTransform(actor.Foot + new Vector2(0, 2), 0, new Vector2(1, .20f));
        for (int i = 0; i < 3; i++)
            DrawCircle(Vector2.Zero, actor.Height * (.28f - i * .055f), new Color("020812", actor.Alpha * .22f), true, -1, true);
        if (actor.Pose is Pose.Recoil or Pose.Brace) DrawSkid(actor.Foot, side == 0 ? -1 : 1);
        float width = actor.Height * texture.GetWidth() / texture.GetHeight();
        float origin = actor.Pose == Pose.Strike ? side == 0 ? .35f : .65f : .5f;
        var rect = new Rect2(-width * origin, -actor.Height, width, actor.Height);
        if (_shot != null && (actor.Pose is Pose.Strike or Pose.Slash or Pose.Smash) && _shot.Progress < .51f)
        {
            float tail = Smooth(_shot.Progress, .17f, .25f) * (1 - Smooth(_shot.Progress, .37f, .51f));
            DrawSetTransform(actor.Foot + new Vector2(side == 0 ? -26 : 26, 3) * _unit);
            DrawTextureRect(texture, rect, false, new Color(Ink[side], tail * .23f));
        }
        DrawSetTransform(actor.Foot, actor.Tilt);
        DrawTextureRect(texture, rect, false, new Color(actor.Tint, actor.Alpha));
        DrawSetTransform(Vector2.Zero);
    }
}
