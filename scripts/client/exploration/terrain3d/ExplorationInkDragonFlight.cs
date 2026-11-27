using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Godot;

namespace Xianxia.Client.Exploration;

/// <summary>移植《潜鳞》的巡飞脊线、翼节拍和羽梢弹簧，作用于原模型骨骼。</summary>
internal sealed class ExplorationInkDragonFlight
{
    private const float ModelScale = 4.85f;
    private const float Stretch = 1.28f;
    private const float TrailStep = .038f;
    private static readonly string[] WingJoints = { "socket", "01", "02", "03", "04", "05" };
    private static readonly string[] LegJoints = { "01_1", "01_2", "02_1", "02_2", "paw" };
    private static readonly Regex FeatherName = new(@"^DEF_feather_([0-9]+)_(base|tip)[._]?([LR])$");
    private static readonly Regex StrandName = new(@"^DEF_(whisker|beard)_([0-9]+)[._]?([LR])$");
    private readonly Skeleton3D _skeleton;
    private readonly AnimationPlayer _animation;
    private readonly int[] _spine = new int[34];
    private readonly Transform3D[] _rest = new Transform3D[34];
    private readonly Vector3[] _trail = new Vector3[76];
    private readonly float[] _trailLengths = new float[76];
    private readonly Wing[] _wings = new Wing[2];
    private readonly List<Leg> _legs = new();
    private readonly List<Plumage> _plumage = new();
    private float _wingPhase = .20f, _wingPower = .55f, _time, _turn;

    internal ExplorationInkDragonFlight(Skeleton3D skeleton, AnimationPlayer animation)
    {
        _skeleton = skeleton;
        _animation = animation;
        for (int index = 0; index < _spine.Length; index++)
        {
            int bone = skeleton.FindBone($"DEF_body_{index:00}");
            _spine[index] = bone;
            _rest[index] = skeleton.GetBoneGlobalRest(bone);
        }
        for (int index = 0; index < _wings.Length; index++)
        {
            string side = index == 0 ? "L" : "R";
            float sign = index == 0 ? -1f : 1f;
            var bones = FindJoints("DEF_wing_", WingJoints, side);
            var rotations = new Quaternion[bones.Length];
            for (int i = 0; i < bones.Length; i++)
                rotations[i] = skeleton.GetBoneRest(bones[i]).Basis.GetRotationQuaternion();
            _wings[index] = new Wing(bones, rotations, sign);
            _legs.Add(new Leg(FindJoints("DEF_fore_", LegJoints, side), sign, false));
            _legs.Add(new Leg(FindJoints("DEF_hind_", LegJoints, side), sign, true));
        }
        for (int bone = 0; bone < skeleton.GetBoneCount(); bone++)
        {
            string name = skeleton.GetBoneName(bone).ToString();
            var feather = FeatherName.Match(name);
            var strand = StrandName.Match(name);
            if (!feather.Success && !strand.Success) continue;
            var kind = feather.Success ? "feather" : strand.Groups[1].Value;
            int index = int.Parse(feather.Success ? feather.Groups[1].Value : strand.Groups[2].Value);
            bool root = feather.Success && feather.Groups[2].Value == "base";
            float side = (feather.Success ? feather.Groups[3].Value : strand.Groups[3].Value) == "L" ? -1f : 1f;
            var children = skeleton.GetBoneChildren(bone);
            int lengthBone = root && children.Length > 0 ? children[0] : bone;
            var rest = skeleton.GetBoneRest(bone);
            _plumage.Add(new Plumage(bone, kind, index, side, root,
                skeleton.GetBoneRest(lengthBone).Origin.Length(), rest.Basis.GetRotationQuaternion()));
        }
    }

    internal Transform3D Update(float angle, float delta)
    {
        _time += delta;
        var point = OrbitPoint(angle);
        var incoming = (point - OrbitPoint(angle - .052f)).Normalized();
        var outgoing = (OrbitPoint(angle + .052f) - point).Normalized();
        var forward = (incoming + outgoing).Normalized();
        float steering = Mathf.Clamp(incoming.SignedAngleTo(outgoing, Vector3.Up) / .09f, -.84f, .84f);
        _turn = Mathf.Lerp(_turn, steering, 1f - Mathf.Exp(-5f * delta));
        float climb = forward.Y;
        const float drive = .70f;
        float effort = Mathf.Clamp(.44f + .23f * drive + .41f * Mathf.Max(climb, 0f)
            + .14f * Mathf.Abs(_turn) - .24f * Mathf.Max(-climb, 0f), 0f, 1f);
        _wingPower = Mathf.Lerp(_wingPower, effort, 1f - Mathf.Exp(-3.6f * delta));
        _wingPhase += delta * (.36f + .20f * _wingPower);

        // 原作以实时翼相位采样原始片段，再对骨骼叠加飞行姿态。
        foreach (var strand in _plumage)
            _skeleton.SetBonePoseRotation(strand.Bone, strand.Rest);
        double frames = _animation.CurrentAnimationLength * Mathf.PosMod(_wingPhase * .5f, 1f);
        _animation.Seek(frames, update: true);
        var attitude = Basis.LookingAt(forward, Vector3.Up, useModelFront: true).Rotated(forward, -.23f * _turn);
        var scaled = attitude.Scaled(Vector3.One * ModelScale);
        var model = new Transform3D(scaled, point - scaled * _rest[0].Origin);
        SampleTrail(angle, point);
        PoseSpine(model.AffineInverse(), drive);
        PoseWings(climb);
        PoseLegs(climb, drive);
        FollowPlumage(delta, climb, drive);
        return model;
    }

