using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Godot;
using UnityWorld.Game.Data;
using UnityWorld.Game.Domain;
using Xianxia.Client.Exploration;
using Xianxia.Client.UI.Exploration;

namespace Xianxia.Client.UI.Landmarks;

/// <summary>通过正常首页、编辑器和 2D 探索场景验收导出与地标绘制。</summary>
internal static class LandmarkEditorCheck
{
    private static readonly string[] BuiltIns = ["sect_archive", "alchemy_pavilion", "spirit_herb_garden", "sword_practice_ground",
        "array_altar", "cloud_ferry", "seclusion_cave", "spirit_waterfall", "thunder_bamboo_grove", "cloud_spirit_spring"];
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    internal static async Task Run(SceneTree tree)
    {
        string id = "landmark_check_" + Guid.NewGuid().ToString("N");
        string json = Path.Combine(GameBootstrap.ResolveDataRoot(), "Data", "LandMarkDefines", id + ".json");
        string images = Path.Combine(GameBootstrap.ResolveInstallRoot(), "src", "landmarks");
        string png = Path.Combine(images, id + ".png");
        string? replacementPng = null;
        string? evidence = OS.GetCmdlineUserArgs().FirstOrDefault(arg => arg.StartsWith("--landmark-evidence=", StringComparison.Ordinal))?
            ["--landmark-evidence=".Length..];
        int exitCode = 1;
        try
        {
            await Frames(tree);
            if (evidence != null) Directory.CreateDirectory(evidence);
            await ClickNavigate(tree, "%LandmarkEditor");
            Check(tree.CurrentScene is LandmarkEditorView, "首页没有进入地标编辑器。");
            var editor = (LandmarkEditorView)tree.CurrentScene;
            var library = Named<ItemList>(editor, "Library");
            var export = Named<Button>(editor, "Export");
            var idField = Named<LineEdit>(editor, "ID");
            int source = Enumerable.Range(0, library.ItemCount).Single(i => library.GetItemMetadata(i).AsString() == "array_altar");
            library.EmitSignal(ItemList.SignalName.ItemSelected, source);
            Check(idField.Text == "array_altar" && Named<TextureRect>(editor, "ImagePreview").Texture != null,
                "无法读取已存在的带图定义。");
            Check(export.Text == "保存修改" && !idField.Editable, "已有定义应可保存并锁定原 ID。");
            Check(Named<LineEdit>(editor, "PerTileChance").Text.Length == 0 && Named<LineEdit>(editor, "MinCount").Text.Length == 0,
                "省略的创世数值在表单中应保持留空。");
            await Capture(tree, evidence, "landmark-editor");
            Named<Button>(editor, "CopyLandmark").EmitSignal(Button.SignalName.Pressed);
            Check(idField.Text == "array_altar_copy" && idField.Editable && export.Text == "导出新地标", "复制没有新建独立草稿。");
            var title = Named<LineEdit>(editor, "DisplayName");
            idField.Text = id;
            title.Text = "验收灵台";
            Named<TextEdit>(editor, "ForcedPositions").Text = "100,98";
            Named<LineEdit>(editor, "PerTileChance").Text = "0";
            Named<LineEdit>(editor, "MinCount").Text = "0";
            Named<LineEdit>(editor, "MaxCount").Text = "1";
            var need = Named<LineEdit>(editor, "NeedCount");
            need.Text = "2";
            Named<LineEdit>(editor, "MinSpacing").Text = "6";
            Named<LineEdit>(editor, "TerrainConditions").Text = "mountain, forest_mountain";
            Named<LineEdit>(editor, "LandMarkConditions").Text = "Near(3):natural_leyline_eye";
            Named<LineEdit>(editor, "TagConditions").Text = "山类, No:寒冷";
            string reservedText = "future condition\nverbatim text";
            Named<TextEdit>(editor, "ExtraCondition").Text = reservedText;
            Named<LineEdit>(editor, "DrawType").Text = "Icon";
            Check(!Named<CheckButton>(editor, "IsHide").ButtonPressed, "隐藏默认值错误。");
            Named<FileDialog>(editor, "ImagePicker").EmitSignal(FileDialog.SignalName.FileSelected, Path.Combine(images, "array_altar.png"));
            Check(Named<TextureRect>(editor, "ImagePreview").Texture != null, "导入 PNG 未显示预览。");

            var status = Named<Label>(editor, "Status");
            var modifiers = Named<LineEdit>(editor, "ModifierDefineIds");
            modifiers.Text = "missing_landmark_modifier_for_check";
            export.EmitSignal(Button.SignalName.Pressed);
            await WaitForExport(tree, export);
            Check(status.Text.Contains(modifiers.Text) && !File.Exists(json) && !File.Exists(png), "未知修正没有拒绝，或留下了部分导出文件。");
            modifiers.Text = "";
            export.EmitSignal(Button.SignalName.Pressed);
            await WaitForExport(tree, export);
            Check(status.Text.Contains("NeedCount") && !File.Exists(json) && !File.Exists(png), "保底超过上限时错误导出。");
            need.Text = "1";
            export.EmitSignal(Button.SignalName.Pressed);
            await WaitForExport(tree, export);
            Check(status.Text.StartsWith("已导出", StringComparison.Ordinal) && File.Exists(json) && File.Exists(png), status.Text);
            var saved = MapCatalog.Load(GameBootstrap.ResolveDataRoot()).LandMarks[id];
            Check(saved.InitialPlacement is { PerTileChance: 0, MinCount: 0, MaxCount: 1, NeedCount: 1, MinSpacing: 6 } placement &&
                placement.TerrainConditions.SequenceEqual(new[] { "mountain", "forest_mountain" }) &&
                placement.LandMarkConditions.SequenceEqual(new[] { "Near(3):natural_leyline_eye" }) &&
                placement.TagConditions.SequenceEqual(new[] { "山类", "No:寒冷" }) && placement.ExtraCondition == reservedText &&
                saved.DrawType == "Icon" && !saved.IsHide, "新表单字段未完整导出。");
            Check(export.Text == "保存修改" && !idField.Editable, "导出后应进入原定义编辑状态。");

            // 模拟另一个工具在同一个源文件中追加了地标。
            var records = JsonNode.Parse(File.ReadAllText(json))!.AsArray();
            var sibling = JsonSerializer.SerializeToNode(new LandMarkDefine { ID = id + "_neighbor", DisplayName = "同文件定义" })!;
            records.Add(sibling);
            File.WriteAllText(json, records.ToJsonString());
            byte[] modifiedOutside = File.ReadAllBytes(json);
            title.Text = "验收灵台已修改";
            export.EmitSignal(Button.SignalName.Pressed);
            await WaitForExport(tree, export);
            Check(status.Text.Contains("源文件在打开后已变更") && modifiedOutside.SequenceEqual(File.ReadAllBytes(json)),
                "已有定义保存覆盖了打开后发生的外部修改。");

            int savedEntry = Enumerable.Range(0, library.ItemCount).Single(i => library.GetItemMetadata(i).AsString() == id);
            library.EmitSignal(ItemList.SignalName.ItemSelected, savedEntry);
            var discard = Named<ConfirmationDialog>(editor, "DiscardChanges");
            if (discard.Visible)
            {
                discard.EmitSignal(ConfirmationDialog.SignalName.Confirmed);
                discard.Hide();
            }
            Check(title.Text == "验收灵台", "重新打开地标时没有从源文件更新草稿。");

            need.Text = "2";
            export.EmitSignal(Button.SignalName.Pressed);
            await WaitForExport(tree, export);
            Check(status.Text.Contains("NeedCount") && modifiedOutside.SequenceEqual(File.ReadAllBytes(json)) && need.Text == "2",
                "已有定义的非法配置应保留草稿和源文件。");

            need.Text = "1";
            title.Text = "验收灵台已修改";
            Named<LineEdit>(editor, "PerTileChance").Text = "0.125";
            Named<LineEdit>(editor, "MinCount").Text = "1";
            export.EmitSignal(Button.SignalName.Pressed);
            await WaitForExport(tree, export);
            Check(status.Text.StartsWith("已保存", StringComparison.Ordinal), status.Text);
            var updated = MapCatalog.Load(GameBootstrap.ResolveDataRoot());
            Check(updated.LandMarkSources[id] == Path.GetFullPath(json), "已有定义应写入原文件。");
            Check(updated.LandMarks[id].DisplayName == "验收灵台已修改" && title.Text == "验收灵台已修改" &&
                updated.LandMarks[id].InitialPlacement is { PerTileChance: .125, MinCount: 1, MaxCount: 1, NeedCount: 1 },
                "保存并重新读取后应得到原 ID 下的新数值。");
            Check(updated.LandMarks[id].ImagePath == $"landmarks/{id}.png" &&
                JsonNode.DeepEquals(sibling, JsonNode.Parse(File.ReadAllText(json))!.AsArray()[1]), "保存数值应保留原图引用和同文件的其他定义。");

            // 只有创世时才计算落点容量。静态合法的宽间距与保底配置也应当能保存。
            var max = Named<LineEdit>(editor, "MaxCount");
            var spacing = Named<LineEdit>(editor, "MinSpacing");
            max.Text = "";
            need.Text = "500";
            spacing.Text = "2147483647";
            export.EmitSignal(Button.SignalName.Pressed);
            await WaitForExport(tree, export);
            Check(status.Text.StartsWith("已保存", StringComparison.Ordinal) &&
                MapCatalog.Load(GameBootstrap.ResolveDataRoot()).LandMarks[id].InitialPlacement is { NeedCount: 500, MinSpacing: int.MaxValue },
                "保存时不应试跑固定种子或要求地图候选满足保底。");

            max.Text = "1";
            need.Text = "1";
            spacing.Text = "6";
            Named<FileDialog>(editor, "ImagePicker").EmitSignal(FileDialog.SignalName.FileSelected, Path.Combine(images, "seclusion_cave.png"));
            export.EmitSignal(Button.SignalName.Pressed);
            await WaitForExport(tree, export);
            Check(status.Text.StartsWith("已保存", StringComparison.Ordinal), status.Text);
            string updatedImage = MapCatalog.Load(GameBootstrap.ResolveDataRoot()).LandMarks[id].ImagePath;
            Check(updatedImage.StartsWith("landmarks/landmark_", StringComparison.Ordinal), "替换图片应获得独立资源地址。");
            replacementPng = Path.Combine(images, Path.GetFileName(updatedImage));
            Check(File.Exists(replacementPng) && File.ReadAllBytes(replacementPng).SequenceEqual(File.ReadAllBytes(Path.Combine(images, "seclusion_cave.png"))) &&
                File.ReadAllBytes(png).SequenceEqual(File.ReadAllBytes(Path.Combine(images, "array_altar.png"))),
                "保存后的图片应可加载，原图应继续可供其他地标引用。");

            byte[] before = File.ReadAllBytes(json);
            Named<Button>(editor, "NewLandmark").EmitSignal(Button.SignalName.Pressed);
            idField.Text = id;
            title.Text = "重名新地标";
            export.EmitSignal(Button.SignalName.Pressed);
            await WaitForExport(tree, export);
            Check(status.Text.Contains("ID 已存在") && before.SequenceEqual(File.ReadAllBytes(json)), "重复导出未拒绝或覆盖了定义。");

            GameBootstrap.OpenHome();
            await tree.ToSignal(tree, SceneTree.SignalName.SceneChanged);
            await ClickNavigate(tree, "%MapTest");
            Check(tree.CurrentScene is ExplorationView, "没有进入正常 2D 世界探索场景。");
            var view = (ExplorationView)tree.CurrentScene;
            var session = (ExplorationSession?)typeof(ExplorationView).GetField("_session", Private)!.GetValue(view);
            Check(session != null, "探索会话未打开。");
            var plane = PlaneMgr.Instance.MainPlane;
            var instances = LandMarkMgr.Instance.GetByPlane(plane.Id).ToLookup(landmark => landmark.DefineId);
            var map = view.GetNode<ExplorationMap>("%LocalMap");
            var thumbnail = view.GetNode<Control>("%Thumbnail");
            foreach (string key in BuiltIns.Append(id).Append("building_old_observatory"))
            {
                Check(instances.Contains(key), $"默认大陆缺少地标实例：{key}");
                var landmark = instances[key].First();
                var origins = TileId.Directions.Select(dir => landmark.Position + dir).Where(plane.TileSystem.CanWalk).ToArray();
                var origin = origins.Length == 0 ? landmark.Position : origins[0];
                Check(NpcMgr.Instance.PositionSystem.TryPlace(session!.Snapshot.NpcId, plane.Id, origin), $"无法观察地标：{key}");
                var state = (ExplorationSnapshot)typeof(ExplorationSession).GetMethod("ReadSnapshot", Private)!.Invoke(session, [""])!;
                Check(state.Tiles.TryGetValue(landmark.Position, out var tile), $"地标不在真实视野：{key}");
                Check(tile!.LandmarkName == GameDataMgr.Instance.MapCatalog.LandMarks[key].DisplayName, $"地标名称不匹配：{key}");
                var selected = typeof(ExplorationView).GetField("_selected", Private)!;
                var present = typeof(ExplorationView).GetMethod("Present", Private)!;
                selected.SetValue(view, landmark.Position);
                present.Invoke(view, [state]);
                Check(view.GetNode<Label>("%PlaceTitle").Text == tile.LandmarkName, $"地点详情未显示地标：{key}");
                if (key == "building_old_observatory")
                {
                    Check(tile.LandmarkImage.Length == 0, "旧地标没有保留可选图片约定。");
                    continue;
                }
                Check(tile.LandmarkImage == (key == id ? updatedImage : $"landmarks/{key}.png") && GameBootstrap.Assets.Texture(tile.LandmarkImage) != null,
                    $"地标图没有通过正常资产链加载：{key}");
                if (DisplayServer.GetName() == "headless") continue;

                using var rendered = await Screenshot(tree);
                var mapCenter = map.GetGlobalTransformWithCanvas() * map.TileCenter(landmark.Position);
                var thumbBounds = thumbnail.GetGlobalRect();
                var withoutArt = state.Tiles.ToDictionary(pair => pair.Key, pair => pair.Value);
                withoutArt[landmark.Position] = tile with { LandmarkImage = "" };
                present.Invoke(view, [state with { Tiles = withoutArt }]);
                using var fallback = await Screenshot(tree);
                Check(ChangedPixels(rendered, fallback, new Rect2(mapCenter - Vector2.One * 16, Vector2.One * 32)) > 40,
                    $"地图绘制没有显示真实图片：{key}");
                Check(ChangedPixels(rendered, fallback, thumbBounds) > 40, $"地点缩略图没有显示真实图片：{key}");
                if (key == id)
                {
                    var sample = new Rect2(mapCenter - Vector2.One * 22, Vector2.One * 44);
                    withoutArt[landmark.Position] = tile with { LandmarkDrawType = "None" };
                    present.Invoke(view, [state with { Tiles = withoutArt }]);
                    using var noDrawing = await Screenshot(tree);
                    Check(view.GetNode<Label>("%PlaceTitle").Text == tile.LandmarkName, "无绘制地标丢失详情名称。");
                    foreach (string drawType in new[] { "TerrainMask", "Shader", "Atmosphere" })
                    {
                        withoutArt[landmark.Position] = tile with { LandmarkDrawType = drawType };
                        present.Invoke(view, [state with { Tiles = withoutArt }]);
                        using var effect = await Screenshot(tree);
                        Check(ChangedPixels(noDrawing, effect, sample) > 40, $"{drawType} 未画出地标效果。");
                    }
                }
                if (evidence != null)
                    Check(rendered.SavePng(Path.Combine(evidence, key == id ? "exported-landmark.png" : key + ".png")) == Error.Ok,
                        $"无法保存地标画面：{key}");
            }
            GD.Print($"LANDMARK_EDITOR_CHECK_OK builtIns={BuiltIns.Length} visibleExport=1 savedExisting=1 sourceConflictRejected=1 duplicateRejected=1 renderer={DisplayServer.GetName()}");
            exitCode = 0;
        }
        catch (Exception ex) { GD.PushError($"[LandmarkEditorCheck] {ex}"); }
        finally
        {
            // 只清理此次检查创建的定义和图片。
            File.Delete(json);
            File.Delete(png);
            if (replacementPng != null) File.Delete(replacementPng);
            tree.Quit(exitCode);
        }
    }

