using System.Collections.Generic;
using Godot;

namespace Xianxia.Client.Exploration;

/// <summary>在现有探索几何上切换纸、墨、设色和雾的材质。</summary>
internal sealed class ExplorationInkMode
{
    private const string SurfaceShader = "res://assets/presentation/exploration_ink_surface_3d.gdshader";
    private const string FoliageShader = "res://assets/presentation/exploration_foliage_3d.gdshader";
    private const string CliffBrush = "res://assets/art/exploration/terrain3d/tideborn/chapter-cliff-ink-c76.webp";
    private static readonly Color Paper = new("#f3ecdc");

    private readonly WorldEnvironment _world;
    private readonly Godot.Environment _naturalEnvironment;
    private readonly Godot.Environment _inkEnvironment;
    private readonly CanvasItem _paper;
    private readonly MeshInstance3D _wetInk;
    private readonly ShaderMaterial _outline;
    private readonly Surface[] _terrain;
    private readonly ShaderMaterial _distantInk;
    private readonly Dictionary<(Color Tint, bool VertexColor), ShaderMaterial> _objectInks = new();
    private readonly List<Surface> _objects = new();
    private readonly List<GeometryInstance3D> _foliage = new();
    private Node3D? _scenery;
    private Surface? _backdrop;

    internal ExplorationInkMode(WorldEnvironment world, CanvasItem paper, MeshInstance3D wetInk, ShaderMaterial outline,
        MeshInstance3D land, MeshInstance3D water, MeshInstance3D ocean, MeshInstance3D rim)
    {
        _world = world;
        _naturalEnvironment = world.Environment;
        _inkEnvironment = (Godot.Environment)_naturalEnvironment.Duplicate();
        _inkEnvironment.BackgroundMode = Godot.Environment.BGMode.Color;
        _inkEnvironment.BackgroundColor = Paper;
        _inkEnvironment.FogLightColor = Paper;
        _inkEnvironment.TonemapMode = Godot.Environment.ToneMapper.Linear;
        _inkEnvironment.SsrEnabled = false;
        _inkEnvironment.SsaoEnabled = false;
        _inkEnvironment.SsilEnabled = false;
        // 只用纸色深度雾。原近景的暖光体积雾会在远山和天空之间压出一条褐色带。
        _inkEnvironment.VolumetricFogEnabled = false;
        _paper = paper;
        _wetInk = wetInk;
        _outline = outline;
        _terrain = new[]
        {
            new Surface(land, land.MaterialOverride, MaterialFor(SurfaceKind.Terrain, new("#303638"), new("#647c6b"))),
            new Surface(water, water.MaterialOverride, MaterialFor(SurfaceKind.Water, new("#435658"), new("#718c85"))),
            new Surface(ocean, ocean.MaterialOverride, MaterialFor(SurfaceKind.OuterWater, new("#435658"), new("#718c85"))),
            new Surface(rim, rim.MaterialOverride, MaterialFor(SurfaceKind.Object, new("#414544"), new("#85857b")))
        };
        _distantInk = MaterialFor(SurfaceKind.DistantPeak, new("#454e52"), new("#6c8380"));
    }

    internal bool Enabled { get; private set; }

    internal void SetEnabled(bool enabled)
    {
        if (Enabled == enabled) return;
        Enabled = enabled;
        _world.Environment = enabled ? _inkEnvironment : _naturalEnvironment;
        _paper.Visible = enabled;
        _wetInk.Visible = enabled;
        _outline.SetShaderParameter("ink_mode", enabled);
        foreach (var surface in _terrain) surface.Apply(enabled);
        _backdrop?.Apply(enabled);
        if (enabled) ApplyScenery();
        else RestoreScenery();
    }

    internal void SetScenic(bool scenic)
    {
        _inkEnvironment.FogDepthBegin = scenic ? 7f : 27f;
        _inkEnvironment.FogDepthEnd = scenic ? 31f : 60f;
        _inkEnvironment.FogDensity = scenic ? .83f : .58f;
    }

    internal void SetScenery(Node3D scenery)
    {
        RestoreScenery();
        _scenery = scenery;
        if (Enabled) ApplyScenery();
    }

    internal void SetBackdrop(MeshInstance3D backdrop)
    {
        _backdrop = new Surface(backdrop, backdrop.MaterialOverride, _distantInk);
        _backdrop.Value.Apply(Enabled);
    }

    private void ApplyScenery()
    {
        if (_scenery != null) ApplyObject(_scenery);
    }

    private void ApplyObject(Node node)
    {
        if (node is GeometryInstance3D geometry)
        {
            var original = geometry.MaterialOverride;
            if (original is ShaderMaterial leaves && leaves.Shader.ResourcePath == FoliageShader)
            {
                // 描边视口的叶片副本保留同一裁切与风动，只有主场景改色。
                geometry.SetInstanceShaderParameter("ink_mode", true);
                _foliage.Add(geometry);
            }
            else if (original is BaseMaterial3D material)
            {
                var key = (material.AlbedoColor, material.VertexColorUseAsAlbedo);
                if (!_objectInks.TryGetValue(key, out var ink))
                {
                    var tint = new Color("#929385").Lerp(key.AlbedoColor, .50f);
                    ink = MaterialFor(SurfaceKind.Object, new("#262e2c"), tint);
                    ink.SetShaderParameter("use_vertex_color", key.VertexColorUseAsAlbedo);
                    _objectInks.Add(key, ink);
                }
                var surface = new Surface(geometry, original, ink);
                surface.Apply(true);
                _objects.Add(surface);
            }
        }
        foreach (var child in node.GetChildren()) ApplyObject(child);
    }

    private void RestoreScenery()
    {
        foreach (var surface in _objects)
            if (GodotObject.IsInstanceValid(surface.Geometry)) surface.Apply(false);
        foreach (var leaf in _foliage)
            if (GodotObject.IsInstanceValid(leaf)) leaf.SetInstanceShaderParameter("ink_mode", false);
        _objects.Clear();
        _foliage.Clear();
    }

    private static ShaderMaterial MaterialFor(SurfaceKind kind, Color pigment, Color mineral)
    {
        var material = new ShaderMaterial { Shader = GD.Load<Shader>(SurfaceShader) };
        material.SetShaderParameter("surface_kind", (int)kind);
        material.SetShaderParameter("paper", Paper);
        material.SetShaderParameter("pigment", pigment);
        material.SetShaderParameter("mineral", mineral);
        if (kind is SurfaceKind.Terrain or SurfaceKind.DistantPeak)
            material.SetShaderParameter("cliff_brush", GD.Load<Texture2D>(CliffBrush));
        return material;
    }

    private enum SurfaceKind { Terrain, DistantPeak, Object, Water, OuterWater }

    private readonly record struct Surface(GeometryInstance3D Geometry, Material? Natural, ShaderMaterial Ink)
    {
        internal void Apply(bool ink) => Geometry.MaterialOverride = ink ? Ink : Natural;
    }
}