    private int[] FindJoints(string prefix, string[] names, string side)
    {
        var bones = new int[names.Length];
        for (int i = 0; i < names.Length; i++)
            bones[i] = _skeleton.FindBone($"{prefix}{names[i]}.{side}");
        return bones;
    }

    private void SampleTrail(float angle, Vector3 head)
    {
        _trail[0] = head;
        _trailLengths[0] = 0;
        for (int i = 1; i < _trail.Length; i++)
        {
            _trail[i] = OrbitPoint(angle - i * TrailStep);
            _trailLengths[i] = _trailLengths[i - 1] + _trail[i].DistanceTo(_trail[i - 1]);
        }
    }

    private void PoseSpine(Transform3D inverse, float drive)
    {
        int sample = 1;
        var headRest = _rest[0].Origin;
        float length = (_rest[0].Origin.Z - _rest[^1].Origin.Z) * ModelScale * Stretch;
        var parent = Transform3D.Identity;
        for (int index = 0; index < _spine.Length; index++)
        {
            var rest = _rest[index];
            float station = (headRest.Z - rest.Origin.Z) * ModelScale * Stretch;
            while (_trailLengths[sample] < station && sample < _trail.Length - 1) sample++;
            int previous = sample - 1;
            float blend = (station - _trailLengths[previous]) / (_trailLengths[sample] - _trailLengths[previous]);
            var position = _trail[previous].Lerp(_trail[sample], blend);
            var front = _trail[Math.Max(0, previous - 1)] - _trail[sample];
            var rear = _trail[previous] - _trail[Math.Min(sample + 1, _trail.Length - 1)];
            var tangent = (inverse.Basis * front.Normalized().Lerp(rear.Normalized(), blend)).Normalized();
            var frame = Basis.LookingAt(tangent, Vector3.Up, useModelFront: true);
            float along = station / length;
            float flank = Mathf.Clamp((along - .18f) / .42f, 0f, 1f);
            float tail = Mathf.Clamp((along - .60f) / .40f, 0f, 1f);
            var beat = StrokeAt(_wingPhase - .21f * flank - .095f * tail);
            float body = Mathf.SmoothStep(.10f, .34f, along) * (.60f + .40f * flank) * (1f - .42f * tail);
            float strokeLift = (.44f * beat.Rise - .21f * beat.Recover) * body * (.41f + .59f * _wingPower);
            float wave = Mathf.Sin(Mathf.Tau * (station / 11.8f - _wingPhase * .66f))
                * .70f * (.80f + .22f * _wingPower + .18f * drive)
                * Mathf.SmoothStep(1.6f, 6.5f, station) * (1f - .20f * Mathf.SmoothStep(.88f, 1f, along));
            var restOffset = new Vector3(rest.Origin.X - headRest.X, rest.Origin.Y - headRest.Y, 0f);
            var target = new Transform3D(frame * rest.Basis,
                inverse * (position + Vector3.Up * (strokeLift + wave)) + frame * restOffset);
            _skeleton.SetBonePose(_spine[index], parent.AffineInverse() * target);
            parent = target;
        }
    }

