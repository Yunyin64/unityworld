using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Xianxia.Client.Battle;

namespace Xianxia.Client.BattleWindows;

// 一个完整 Tick 和它新增的战报一起传输。确认控制命令不伪造一个新 Tick。
internal sealed record ObserverPacket(string Kind, string Session, BattlePresentationFrame? Frame = null,
    IReadOnlyList<BattleRecord>? Records = null, BattleCase? Case = null, string? Directory = null,
    bool Playing = false, bool CanAdvance = false, string? Error = null)
{
    public static ObserverPacket Decode(string json) => JsonSerializer.Deserialize<ObserverPacket>(json, BattleJson.Options)
        ?? throw new InvalidDataException("观察器消息为空。");

    public void Write(StreamWriter writer) => writer.WriteLine(JsonSerializer.Serialize(this, BattleJson.Options));
}
