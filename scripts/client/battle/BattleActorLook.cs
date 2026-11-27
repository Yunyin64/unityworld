using System;
using System.Collections.Generic;

namespace Xianxia.Client.Battle;

/// <summary>客户端部件 ID 和可选染色，跟随个人草稿和试战输入。</summary>
public sealed record BattleActorPart(string Id, string? Dye = null);

/// <summary>一个人物的组装结果，键为外观目录中的部位 ID。</summary>
public sealed record BattleActorLook(string Rig, Dictionary<string, BattleActorPart> Parts)
{
    /// <summary>试战和重放保留独立副本。</summary>
    public BattleActorLook Copy() => this with { Parts = new(Parts ?? new(), StringComparer.Ordinal) };

    /// <summary>只替换指定部位，避免共享草稿的字典被修改。</summary>
    public BattleActorLook WithPart(string group, BattleActorPart part)
    {
        var copy = Copy();
        copy.Parts[group] = part;
        return copy;
    }
}