    private void PoseWings(float climb)
    {
        var stroke = StrokeAt(_wingPhase);
        float strength = .74f + .42f * _wingPower;
        float loading = .34f + .66f * stroke.Load;
        foreach (var wing in _wings)
        {
            var bones = wing.Bones;
            float side = wing.Side;
            for (int i = 0; i < bones.Length; i++)
                _skeleton.SetBonePoseRotation(bones[i], wing.Rest[i]);
            float beat = _wingPhase + side * .016f;
            float shoulder = .53f * WingElevation(beat);
            float elbow = .52f * WingElevation(beat - .055f);
            float wrist = .50f * WingElevation(beat - .106f);
            float pinion = .48f * WingElevation(beat - .146f);
            float tip = .46f * WingElevation(beat - .178f);
            float sweep = (.09f * WingFold(beat) * strength + .12f * Mathf.Max(0f, _turn * side) * loading) * side;
            float bank = -.15f * _turn * loading;
            float arc = side * ((shoulder - .045f + .056f * climb) * strength);
            RotateInModel(bones[0], Vector3.Up, sweep);
            RotateInModel(bones[0], Vector3.Back, .65f * arc + bank);
            RotateInModel(bones[1], Vector3.Up, sweep);
            RotateInModel(bones[1], Vector3.Back, arc + bank);
            RotateInModel(bones[2], Vector3.Back, side * (elbow - shoulder) * strength);
            float fold = WingFold(beat - .085f) * strength;
            RotateInModel(bones[3], Vector3.Up, side * .16f * fold);
            RotateInModel(bones[3], Vector3.Back, side * (wrist - elbow) * strength - .09f * _turn * loading);
            RotateInModel(bones[3], Vector3.Right, -.10f * fold);
            RotateInModel(bones[4], Vector3.Back, side * (pinion - wrist) * strength);
            RotateInModel(bones[5], Vector3.Back, side * (tip - pinion) * strength);
        }
    }

    private void PoseLegs(float climb, float drive)
    {
        float power = .31f + .69f * _wingPower;
        foreach (var leg in _legs)
        {
            float bank = _turn * leg.Side;
            float phase = _wingPhase - (leg.Rear ? .15f : .045f) - bank * (leg.Rear ? .13f : .08f);
            var beat = StrokeAt(phase);
            float shoulder = .07f * drive + (leg.Rear ? .06f : .11f) * climb
                + power * (.13f * beat.Rise - .19f * beat.Recover)
                + Mathf.Sin(phase * Mathf.Tau) * .045f + bank * (leg.Rear ? .27f : .39f);
            float elbow = power * (.31f * beat.Load + .13f * beat.Rise - .23f * beat.Recover);
            float paw = power * (-.13f * beat.Load + .17f * beat.Recover) - .15f * bank;
            FlexLeg(leg.Bones, 0, shoulder);
            FlexLeg(leg.Bones, 2, elbow);
            FlexLeg(leg.Bones, 4, paw);
        }
    }

    private void FlexLeg(int[] bones, int first, float angle)
    {
        // 原有五个变形关节共用一个父骨，远端必须绕同一铰点连带移动。
        var axis = ParentRotation(bones[first]).Inverse() * Vector3.Right;
        var rotation = new Quaternion(axis, angle);
        var pivot = _skeleton.GetBonePosePosition(bones[first]);
        for (int i = first; i < bones.Length; i++)
        {
            int bone = bones[i];
            var offset = _skeleton.GetBonePosePosition(bone) - pivot;
            _skeleton.SetBonePosePosition(bone, pivot + rotation * offset);
            _skeleton.SetBonePoseRotation(bone, rotation * _skeleton.GetBonePoseRotation(bone));
        }
    }

    private void FollowPlumage(float delta, float climb, float drive)
    {
        float step = Mathf.Clamp(delta, 0f, .05f);
        var wind = new Vector3(.18f * Mathf.Sin(_time * 2.1f) - _turn * .38f,
            .14f + .12f * Mathf.Sin(_time * 1.3f) - climb * .48f,
            -.22f - drive * .47f);
        // 先拿到整套基础姿态的受风方向，再施加各羽梢和龙须的弹簧。
        foreach (var strand in _plumage)
            if (strand.Kind == "feather") _skeleton.SetBonePoseRotation(strand.Bone, strand.Rest);
        foreach (var strand in _plumage)
        {
            var orientation = _skeleton.GetBoneGlobalPose(strand.Bone).Basis.Orthonormalized();
            strand.Pressure = wind.Dot(orientation.Z);
            strand.CrossLoad = wind.Dot(orientation.X);
        }
        foreach (var strand in _plumage)
        {
            float pressure = strand.Pressure, cross = strand.CrossLoad;
            float bend, twist, omega;
            Vector3 twistAxis;
            if (strand.Kind == "feather")
            {
                float outer = 1f - Mathf.SmoothStep(1f, 15f, strand.Index);
                float lag = (strand.Root ? .072f : .102f) + outer * .06f;
                float phase = _wingPhase + strand.Side * .016f - lag;
                float air = (WingElevation(phase - .025f) - WingElevation(phase + .025f))
                    * (.70f + .30f * _wingPower);
                float flex = strand.Root ? .36f : 1f;
                bend = Mathf.Clamp((pressure * strand.Length * .60f + air * .34f) * flex, -.15f, .16f);
                twist = Mathf.Clamp((cross * strand.Length * .32f + strand.Side * (.07f * air + .06f * _turn))
                    * flex, -.10f, .10f);
                omega = (strand.Root ? 24f : 20f) - outer * 3f;
                twistAxis = Vector3.Up;
            }
            else
            {
                float tip = Mathf.Clamp((strand.Index - 1f) / (strand.Kind == "whisker" ? 15f : 5f), 0f, 1f);
                float freedom = .21f + .79f * Mathf.SmoothStep(.04f, .86f, tip);
                float wave = _time * .76f - tip * 3f + strand.Side * .27f;
                float amplitude = strand.Kind == "whisker" ? 1f : 1.24f;
                bend = Mathf.Clamp((.039f * pressure + .030f * Mathf.Sin(wave)) * freedom * amplitude, -.08f, .08f);
                twist = Mathf.Clamp((-.037f * cross + .027f * Mathf.Cos(wave - .75f) + .026f * _turn)
                    * freedom * amplitude, -.076f, .076f);
                omega = 23f - tip * 12f;
                twistAxis = Vector3.Back;
            }
            strand.Spring(bend, twist, omega, step);
            var rotation = _skeleton.GetBonePoseRotation(strand.Bone)
                * new Quaternion(Vector3.Right, strand.Bend) * new Quaternion(twistAxis, strand.Twist);
            _skeleton.SetBonePoseRotation(strand.Bone, rotation);
        }
    }

