using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityWorld.Game.Domain.Combat;
using Xianxia.Client.Battle;

namespace Xianxia.Client.UI;

public partial class ObserverDuelStage
{
    /// <summary>舞台音效按挥击、射出、碰撞、格挡、命中、重击、闪避、结印、回场、败退排列。</summary>
    [Export] public AudioStream[] SoundEffects { get; set; } = Array.Empty<AudioStream>();

    private enum Sound { Sweep, Release, Clash, Block, Hit, Heavy, Evade, Seal, Return, Defeat }
    internal readonly record struct SoundStamp(long Sequence, string Name, float Progress, float Mark);
    private readonly AudioStreamPlayer[] _voices = new AudioStreamPlayer[4];
    private readonly Queue<SoundStamp> _soundHistory = new();
    private (PresentationCue? Cue, float Progress) _heardShot;
    private readonly (PresentationCue? Cue, float Progress)[] _heardCasts = new (PresentationCue?, float)[2];
    private int _audioGeneration = -1;
    private int _voiceIndex;
    internal IEnumerable<SoundStamp> SoundHistory => _soundHistory;

    private void ConfigureAudio()
    {
        for (int index = 0; index < _voices.Length; index++)
        {
            var voice = new AudioStreamPlayer { VolumeDb = -8 };
            _voices[index] = voice;
            AddChild(voice);
        }
    }

    private void SyncAudio(int generation)
    {
        if (generation != _audioGeneration)
        {
            foreach (var voice in _voices) voice.Stop();
            _audioGeneration = generation;
            _heardShot = default;
            Array.Clear(_heardCasts);
            _soundHistory.Clear();
        }
        if (_shot is { Exchange: { } exchange } cue)
        {
            var fact = cue.Event;
            float hitAt = cue.ContactProgress;
            bool ranged = fact.Damage?.DamageType == "SheJi" ||
                fact.Contest is { } contest && (contest.Left.ContestType == "SheJi" || contest.Right.ContestType == "SheJi");
            float windup = Math.Max(cue.ApproachProgress + .018f, hitAt - (ranged ? .10f : .155f));
            PlayCrossed(cue, _heardShot, windup, ranged ? Sound.Release : Sound.Sweep);
            PlayCrossed(cue, _heardShot, hitAt, ContactSound(fact, exchange));
            if (exchange.FinishingNpcId.HasValue)
                PlayCrossed(cue, _heardShot, .79f, Sound.Defeat);
            if (!cue.ContinuesOnStage)
                PlayCrossed(cue, _heardShot, ReturnJump, Sound.Return);
        }
        _heardShot = (_shot, _shot?.Progress ?? 0);
        for (int side = 0; side < _casts.Length; side++)
        {
            var cast = _casts[side].Cue;
            if (cast != null) PlayCrossed(cast, _heardCasts[side], CastRelease, Sound.Seal);
            _heardCasts[side] = (cast, cast?.Progress ?? 0);
        }
    }

    private static Sound ContactSound(CombatPresentationEvent fact, DuelExchange exchange)
    {
        var hit = fact.Damage ?? exchange.ContestImpact;
        if (fact.Contest is { } contest)
        {
            if (Evades(exchange, contest.Left) || Evades(exchange, contest.Right)) return Sound.Evade;
            if (DuelExchange.Winner(contest)?.ContestType is "Shield" or "Block") return Sound.Block;
        }
        return hit switch
        {
            { HpDamage: 0, ShieldAbsorbed: > 0 } => Sound.Block,
            { HpDamage: > 0 } => HeavyHit(hit) ? Sound.Heavy : Sound.Hit,
            _ => Sound.Clash
        };
    }

    private void PlayCrossed(PresentationCue cue, (PresentationCue? Cue, float Progress) previous, float mark, Sound sound)
    {
        // 停帧不重复发声。快进跨过一整段时，只播距离当前画面足够近的音效。
        if (cue.Progress < mark || previous.Cue == cue && previous.Progress >= mark ||
            (cue.Progress - mark) * cue.Duration > .10) return;
        if (SoundEffects.ElementAtOrDefault((int)sound) is not { } stream) return;
        var voice = _voices[_voiceIndex];
        _voiceIndex = (_voiceIndex + 1) % _voices.Length;
        voice.Stream = stream;
        voice.Play();
        if (_soundHistory.Count >= 16) _soundHistory.Dequeue();
        _soundHistory.Enqueue(new SoundStamp(cue.Event.Sequence, sound.ToString(), cue.Progress, mark));
    }
}
