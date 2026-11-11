using System;
using Godot;
using UnityWorld.Game.Domain;

namespace Xianxia.Client.Exploration;

/// <summary>Flat-top ground coordinates, independent of camera pan and of the size of any drawn character.</summary>
public static class ExplorationHexProjection
{
    public const float Compression = 0.65f;
    public const float RowSpacing = 1.7320508f * Compression;
    public const float HalfHeight = RowSpacing / 2f;

    public static Vector2 Project(TileId relative, float edge) =>
        new(1.5f * edge * relative.Q, RowSpacing * edge * (relative.R + relative.Q * 0.5f));

    public static TileId Unproject(Vector2 offset, float edge)
    {
        if (edge <= 0) throw new ArgumentOutOfRangeException(nameof(edge));
        // Undo the same vertical compression used for drawing, then round all three cube axes
        // together. A corner belongs to the actual hex, rather than to its rectangular bounds.
        double q = offset.X / (1.5 * edge);
        double r = offset.Y / (RowSpacing * edge) - q * 0.5;
        double s = -q - r;
        int roundedQ = (int)Math.Round(q), roundedR = (int)Math.Round(r), roundedS = (int)Math.Round(s);
        double errorQ = Math.Abs(roundedQ - q), errorR = Math.Abs(roundedR - r), errorS = Math.Abs(roundedS - s);
        if (errorQ > errorR && errorQ > errorS) roundedQ = -roundedR - roundedS;
        else if (errorR > errorS) roundedR = -roundedQ - roundedS;
        return new TileId(roundedQ, roundedR);
    }

    internal static Vector2[] Outline(Vector2 center, float edge) => new[]
    {
        center + new Vector2(edge, 0), center + new Vector2(edge * 0.5f, edge * HalfHeight),
        center + new Vector2(-edge * 0.5f, edge * HalfHeight), center + new Vector2(-edge, 0),
        center + new Vector2(-edge * 0.5f, -edge * HalfHeight), center + new Vector2(edge * 0.5f, -edge * HalfHeight)
    };
}