    private Quaternion ParentRotation(int bone) =>
        _skeleton.GetBoneGlobalPose(_skeleton.GetBoneParent(bone)).Basis.Orthonormalized().GetRotationQuaternion();

    private void RotateInModel(int bone, Vector3 axis, float angle)
    {
        var rotation = new Quaternion(ParentRotation(bone).Inverse() * axis, angle);
        _skeleton.SetBonePoseRotation(bone, rotation * _skeleton.GetBonePoseRotation(bone));
    }

    private static Vector3 OrbitPoint(float angle) => new(
        Mathf.Cos(angle) * 2.82f,
        3.10f + .43f * Mathf.Sin(angle + .39f) + .13f * Mathf.Cos(angle * 2f),
        Mathf.Sin(angle) * 2.26f);

    // Numeric stroke and spring profiles are from Tideborn (see the asset provenance).
    private static float WingElevation(float phase)
    {
        phase = Mathf.PosMod(phase - .08f, 1f);
        const float downstroke = .38f;
        float arc = phase < downstroke ? phase / downstroke : 1f + (phase - downstroke) / (1f - downstroke);
        return Mathf.Cos(Mathf.Pi * arc);
    }

    private static float WingFold(float phase) => Pulse(Mathf.PosMod(phase, 1f), .40f, .68f, .71f, .99f);

    private static Stroke StrokeAt(float phase)
    {
        phase = Mathf.PosMod(phase, 1f);
        return new Stroke(Pulse(phase, .11f, .24f, .28f, .43f), Pulse(phase, .17f, .34f, .46f, .72f),
            Pulse(phase, .43f, .58f, .87f, 1f));
    }

    private static float Pulse(float t, float start, float peak, float release, float end) =>
        Mathf.SmoothStep(start, peak, t) * (1f - Mathf.SmoothStep(release, end, t));

    private readonly record struct Stroke(float Load, float Rise, float Recover);
    private readonly record struct Wing(int[] Bones, Quaternion[] Rest, float Side);
    private readonly record struct Leg(int[] Bones, float Side, bool Rear);

    private sealed class Plumage(int bone, string kind, int index, float side, bool root, float length, Quaternion rest)
    {
        internal readonly int Bone = bone, Index = index;
        internal readonly string Kind = kind;
        internal readonly float Side = side, Length = length;
        internal readonly bool Root = root;
        internal readonly Quaternion Rest = rest;
        internal float Bend, Twist, Pressure, CrossLoad;
        private float _bendVelocity, _twistVelocity;

        internal void Spring(float bend, float twist, float omega, float delta)
        {
            int steps = Math.Max(1, Mathf.CeilToInt(delta * 120f));
            float h = delta / steps;
            float stiffness = omega * omega;
            float damping = 1.52f * omega;
            for (int i = 0; i < steps; i++)
            {
                _bendVelocity += ((bend - Bend) * stiffness - damping * _bendVelocity) * h;
                _twistVelocity += ((twist - Twist) * stiffness - damping * _twistVelocity) * h;
                Bend += _bendVelocity * h;
                Twist += _twistVelocity * h;
            }
        }
    }
}