    private static T Named<T>(Node parent, string name) where T : Node => (T)parent.FindChild(name, true, false);

    private static async Task ClickNavigate(SceneTree tree, string button)
    {
        tree.CurrentScene.GetNode<Button>(button).EmitSignal(Button.SignalName.Pressed);
        await tree.ToSignal(tree, SceneTree.SignalName.SceneChanged);
        await Frames(tree);
    }

    private static async Task Frames(SceneTree tree)
    {
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
    }

    private static async Task WaitForExport(SceneTree tree, Button button)
    {
        for (int i = 0; i < 200 && button.Disabled; i++)
        {
            await tree.ToSignal(tree.CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
        }
        Check(!button.Disabled, "导出没有在规定时间完成。");
    }

    private static async Task<Image> Screenshot(SceneTree tree)
    {
        await Frames(tree);
        await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        return tree.Root.GetTexture().GetImage();
    }

    private static async Task Capture(SceneTree tree, string? directory, string file)
    {
        if (directory == null || DisplayServer.GetName() == "headless") return;
        using var image = await Screenshot(tree);
        Check(image.SavePng(Path.Combine(directory, file + ".png")) == Error.Ok, "无法保存编辑器画面。");
    }

    private static int ChangedPixels(Image withArt, Image withoutArt, Rect2 screen)
    {
        int changed = 0;
        for (int y = Math.Max(0, (int)screen.Position.Y); y < Math.Min(withArt.GetHeight(), (int)screen.End.Y); y++)
        for (int x = Math.Max(0, (int)screen.Position.X); x < Math.Min(withArt.GetWidth(), (int)screen.End.X); x++)
            if (withArt.GetPixel(x, y) != withoutArt.GetPixel(x, y)) changed++;
        return changed;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
