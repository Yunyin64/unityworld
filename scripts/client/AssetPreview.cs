using Godot;
using System;
using System.Collections.Generic;

namespace Xianxia.Client;

/// <summary>Static art loading preview. Does not initialize or reference the game core.</summary>
public partial class AssetPreview : Control
{
    /// <summary>Checks scene textures and optionally captures a preview for visual verification.</summary>
    public override async void _Ready()
    {
        var textures = new HashSet<string>();
        foreach (var node in FindChildren("*", "TextureRect"))
        {
            if (node is not TextureRect image)
                continue;
            if (image.Texture == null)
            {
                GD.PushError($"[AssetPreview] Missing texture: {image.GetPath()}");
                return;
            }
            var source = image.Texture is AtlasTexture atlas ? atlas.Atlas : image.Texture;
            textures.Add(source.ResourcePath);
        }
        GD.Print($"[AssetPreview] Loaded {textures.Count} independent art textures. Static scene ready.");

        foreach (var argument in OS.GetCmdlineUserArgs())
        {
            const string capturePrefix = "--preview-capture=";
            if (!argument.StartsWith(capturePrefix, StringComparison.Ordinal))
                continue;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            var error = GetViewport().GetTexture().GetImage().SavePng(argument[capturePrefix.Length..]);
            GD.Print($"[AssetPreview] Capture: {error}");
            GetTree().Quit(error == Error.Ok ? 0 : 1);
        }
    }
}
