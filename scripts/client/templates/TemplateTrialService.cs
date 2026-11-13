using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using Xianxia.Client.Battle;

namespace Xianxia.Client.Templates;

public sealed record TemplateTrialSettings(int WorldSeed = 12345, int CombatSeed = 12345,
    int MaxTicks = 5000, int TicksPerRow = 10);

/// <summary>Preserves structured editor issues for an editor to locate the participant and card.</summary>
public sealed class TemplateTrialValidationException : InvalidOperationException
{
    public string TemplateId { get; }
    public TemplateValidationReport Report { get; }

    public TemplateTrialValidationException(string templateId, TemplateValidationReport report)
        : base($"模板 {templateId} 不能试战：" + string.Join("；", report.Issues
            .Where(i => i.Severity == TemplateIssueSeverity.Error).Select(i => i.Message)))
    { TemplateId = templateId; Report = report; }
}

/// <summary>Pure input preparation and disk persistence; never creates NPCs, starts a world, or saves an editor draft.</summary>
public static class TemplateTrialService
{
    /// <summary>Call only after the draft converter has rejected unresolved bindings and library ID conflicts.</summary>
    public static BattleCase Freeze(TemplateCatalog catalog, NpcDefine self, BattleParticipant opponent,
        TemplateTrialSettings? settings = null, string selfSnapshotId = "")
    {
        settings ??= new();
        catalog.AssertDiskUnchanged();
        var selfParticipant = BattleParticipant.FromSnapshot(self, "TeamA", selfSnapshotId);
        var input = new BattleCase
        {
            Id = "template-trial-" + Guid.NewGuid().ToString("N"),
            Name = $"{DisplayName(self)} · 模板试战",
            SourceMethod = "TemplateTrialService.Freeze",
            WorldSeed = settings.WorldSeed, CombatSeed = settings.CombatSeed,
            MaxTicks = settings.MaxTicks, TicksPerRow = settings.TicksPerRow,
            Participants = Array.AsReadOnly(new[] { selfParticipant, opponent.Copy() }),
            DataFingerprint = catalog.DataFingerprint,
            CoreVersion = BattleCaseFactory.CoreVersion
        };
        BattleCaseFactory.Validate(input);
        if (input.Participants[1].Team != "TeamB")
            throw new InvalidDataException("模板试战对手必须属于 TeamB。");
        foreach (var participant in input.Participants)
        {
            var definition = participant.Source == BattleParticipant.SnapshotSource
                ? participant.Definition!
                : catalog.GetNpc(participant.TemplateId)
                    ?? throw new InvalidDataException($"项目对手不存在：{participant.TemplateId}。");
            var report = TemplateEditorValidator.Validate(definition, catalog);
            if (!report.IsValid) throw new TemplateTrialValidationException(participant.TemplateId, report);
        }
        // Validation reads the successful catalog; reject changes made on disk during preparation too.
        catalog.AssertDiskUnchanged();
        return input.Copy();
    }

    /// <summary>Writes a new private session input, not a reference to a mutable draft file.</summary>
    public static string WriteInput(BattleCase input, string trialsDirectory, string dataRoot)
    {
        var copy = input.Copy();
        BattleCaseFactory.Validate(copy);
        AssertDataIdentity(copy, dataRoot);
        return BattleCaseInput.Write(copy, trialsDirectory);
    }

    public static void AssertDataIdentity(BattleCase input, string dataRoot)
    {
        if (string.IsNullOrWhiteSpace(input.DataFingerprint))
            throw new InvalidDataException("试战输入缺少已加载目录的数据指纹。");
        if (input.DataFingerprint != BattleCaseFactory.Fingerprint(dataRoot).Sha256)
            throw new InvalidOperationException("磁盘玩法数据已变化，请刷新目录并重新检查当前模板后再试战。");
        if (input.CoreVersion != BattleCaseFactory.CoreVersion)
            throw new InvalidOperationException("核心版本已变化，请重新打开编辑器并重新发起试战。");
    }

    private static string DisplayName(NpcDefine definition) =>
        string.IsNullOrWhiteSpace(definition.DisplayName) ? definition.ID : definition.DisplayName;
}
