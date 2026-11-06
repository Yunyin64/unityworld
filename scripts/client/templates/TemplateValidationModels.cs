using System;
using System.Collections.Generic;
using System.Linq;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;

namespace Xianxia.Client.Templates;

public enum TemplateIssueSeverity { Warning, Error }

/// <summary>PlacementIndex 从 1 开始；Cells 使用公开的从 1 开始坐标。</summary>
public sealed record TemplateIssue(string Code, TemplateIssueSeverity Severity,
    string Message, string FieldPath, int? PlacementIndex = null,
    IReadOnlyList<BackpackPosition>? Cells = null);

/// <summary>客户端配置检查结果，附带用于编辑器展示的布局和属性预览。</summary>
public sealed class TemplateValidationReport
{
    public IReadOnlyList<TemplateIssue> Issues { get; init; } = Array.Empty<TemplateIssue>();
    public bool IsValid => Issues.All(issue => issue.Severity != TemplateIssueSeverity.Error);
    public BackpackLevelDefine? Layout { get; init; }
    public int OccupiedSp { get; init; }
    public float? HpMax { get; init; }
    public float? MpMax { get; init; }
    public float? SpMax { get; init; }
}

