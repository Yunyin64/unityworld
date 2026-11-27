using System;
using Godot;

namespace Xianxia.Client.Exploration;

/// <summary>连接《潜鳞》原始应龙网格、皮肤权重和水墨材质。</summary>
internal static class ExplorationInkDragonMesh
{
    internal const string FlightClip = "Cloud_Flight";
    private const string Art = "res://assets/art/exploration/terrain3d/tideborn/";
    private const string Model = Art + "yinglong-rigged-v2-gray.glb";
    private const string InkShader = "res://assets/presentation/exploration_ink_dragon_3d.gdshader";

    internal static Node3D Instantiate(out AnimationPlayer player, out Skeleton3D skeleton)
    {
        var root = GD.Load<PackedScene>(Model).Instantiate<Node3D>();
        var rig = root.GetNode<Node3D>("Yinglong_Rig_v2");
        skeleton = rig.GetNode<Skeleton3D>("Skeleton3D");
        var material = new ShaderMaterial { Shader = GD.Load<Shader>(InkShader) };
        material.SetShaderParameter("brush_atlas", GD.Load<Texture2D>(Art + "shared-ink-brush-atlas.png"));
        material.SetShaderParameter("flow_noise", GD.Load<Texture2D>(Art + "shared-ink-flow-noise.png"));
        foreach (var child in skeleton.GetChildren())
            if (child is MeshInstance3D mesh) PrepareMesh(mesh, material);

        player = root.GetNode<AnimationPlayer>("AnimationPlayer");
        var library = new AnimationLibrary();
        foreach (var name in player.GetAnimationList())
        {
            var animation = (Animation)player.GetAnimation(name).Duplicate();
            if (name == FlightClip) animation.LoopMode = Animation.LoopModeEnum.Linear;
            library.AddAnimation(name, animation);
        }
        player.RemoveAnimationLibrary("");
        player.AddAnimationLibrary("", library);
        return root;
    }

    private static void PrepareMesh(MeshInstance3D instance, ShaderMaterial ink)
    {
        var original = (ArrayMesh)instance.Mesh;
        var painted = new ArrayMesh();
        var skin = instance.Skin;
        var wingBinds = new bool[skin.GetBindCount()];
        for (int bind = 0; bind < wingBinds.Length; bind++)
        {
            var name = skin.GetBindName(bind).ToString();
            wingBinds[bind] = name.StartsWith("DEF_wing_", StringComparison.Ordinal)
                || name.StartsWith("DEF_feather_", StringComparison.Ordinal);
        }
        for (int surface = 0; surface < original.GetSurfaceCount(); surface++)
        {
            var arrays = original.SurfaceGetArrays(surface);
            var points = arrays[(int)Mesh.ArrayType.Vertex].AsVector3Array();
            var normals = arrays[(int)Mesh.ArrayType.Normal].AsVector3Array();
            var bones = arrays[(int)Mesh.ArrayType.Bones].AsInt32Array();
            var weights = arrays[(int)Mesh.ArrayType.Weights].AsFloat32Array();
            int influences = bones.Length / points.Length;
            var paint = new float[points.Length * 4];
            var formNormals = new float[paint.Length];
            for (int index = 0; index < points.Length; index++)
            {
                float wing = 0f;
                for (int j = 0; j < influences; j++)
                {
                    int influence = index * influences + j;
                    if (wingBinds[bones[influence]]) wing += weights[influence];
                }
                // 落墨用未变形的原模坐标。可见网格仍使用 GLB 原来的骨骼和权重。
                int offset = index * 4;
                paint[offset] = points[index].X;
                paint[offset + 1] = points[index].Y;
                paint[offset + 2] = points[index].Z;
                paint[offset + 3] = wing;
                formNormals[offset] = normals[index].X;
                formNormals[offset + 1] = normals[index].Y;
                formNormals[offset + 2] = normals[index].Z;
            }
            arrays[(int)Mesh.ArrayType.Custom0] = paint;
            arrays[(int)Mesh.ArrayType.Custom1] = formNormals;
            var format = (Mesh.ArrayFormat)(((ulong)Mesh.ArrayCustomFormat.RgbaFloat << (int)Mesh.ArrayFormat.FormatCustom0Shift)
                | ((ulong)Mesh.ArrayCustomFormat.RgbaFloat << (int)Mesh.ArrayFormat.FormatCustom1Shift));
            format |= original.SurfaceGetFormat(surface) & Mesh.ArrayFormat.FlagUse8BoneWeights;
            painted.AddSurfaceFromArrays(original.SurfaceGetPrimitiveType(surface), arrays, flags: format);
        }
        instance.Mesh = painted;
        instance.MaterialOverride = ink;
        instance.CustomAabb = new Aabb(new Vector3(-1.4f, -.82f, -1.65f), new Vector3(2.8f, 1.64f, 2.3f));
        instance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
    }
}
