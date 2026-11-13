using System.Collections.Generic;

namespace Xianxia.Client.Battle;

/// <summary>观察界面只读这些值，本地模拟和跨进程模拟显示同一份布局。</summary>
public interface IBattleObserverState
{
    BattleCase Case { get; }
    string DataFingerprint { get; }
    BattleSnapshot CurrentSnapshot { get; }
    IReadOnlyList<BattleRecord> Records { get; }
    bool IsPlaying { get; }
    bool CanAdvance { get; }
    string? Diagnostic { get; }
}
