using System.Collections.Generic;
using Godot;

namespace Xianxia.Client.Exploration;

internal enum OutlineProfile
{
    Off = 0, Mountain = 1, Foliage = 2, Building = 3, Ground = 4, Water = 5, Figure = 6, Stone = 7
}

/// <summary>将可见模型的轮廓类别绘制到独立缓冲。描边与主镜头使用同一份几何和相机投影。</summary>
public partial class ExplorationOutline3D : SubViewport
{
    private const string ProfileMeta = "outline_profile";
    private const string LabelShader = "res://assets/presentation/exploration_outline_id_3d.gdshader";
    private readonly Dictionary<(OutlineProfile, bool), ShaderMaterial> _materials = new();
    private readonly List<MaskSource> _sources = new();
    private Camera3D _camera = null!, _maskCamera = null!;
    private MeshInstance3D _pass = null!;
    private bool _enabled;

    internal void Configure(Camera3D camera, MeshInstance3D pass)
    {
        _camera = camera;
        _pass = pass;
        OwnWorld3D = true;
        ProcessPriority = 1;
        _maskCamera = new Camera3D
        {
            Name = "OutlineMaskCamera",
            Current = true,
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color, BackgroundColor = Colors.Black
            }
        };
        AddChild(_maskCamera);
        ((ShaderMaterial)_pass.MaterialOverride).SetShaderParameter("outline_profiles", GetTexture());
        SynchronizeCamera();
    }

    internal void SetEnabled(bool enabled)
    {
        _enabled = enabled;
        _pass.Visible = enabled;
        RenderTargetUpdateMode = enabled ? UpdateMode.Always : UpdateMode.Disabled;
        if (enabled) SynchronizeCamera();
    }

    internal static void Mark(GeometryInstance3D node, OutlineProfile profile) => node.SetMeta(ProfileMeta, (int)profile);

    internal void TrackTree(Node3D root, OutlineProfile fallback)
    {
        if (root is GeometryInstance3D geometry)
            Track(geometry, geometry.HasMeta(ProfileMeta) ? (OutlineProfile)geometry.GetMeta(ProfileMeta).AsInt32() : fallback);
        foreach (var child in root.GetChildren())
            if (child is Node3D node) TrackTree(node, fallback);
    }

    internal void Track(GeometryInstance3D original, OutlineProfile profile, bool terrain = false)
    {
        GeometryInstance3D proxy;
        if (original is MeshInstance3D) proxy = new MeshInstance3D();
        else if (original is MultiMeshInstance3D) proxy = new MultiMeshInstance3D();
        else return;

        proxy.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
        if (profile == OutlineProfile.Foliage && original.MaterialOverride is ShaderMaterial leaves)
        {
            // 保留叶片的透明裁切、风动和贴图采样，让类别边和实际遮挡边一致。
            var cutout = (ShaderMaterial)leaves.Duplicate();
            cutout.SetShaderParameter("outline_profile_capture", true);
            proxy.MaterialOverride = cutout;
        }
        else proxy.MaterialOverride = ProfileMaterial(profile, terrain);
        AddChild(proxy);
        var binding = new MaskSource(original, proxy);
        _sources.Add(binding);
        SyncSource(binding);
    }

    /// <summary>同步当前视野和变动的模型，离开场景的模型同时从分类缓冲移除。</summary>
    public override void _Process(double delta)
    {
        if (_enabled) SynchronizeCamera();
        for (int i = _sources.Count - 1; i >= 0; i--)
        {
            var binding = _sources[i];
            if (!IsInstanceValid(binding.Original) || !binding.Original.IsInsideTree())
            {
                binding.Proxy.Hide();
                binding.Proxy.QueueFree();
                _sources.RemoveAt(i);
            }
            else if (_enabled) SyncSource(binding);
        }
    }

    private void SynchronizeCamera()
    {
        var imageSize = _camera.GetViewport().GetTexture().GetSize();
        var resolution = new Vector2I((int)imageSize.X, (int)imageSize.Y).Max(new Vector2I(2, 2));
        if (Size != resolution) Size = resolution;
        _maskCamera.GlobalTransform = _camera.GlobalTransform;
        _maskCamera.Projection = _camera.Projection;
        _maskCamera.KeepAspect = _camera.KeepAspect;
        _maskCamera.Near = _camera.Near;
        _maskCamera.Far = _camera.Far;
        _maskCamera.Size = _camera.Size;
        _maskCamera.Fov = _camera.Fov;
    }

    private ShaderMaterial ProfileMaterial(OutlineProfile profile, bool terrain)
    {
        if (_materials.TryGetValue((profile, terrain), out var material)) return material;
        material = new ShaderMaterial { Shader = GD.Load<Shader>(LabelShader) };
        material.SetShaderParameter("profile_id", (int)profile);
        material.SetShaderParameter("from_terrain", terrain);
        _materials.Add((profile, terrain), material);
        return material;
    }

    private static void SyncSource(MaskSource binding)
    {
        var original = binding.Original;
        var proxy = binding.Proxy;
        if (original is MeshInstance3D mesh && proxy is MeshInstance3D maskMesh && maskMesh.Mesh != mesh.Mesh)
            maskMesh.Mesh = mesh.Mesh;
        if (original is MultiMeshInstance3D batch && proxy is MultiMeshInstance3D maskBatch && maskBatch.Multimesh != batch.Multimesh)
            maskBatch.Multimesh = batch.Multimesh;
        proxy.GlobalTransform = original.GlobalTransform;
        proxy.Visible = original.IsVisibleInTree();
    }

    private readonly record struct MaskSource(GeometryInstance3D Original, GeometryInstance3D Proxy);
}
